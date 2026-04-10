using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Player_AC AnimatorController を自動セットアップする
/// </summary>
public static class PlayerAnimatorSetupTool
{
    // アニメーションクリップのパス
    private const string IdlePath   = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Idle/Base/A_Idle_Base_ToIdle_Femn.fbx";
    private const string RunPath    = "Assets/Synty/AnimationGoblinLocomotion/Animations/Polygon/Neutral/Locomotion/Run/A_POLY_GBL_Run_FwdStrafe_F_Neut.fbx";
    private const string AttackPath = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Attack/LightCombo01/A_Attack_LightCombo01A_ReturnToIdle_RootMotion_Sword.fbx";
    private const string DodgePath  = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Dodge/A_DodgeRoll_F_RootMotion_Sword.fbx";
    private const string DeathPath  = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Death/A_Death_L_01_Sword.fbx";
    private const string ACPath     = "Assets/Animations/Player_AC.controller";

    [MenuItem("Game/Setup Player Animator Controller")]
    public static void SetupPlayerAnimator()
    {
        // AnimationClip をロード
        var clipIdle   = LoadClip(IdlePath);
        var clipRun    = LoadClip(RunPath);
        var clipAttack = LoadClip(AttackPath);
        var clipDodge  = LoadClip(DodgePath);
        var clipDeath  = LoadClip(DeathPath);

        if (clipIdle == null || clipRun == null || clipAttack == null ||
            clipDodge == null || clipDeath == null)
        {
            Debug.LogError("[PlayerAnimatorSetup] 一部のアニメーションクリップが見つかりません。パスを確認してください。");
            return;
        }

        // 既存があれば削除して新規作成
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ACPath) != null)
            AssetDatabase.DeleteAsset(ACPath);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(ACPath);

        // --- パラメーター ---
        ac.AddParameter("Speed",      AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Dodge",      AnimatorControllerParameterType.Trigger);
        ac.AddParameter("IsDead",     AnimatorControllerParameterType.Bool);

        // --- ステートマシン ---
        var rootSM = ac.layers[0].stateMachine;

        // Idle
        var stateIdle = rootSM.AddState("Idle");
        stateIdle.motion = clipIdle;
        rootSM.defaultState = stateIdle;

        // Run
        var stateRun = rootSM.AddState("Run");
        stateRun.motion = clipRun;

        // Attack
        var stateAttack = rootSM.AddState("Attack");
        stateAttack.motion = clipAttack;

        // Dodge
        var stateDodge = rootSM.AddState("Dodge");
        stateDodge.motion = clipDodge;

        // Death
        var stateDeath = rootSM.AddState("Death");
        stateDeath.motion = clipDeath;

        // --- トランジション ---

        // Idle <-> Run (Speed)
        var t = stateIdle.AddTransition(stateRun);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        t = stateRun.AddTransition(stateIdle);
        t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Idle/Run -> Attack
        AddAttackTransition(stateIdle,   stateAttack);
        AddAttackTransition(stateRun,    stateAttack);

        // Attack -> Idle (exit time)
        t = stateAttack.AddTransition(stateIdle);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f;

        // Idle/Run -> Dodge
        AddDodgeTransition(stateIdle,  stateDodge);
        AddDodgeTransition(stateRun,   stateDodge);

        // Dodge -> Idle (exit time)
        t = stateDodge.AddTransition(stateIdle);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f;

        // Any -> Death
        var tDeath = rootSM.AddAnyStateTransition(stateDeath);
        tDeath.hasExitTime = false; tDeath.duration = 0.1f;
        tDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerAnimatorSetup] Player_AC セットアップ完了");
    }

    private static void AddAttackTransition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
    }

    private static void AddDodgeTransition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false; t.duration = 0.05f;
        t.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        var clip   = assets.OfType<AnimationClip>()
                           .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null)
            Debug.LogWarning($"[PlayerAnimatorSetup] クリップが見つかりません: {fbxPath}");
        return clip;
    }
}
