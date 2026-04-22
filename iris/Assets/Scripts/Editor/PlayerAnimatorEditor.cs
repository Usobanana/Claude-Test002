using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// PlayerAnimator の Inspector 拡張。
/// ・使用アニメーション一覧の表示・編集
/// ・AnimatorController セットアップボタン（ComboAttack ステート・遷移・タグの自動生成）
/// </summary>
[CustomEditor(typeof(PlayerAnimator))]
public class PlayerAnimatorEditor : Editor
{
    // ─── クリップ一覧 ───
    private bool showClips = true;
    private bool clipsDirty = false;

    // ─── セットアップ定数 ───
    private const string PlaceholderClipName  = "ComboAttackBase";
    private const string ComboStateName       = "ComboAttack";
    private const string LocomotionTag        = "Locomotion";
    private const string AttackTag            = "Attack";
    private const string AttackParamName      = "Attack";
    private static readonly string[] LocomotionStateNames = { "Idle", "Run", "Walk", "Locomotion" };

    // ─────────────────────────────────────────
    // Inspector UI
    // ─────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);

        var pa       = (PlayerAnimator)target;
        var animator = pa.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            EditorGUILayout.HelpBox("Animator コンポーネントが見つかりません", MessageType.Warning);
            return;
        }

        // OverrideController を含むチェーンを遡って AnimatorController を取得
        var ac = GetBaseAnimatorController(animator.runtimeAnimatorController);
        if (ac == null)
        {
            EditorGUILayout.HelpBox(
                "AnimatorController が割り当てられていません\n" +
                "Animator の Controller フィールドに AnimatorController をセットしてください",
                MessageType.Warning);
        }
        else
        {
            DrawClipList(ac);
        }

        DrawSetupSection(pa);
    }

    // ─────────────────────────────────────────
    // クリップ一覧
    // ─────────────────────────────────────────

    private void DrawClipList(AnimatorController ac)
    {
        EditorGUILayout.Space(4f);
        showClips = EditorGUILayout.BeginFoldoutHeaderGroup(showClips, "使用アニメーション一覧（編集可）");
        EditorGUILayout.EndFoldoutHeaderGroup();
        if (!showClips) return;

        foreach (var layer in ac.layers)
        {
            EditorGUILayout.Space(4f);

            var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4f);
            EditorGUI.DrawRect(headerRect, new Color(0.18f, 0.18f, 0.18f, 0.5f));
            EditorGUI.LabelField(headerRect,
                $"  Layer {System.Array.IndexOf(ac.layers, layer)}: {layer.name}  (weight {layer.defaultWeight:F1})",
                EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            foreach (var stateRef in layer.stateMachine.states)
            {
                var state = stateRef.state;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(state.name, GUILayout.Width(110f));

                EditorGUI.BeginChangeCheck();
                var newMotion = (Motion)EditorGUILayout.ObjectField(state.motion, typeof(Motion), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(state, $"Set Clip [{layer.name}/{state.name}]");
                    state.motion = newMotion;
                    EditorUtility.SetDirty(state);
                    EditorUtility.SetDirty(ac);
                    clipsDirty = true;
                }

                if (state.motion == null)
                {
                    var iconRect = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(20f));
                    iconRect.y += (EditorGUIUtility.singleLineHeight - 16f) * 0.5f;
                    GUI.Label(iconRect, EditorGUIUtility.IconContent("console.warnicon.sml"));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(!clipsDirty))
        {
            var btnColor = GUI.backgroundColor;
            GUI.backgroundColor = clipsDirty ? new Color(0.6f, 1f, 0.6f) : btnColor;
            if (GUILayout.Button("AnimatorController に保存", GUILayout.Height(28f)))
            {
                AssetDatabase.SaveAssets();
                clipsDirty = false;
                Debug.Log("[PlayerAnimatorEditor] AnimatorController を保存しました");
            }
            GUI.backgroundColor = btnColor;
        }

        if (clipsDirty)
            EditorGUILayout.HelpBox("未保存の変更があります。「AnimatorController に保存」を押してください。", MessageType.Info);
    }

    // ─────────────────────────────────────────
    // セットアップセクション
    // ─────────────────────────────────────────

    private void DrawSetupSection(PlayerAnimator pa)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("─── Animator セットアップ ───", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "AnimatorController に ComboAttack ステート・遷移・タグを自動生成します。\n" +
            "Idle・Run を含む名前のステートには Tag \"Locomotion\" を自動付与します。\n\n" +
            "メニューからも実行可能: Game → Setup Player Animator",
            MessageType.Info);

        if (GUILayout.Button("Animator をセットアップ", GUILayout.Height(32)))
            RunSetup(pa);
    }

    // ─────────────────────────────────────────
    // メニューアイテム
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
        var pa = go.GetComponent<PlayerAnimator>();
        if (pa == null)
        {
            EditorUtility.DisplayDialog("エラー", "選択中のオブジェクトに PlayerAnimator コンポーネントがありません", "OK");
            return;
        }
        RunSetup(pa);
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
        var comboState  = GetOrCreateState(sm, ComboStateName);
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
    // ユーティリティ
    // ─────────────────────────────────────────

    private static AnimatorController GetBaseAnimatorController(RuntimeAnimatorController rtc)
    {
        if (rtc is AnimatorController ac)          return ac;
        if (rtc is AnimatorOverrideController ovc) return GetBaseAnimatorController(ovc.runtimeAnimatorController);
        return null;
    }

    private static AnimationClip GetOrCreatePlaceholderClip(AnimatorController controller)
    {
        string dir      = Path.GetDirectoryName(AssetDatabase.GetAssetPath(controller))?.Replace("\\", "/") ?? "Assets";
        string clipPath = $"{dir}/{PlaceholderClipName}.anim";

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null) return existing;

        var clip = new AnimationClip { name = PlaceholderClipName };
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName)
    {
        return FindStateByName(sm, stateName) ?? sm.AddState(stateName);
    }

    private static AnimatorState FindStateByName(AnimatorStateMachine sm, string stateName)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == stateName) return cs.state;
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

    private static void EnsureAttackParameter(AnimatorController controller)
    {
        foreach (var p in controller.parameters)
            if (p.name == AttackParamName) return;
        controller.AddParameter(AttackParamName, AnimatorControllerParameterType.Trigger);
    }

    private static void EnsureAnyStateTransition(AnimatorStateMachine sm, AnimatorState comboState)
    {
        AnimatorStateTransition transition = null;
        foreach (var t in sm.anyStateTransitions)
        {
            if (t.destinationState == comboState) { transition = t; break; }
        }
        if (transition == null)
            transition = sm.AddAnyStateTransition(comboState);

        transition.canTransitionToSelf = true;
        transition.hasExitTime         = false;
        transition.duration            = 0.05f;
        transition.offset              = 0f;

        bool hasCondition = false;
        foreach (var c in transition.conditions)
            if (c.parameter == AttackParamName) { hasCondition = true; break; }
        if (!hasCondition)
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
