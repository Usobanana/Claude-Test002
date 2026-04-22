using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// PlayerAnimator の Inspector に「Animator をセットアップ」ボタンを追加する。
/// AnimatorController に ComboAttack ステート・遷移・タグを自動生成する。
/// </summary>
[CustomEditor(typeof(PlayerAnimator))]
public class PlayerAnimatorSetupEditor : Editor
{
    private const string PlaceholderClipName = "ComboAttackBase";
    private const string ComboStateName      = "ComboAttack";
    private const string LocomotionTag       = "Locomotion";
    private const string AttackTag           = "Attack";
    private const string AttackParamName     = "Attack";

    // Locomotion タグを自動付与するステート名（部分一致）
    private static readonly string[] LocomotionStateNames = { "Idle", "Run", "Walk", "Locomotion" };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("─── Animator セットアップ ───", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "AnimatorController に ComboAttack ステート・遷移・タグを自動生成します。\n" +
            "Idle・Run を含む名前のステートには Tag \"Locomotion\" を自動付与します。",
            MessageType.Info
        );

        if (GUILayout.Button("Animator をセットアップ", GUILayout.Height(32)))
            RunSetup();
    }

    // ─────────────────────────────────────────
    // セットアップ処理
    // ─────────────────────────────────────────

    private void RunSetup()
    {
        var playerAnimator = (PlayerAnimator)target;

        var animator = playerAnimator.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog("エラー", "子に Animator が見つかりません", "OK");
            return;
        }

        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            EditorUtility.DisplayDialog("エラー",
                "AnimatorController が見つかりません。\nAnimator に AnimatorController を設定してから実行してください。", "OK");
            return;
        }

        Undo.RecordObject(controller, "PlayerAnimator Combo Setup");

        var sm = controller.layers[0].stateMachine;

        // 1. プレースホルダークリップを作成（または既存を取得）
        var placeholder = GetOrCreatePlaceholderClip(controller);

        // 2. ComboAttack ステートを作成（または既存を取得）して設定
        var comboState = GetOrCreateState(sm, ComboStateName);
        comboState.motion = placeholder;
        comboState.tag    = AttackTag;

        // 3. Locomotion ステートにタグを付与
        int taggedCount = TagLocomotionStates(sm);

        // 4. Any State → ComboAttack 遷移を設定
        EnsureAttackParameter(controller);
        EnsureAnyStateTransition(sm, comboState);

        // 5. ComboAttack → Idle 遷移を設定（Idle がなければスキップ）
        var idleState = FindStateByName(sm, "Idle");
        if (idleState != null)
            EnsureExitTransition(comboState, idleState);

        // 6. PlayerAnimator の comboPlaceholderClip フィールドを更新
        var so   = new SerializedObject(playerAnimator);
        var prop = so.FindProperty("comboPlaceholderClip");
        prop.objectReferenceValue = placeholder;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        string message = $"セットアップが完了しました。\n\n"
                       + $"・ComboAttack ステート：作成済み\n"
                       + $"・プレースホルダークリップ：{placeholder.name}\n"
                       + $"・Locomotion タグ付与：{taggedCount} ステート";
        if (taggedCount == 0)
            message += "\n\n⚠ Idle・Run ステートが見つかりませんでした。\n  Animator で手動タグを設定してください。";

        EditorUtility.DisplayDialog("完了", message, "OK");
        Debug.Log($"[PlayerAnimatorSetup] {message}");
    }

    // ─────────────────────────────────────────
    // プレースホルダークリップ
    // ─────────────────────────────────────────

    private AnimationClip GetOrCreatePlaceholderClip(AnimatorController controller)
    {
        string controllerPath = AssetDatabase.GetAssetPath(controller);
        string dir            = Path.GetDirectoryName(controllerPath)?.Replace("\\", "/") ?? "Assets";
        string clipPath       = $"{dir}/{PlaceholderClipName}.anim";

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null) return existing;

        var clip = new AnimationClip { name = PlaceholderClipName };
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    // ─────────────────────────────────────────
    // ステート操作
    // ─────────────────────────────────────────

    private AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName)
    {
        var existing = FindStateByName(sm, stateName);
        return existing ?? sm.AddState(stateName);
    }

    private AnimatorState FindStateByName(AnimatorStateMachine sm, string stateName)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == stateName)
                return cs.state;
        return null;
    }

    private int TagLocomotionStates(AnimatorStateMachine sm)
    {
        int count = 0;
        foreach (var cs in sm.states)
        {
            foreach (var keyword in LocomotionStateNames)
            {
                if (cs.state.name.Contains(keyword))
                {
                    cs.state.tag = LocomotionTag;
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    // ─────────────────────────────────────────
    // パラメータ・遷移
    // ─────────────────────────────────────────

    private void EnsureAttackParameter(AnimatorController controller)
    {
        foreach (var p in controller.parameters)
            if (p.name == AttackParamName) return;
        controller.AddParameter(AttackParamName, AnimatorControllerParameterType.Trigger);
    }

    private void EnsureAnyStateTransition(AnimatorStateMachine sm, AnimatorState comboState)
    {
        // 既に同じ遷移があれば何もしない
        foreach (var t in sm.anyStateTransitions)
            if (t.destinationState == comboState) return;

        var transition = sm.AddAnyStateTransition(comboState);
        transition.canTransitionToSelf = true;   // コンボ中の自己遷移を許可
        transition.hasExitTime         = false;
        transition.duration            = 0.05f;
        transition.offset              = 0f;
        transition.AddCondition(AnimatorConditionMode.If, 0, AttackParamName);
    }

    private void EnsureExitTransition(AnimatorState from, AnimatorState to)
    {
        foreach (var t in from.transitions)
            if (t.destinationState == to) return;

        var transition = from.AddTransition(to);
        transition.hasExitTime      = true;
        transition.exitTime         = 1f;    // アニメーション完了後に Idle へ
        transition.duration         = 0.1f;
        transition.hasFixedDuration = false;
    }
}
