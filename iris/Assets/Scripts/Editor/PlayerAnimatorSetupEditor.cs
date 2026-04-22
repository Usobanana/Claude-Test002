using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// PlayerAnimator の Inspector に「Animator をセットアップ」ボタンを追加する。
/// AnimatorController に ComboAttack ステート・遷移・タグを自動生成する。
///
/// メニューからも実行可能: Game → Setup Player Animator
/// （Inspector のボタンが表示されない場合はメニューから実行する）
/// </summary>
[CustomEditor(typeof(PlayerAnimator))]
public class PlayerAnimatorSetupEditor : Editor
{
    private const string PlaceholderClipName = "ComboAttackBase";
    private const string ComboStateName      = "ComboAttack";
    private const string LocomotionTag       = "Locomotion";
    private const string AttackTag           = "Attack";
    private const string AttackParamName     = "Attack";

    private static readonly string[] LocomotionStateNames = { "Idle", "Run", "Walk", "Locomotion" };

    // ─────────────────────────────────────────
    // Inspector UI
    // ─────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("─── Animator セットアップ ───", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "AnimatorController に ComboAttack ステート・遷移・タグを自動生成します。\n" +
            "Idle・Run を含む名前のステートには Tag \"Locomotion\" を自動付与します。\n\n" +
            "メニューからも実行可能: Game → Setup Player Animator",
            MessageType.Info
        );

        if (GUILayout.Button("Animator をセットアップ", GUILayout.Height(32)))
            RunSetup((PlayerAnimator)target);
    }

    // ─────────────────────────────────────────
    // メニューアイテム（Inspector が表示されない場合の代替手段）
    // ─────────────────────────────────────────

    [MenuItem("Game/Setup Player Animator")]
    public static void SetupFromMenu()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("エラー", "Hierarchy で Player オブジェクトを選択してから実行してください", "OK");
            return;
        }
        var playerAnimator = go.GetComponent<PlayerAnimator>();
        if (playerAnimator == null)
        {
            EditorUtility.DisplayDialog("エラー", "選択中のオブジェクトに PlayerAnimator コンポーネントがありません", "OK");
            return;
        }
        RunSetup(playerAnimator);
    }

    // ─────────────────────────────────────────
    // セットアップ処理
    // ─────────────────────────────────────────

    private static void RunSetup(PlayerAnimator playerAnimator)
    {
        var animator = playerAnimator.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog("エラー", "子に Animator が見つかりません", "OK");
            return;
        }

        // runtimeAnimatorController が AnimatorOverrideController の場合、元の AnimatorController まで遡る
        var controller = GetBaseAnimatorController(animator.runtimeAnimatorController);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("エラー",
                "AnimatorController が見つかりません。\nAnimator の Controller フィールドに AnimatorController を設定してから実行してください。", "OK");
            return;
        }

        Undo.RecordObject(controller, "PlayerAnimator Combo Setup");

        var sm = controller.layers[0].stateMachine;

        var placeholder = GetOrCreatePlaceholderClip(controller);

        var comboState = GetOrCreateState(sm, ComboStateName);
        comboState.motion = placeholder;
        comboState.tag    = AttackTag;

        int taggedCount = TagLocomotionStates(sm);

        EnsureAttackParameter(controller);
        EnsureAnyStateTransition(sm, comboState);

        var idleState = FindStateByName(sm, "Idle");
        if (idleState != null)
            EnsureExitTransition(comboState, idleState);

        var so   = new SerializedObject(playerAnimator);
        var prop = so.FindProperty("comboPlaceholderClip");
        prop.objectReferenceValue = placeholder;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        string message = "セットアップが完了しました。\n\n"
                       + $"・ComboAttack ステート：作成済み\n"
                       + $"・プレースホルダークリップ：{placeholder.name}\n"
                       + $"・Locomotion タグ付与：{taggedCount} ステート";
        if (taggedCount == 0)
            message += "\n\n⚠ Idle・Run ステートが見つかりませんでした。\n  Animator で手動でタグを設定してください。";

        EditorUtility.DisplayDialog("完了", message, "OK");
        Debug.Log($"[PlayerAnimatorSetup] {message}");
    }

    // ─────────────────────────────────────────
    // プレースホルダークリップ
    // ─────────────────────────────────────────

    private static AnimatorController GetBaseAnimatorController(RuntimeAnimatorController rtc)
    {
        if (rtc is AnimatorController ac)           return ac;
        if (rtc is AnimatorOverrideController ovc)  return GetBaseAnimatorController(ovc.runtimeAnimatorController);
        return null;
    }

    private static AnimationClip GetOrCreatePlaceholderClip(AnimatorController controller)
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

    private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName)
    {
        var existing = FindStateByName(sm, stateName);
        return existing ?? sm.AddState(stateName);
    }

    private static AnimatorState FindStateByName(AnimatorStateMachine sm, string stateName)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == stateName)
                return cs.state;
        return null;
    }

    private static int TagLocomotionStates(AnimatorStateMachine sm)
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

    private static void EnsureAttackParameter(AnimatorController controller)
    {
        foreach (var p in controller.parameters)
            if (p.name == AttackParamName) return;
        controller.AddParameter(AttackParamName, AnimatorControllerParameterType.Trigger);
    }

    private static void EnsureAnyStateTransition(AnimatorStateMachine sm, AnimatorState comboState)
    {
        foreach (var t in sm.anyStateTransitions)
            if (t.destinationState == comboState) return;

        var transition = sm.AddAnyStateTransition(comboState);
        transition.canTransitionToSelf = true;
        transition.hasExitTime         = false;
        transition.duration            = 0.05f;
        transition.offset              = 0f;
        transition.AddCondition(AnimatorConditionMode.If, 0, AttackParamName);
    }

    private static void EnsureExitTransition(AnimatorState from, AnimatorState to)
    {
        foreach (var t in from.transitions)
            if (t.destinationState == to) return;

        var transition = from.AddTransition(to);
        transition.hasExitTime      = true;
        transition.exitTime         = 1f;
        transition.duration         = 0.1f;
        transition.hasFixedDuration = false;
    }
}
