using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// EnemyAnimator の Inspector に AnimatorController で使用しているクリップ一覧を表示し変更できる。
/// PlayerAnimatorEditor と同じ構造。
/// ・クリップを ObjectField で直接ドラッグ差し替え可能
/// ・変更は AnimatorController アセットに即時反映（Undo 対応）
/// </summary>
[CustomEditor(typeof(EnemyAnimator))]
public class EnemyAnimatorEditor : Editor
{
    private bool showClips = true;
    private bool clipsDirty = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);

        var ea       = (EnemyAnimator)target;
        var animator = ea.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            EditorGUILayout.HelpBox("Animator コンポーネントが見つかりません", MessageType.Warning);
            return;
        }

        var ac = animator.runtimeAnimatorController as AnimatorController;
        if (ac == null)
        {
            EditorGUILayout.HelpBox("AnimatorController が割り当てられていません", MessageType.Warning);
            return;
        }

        // ─── セクションヘッダー ───
        showClips = EditorGUILayout.BeginFoldoutHeaderGroup(showClips, "使用アニメーション一覧（編集可）");
        EditorGUILayout.EndFoldoutHeaderGroup();
        if (!showClips) return;

        // ─── 各レイヤー → ステート → クリップ ObjectField ───
        foreach (var layer in ac.layers)
        {
            EditorGUILayout.Space(4f);

            // レイヤーヘッダー
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

                // ステート名ラベル（固定幅）
                EditorGUILayout.LabelField(state.name, GUILayout.Width(110f));

                // クリップ ObjectField（AnimationClip / BlendTree 両対応）
                EditorGUI.BeginChangeCheck();
                var newMotion = (Motion)EditorGUILayout.ObjectField(
                    state.motion,
                    typeof(Motion),
                    false);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(state, $"Set Clip [{layer.name}/{state.name}]");
                    state.motion = newMotion;
                    EditorUtility.SetDirty(state);
                    EditorUtility.SetDirty(ac);
                    clipsDirty = true;
                }

                // クリップが未割り当ての場合に警告アイコン
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

        // ─── 保存ボタン ───
        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!clipsDirty))
        {
            var btnColor = GUI.backgroundColor;
            GUI.backgroundColor = clipsDirty ? new Color(0.6f, 1f, 0.6f) : btnColor;

            if (GUILayout.Button("AnimatorController に保存", GUILayout.Height(28f)))
            {
                AssetDatabase.SaveAssets();
                clipsDirty = false;
                Debug.Log("[EnemyAnimatorEditor] AnimatorController を保存しました");
            }

            GUI.backgroundColor = btnColor;
        }

        if (clipsDirty)
            EditorGUILayout.HelpBox("未保存の変更があります。「AnimatorController に保存」を押してください。", MessageType.Info);

        EditorGUILayout.Space(4f);
    }
}
