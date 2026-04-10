using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Enemy_AC AnimatorController を自動セットアップする
/// ゴブリン移動（GoblinLocomotion）+ ソードコンバット（SwordCombat）を組み合わせ
/// </summary>
public static class EnemyAnimatorSetupTool
{
    // アニメーションクリップのパス
    private const string IdlePath   = "Assets/Synty/AnimationGoblinLocomotion/Animations/Polygon/Neutral/Idles/A_POLY_GBL_Idle_Standing_Neut.fbx";
    private const string RunPath    = "Assets/Synty/AnimationGoblinLocomotion/Animations/Polygon/Neutral/Locomotion/Run/A_POLY_GBL_Run_F_Neut.fbx";
    private const string AttackPath = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Attack/LightCombo01/A_Attack_LightCombo01A_Sword.fbx";
    private const string HitPath    = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Hit/HitReact/A_Hit_F_React_Sword.fbx";
    private const string DeathPath  = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Death/A_Death_F_01_Sword.fbx";
    private const string ACPath     = "Assets/Animations/Enemy_AC.controller";

    [MenuItem("Game/Setup Enemy Animator Controller")]
    public static void SetupEnemyAnimator()
    {
        var clipIdle   = LoadClip(IdlePath);
        var clipRun    = LoadClip(RunPath);
        var clipAttack = LoadClip(AttackPath);
        var clipHit    = LoadClip(HitPath);
        var clipDeath  = LoadClip(DeathPath);

        if (clipIdle == null || clipRun == null || clipAttack == null ||
            clipHit == null  || clipDeath == null)
        {
            Debug.LogError("[EnemyAnimatorSetup] 一部のアニメーションクリップが見つかりません。パスを確認してください。");
            return;
        }

        // 既存があれば削除して新規作成
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ACPath) != null)
            AssetDatabase.DeleteAsset(ACPath);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(ACPath);

        // --- パラメーター ---
        ac.AddParameter("Speed",     AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack",    AnimatorControllerParameterType.Trigger);
        ac.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("IsDead",   AnimatorControllerParameterType.Bool);

        // --- ステートマシン ---
        var rootSM = ac.layers[0].stateMachine;

        var stateIdle   = rootSM.AddState("Idle");
        stateIdle.motion = clipIdle;
        rootSM.defaultState = stateIdle;

        var stateRun    = rootSM.AddState("Run");
        stateRun.motion  = clipRun;

        var stateAttack  = rootSM.AddState("Attack");
        stateAttack.motion = clipAttack;

        var stateHit     = rootSM.AddState("HitReact");
        stateHit.motion  = clipHit;

        var stateDeath   = rootSM.AddState("Death");
        stateDeath.motion = clipDeath;

        // --- トランジション ---

        // Idle <-> Run（Speed）
        var t = stateIdle.AddTransition(stateRun);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        t = stateRun.AddTransition(stateIdle);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Idle / Run -> Attack
        AddTriggerTransition(stateIdle, stateAttack, "Attack");
        AddTriggerTransition(stateRun,  stateAttack, "Attack");

        // Attack -> Idle（exitTime）
        t = stateAttack.AddTransition(stateIdle);
        t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.15f;

        // Idle / Run -> HitReact
        AddTriggerTransition(stateIdle, stateHit, "HitReact");
        AddTriggerTransition(stateRun,  stateHit, "HitReact");

        // HitReact -> Idle（exitTime）
        t = stateHit.AddTransition(stateIdle);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f;

        // Any -> Death（IsDead）
        var tDeath = rootSM.AddAnyStateTransition(stateDeath);
        tDeath.hasExitTime = false; tDeath.duration = 0.1f;
        tDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyAnimatorSetup] Enemy_AC セットアップ完了 → " + ACPath);
    }

    private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        var clip   = assets.OfType<AnimationClip>()
                           .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null)
            Debug.LogWarning($"[EnemyAnimatorSetup] クリップが見つかりません: {fbxPath}");
        return clip;
    }
}
