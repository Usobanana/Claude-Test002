using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Player_AC.controller を剣コンボ対応で再構築する
/// Game/Setup Player Animator (Combo) を実行するだけでOK
/// ※ 既存コントローラーを削除して作り直す
/// </summary>
public static class PlayerAnimatorSetupTool
{
    // ─── パス定数 ───
    private const string ACPath  = "Assets/Animations/Player_AC.controller";
    private const string SC      = "Assets/Synty/AnimationSwordCombat/Animations/Polygon";
    private const string GL      = "Assets/Synty/AnimationGoblinLocomotion/Animations/Polygon/Neutral";

    // クリップ
    private const string IdlePath    = SC + "/Idle/Base/A_Idle_Base_Sword.fbx";
    private const string RunPath     = GL + "/Locomotion/Run/A_POLY_GBL_Run_FwdStrafe_F_Neut.fbx";
    private const string Combo1Path  = SC + "/Attack/LightCombo01/A_Attack_LightCombo01A_Sword.fbx";
    private const string Combo2Path  = SC + "/Attack/LightCombo01/A_Attack_LightCombo01B_Sword.fbx";
    private const string Combo3Path  = SC + "/Attack/LightCombo01/A_Attack_LightCombo01C_Sword.fbx";
    private const string Skill1Path  = SC + "/Attack/HeavyStab01/A_Attack_HeavyStab01_Sword.fbx";
    private const string Skill2Path  = SC + "/Attack/LightLeaping01/A_Attack_LightLeaping01_Sword.fbx";
    private const string Skill3Path  = SC + "/Attack/HeavyFlourish01/A_Attack_HeavyFlourish01_Sword.fbx";
    private const string UltAPath    = SC + "/Attack/HeavyCombo01/A_Attack_HeavyCombo01A_Sword.fbx";
    private const string UltBPath    = SC + "/Attack/HeavyCombo01/A_Attack_HeavyCombo01B_Sword.fbx";
    private const string UltCPath    = SC + "/Attack/HeavyCombo01/A_Attack_HeavyCombo01C_Sword.fbx";
    private const string DodgePath   = SC + "/Dodge/A_DodgeRoll_F_Sword.fbx";
    private const string HitPath     = SC + "/Hit/HitReact/A_Hit_F_React_Sword.fbx";
    private const string DeathPath   = SC + "/Death/A_Death_F_01_Sword.fbx";

    [MenuItem("Game/Setup Player Animator (Combo)")]
    public static void Setup()
    {
        // 既存を削除して新規作成
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ACPath) != null)
            AssetDatabase.DeleteAsset(ACPath);

        var ac   = AnimatorController.CreateAnimatorControllerAtPath(ACPath);
        var sm   = ac.layers[0].stateMachine;

        // ─── パラメーター ───
        ac.AddParameter("Speed",      AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("ComboIndex", AnimatorControllerParameterType.Int);
        ac.AddParameter("Dodge",      AnimatorControllerParameterType.Trigger);
        ac.AddParameter("IsDead",     AnimatorControllerParameterType.Bool);
        ac.AddParameter("HitReact",   AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Skill1",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Skill2",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Skill3",     AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Ult",        AnimatorControllerParameterType.Trigger);

        // ─── ステート ───
        var sIdle    = State(sm, "Idle",     IdlePath,   isDefault: true);
        var sRun     = State(sm, "Run",      RunPath);
        var sAttack1 = State(sm, "Attack1",  Combo1Path);
        var sAttack2 = State(sm, "Attack2",  Combo2Path);
        var sAttack3 = State(sm, "Attack3",  Combo3Path);
        var sSkill1  = State(sm, "Skill1",   Skill1Path);
        var sSkill2  = State(sm, "Skill2",   Skill2Path);
        var sSkill3  = State(sm, "Skill3",   Skill3Path);
        var sUltA    = State(sm, "UltA",     UltAPath);
        var sUltB    = State(sm, "UltB",     UltBPath);
        var sUltC    = State(sm, "UltC",     UltCPath);
        var sDodge   = State(sm, "Dodge",    DodgePath);
        var sHit     = State(sm, "HitReact", HitPath);
        var sDeath   = State(sm, "Death",    DeathPath);

        // ─── 遷移 ───

        // 移動
        T(sIdle, sRun,  t => { t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); });
        T(sRun,  sIdle, t => { t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.Less,    0.1f, "Speed"); });

        // コンボ
        // AnyState → 各打撃（AUTO モードでは attackInterval ごとに Idle から直接遷移するため全段に AnyState 遷移が必要）
        AnyT(sm, sAttack1, t => { t.AddCondition(AnimatorConditionMode.If, 0, "Attack"); t.AddCondition(AnimatorConditionMode.Equals, 1, "ComboIndex"); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
        AnyT(sm, sAttack2, t => { t.AddCondition(AnimatorConditionMode.If, 0, "Attack"); t.AddCondition(AnimatorConditionMode.Equals, 2, "ComboIndex"); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
        AnyT(sm, sAttack3, t => { t.AddCondition(AnimatorConditionMode.If, 0, "Attack"); t.AddCondition(AnimatorConditionMode.Equals, 3, "ComboIndex"); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
        T(sAttack1, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f; });
        T(sAttack2, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f; });
        T(sAttack3, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f; });

        // スキル
        foreach (var (state, trig) in new[] { (sSkill1,"Skill1"),(sSkill2,"Skill2"),(sSkill3,"Skill3") })
        {
            string tr = trig;
            AnyT(sm, state, t => { t.AddCondition(AnimatorConditionMode.If, 0, tr); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
            T(state, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f; });
        }

        // ウルト連続（A→B→C→Idle）
        AnyT(sm, sUltA, t => { t.AddCondition(AnimatorConditionMode.If, 0, "Ult"); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
        T(sUltA, sUltB, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.05f; });
        T(sUltB, sUltC, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.05f; });
        T(sUltC, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f;  });

        // 回避
        AnyT(sm, sDodge, t => { t.AddCondition(AnimatorConditionMode.If, 0, "Dodge"); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
        T(sDodge, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f; });

        // 被弾
        AnyT(sm, sHit, t => { t.AddCondition(AnimatorConditionMode.If, 0, "HitReact"); t.duration = 0.05f; t.hasExitTime = false; t.canTransitionToSelf = false; });
        T(sHit, sIdle, t => { t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f; });

        // 死亡
        AnyT(sm, sDeath, t => { t.AddCondition(AnimatorConditionMode.If, 0, "IsDead"); t.duration = 0.1f; t.hasExitTime = false; t.canTransitionToSelf = false; });

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();

        // シーン内の全 Animator に再アサイン
        ReassignController(ac);

        Debug.Log("[PlayerAnimatorSetup] Player_AC.controller を再構築しました（コンボ3段 + スキル + ウルト）");
    }

    [MenuItem("Game/Reassign Player Animator Controller")]
    public static void ReassignOnly()
    {
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ACPath);
        if (ac == null) { Debug.LogError("[PlayerAnimatorSetup] Player_AC.controller が見つかりません"); return; }
        ReassignController(ac);
    }

    private static void ReassignController(AnimatorController ac)
    {
        int count = 0;
        foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            // Player タグまたは "Player" 名のオブジェクト配下の Animator を対象
            var root = animator.transform.root;
            if (root.CompareTag("Player") || root.name == "Player")
            {
                animator.runtimeAnimatorController = ac;
                EditorUtility.SetDirty(animator);
                count++;
                Debug.Log($"[PlayerAnimatorSetup] {animator.gameObject.name} に Player_AC を再アサイン");
            }
        }
        if (count == 0) Debug.LogWarning("[PlayerAnimatorSetup] 対象の Animator が見つかりませんでした（Player タグを確認してください）");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    // ─── ユーティリティ ───

    private static AnimatorState State(AnimatorStateMachine sm, string name, string clipPath, bool isDefault = false)
    {
        var s = sm.AddState(name);
        var c = LoadClip(clipPath);
        if (c != null) s.motion = c;
        if (isDefault) sm.defaultState = s;
        return s;
    }

    private static AnimationClip LoadClip(string path)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(path);
        var clip  = clips.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null) Debug.LogWarning($"[PlayerAnimatorSetup] クリップなし: {path}");
        return clip;
    }

    private static void T(AnimatorState from, AnimatorState to, System.Action<AnimatorStateTransition> cfg)
    {
        var t = from.AddTransition(to);
        cfg(t);
    }

    private static void AnyT(AnimatorStateMachine sm, AnimatorState to, System.Action<AnimatorStateTransition> cfg)
    {
        var t = sm.AddAnyStateTransition(to);
        cfg(t);
    }
}
