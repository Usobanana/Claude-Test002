using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem.UI;

/// <summary>
/// EventSystem の InputSystemUIInputModule に
/// GameInputActions の UI アクション（Point/Click/ScrollWheel）を割り当てる
/// Game/Setup EventSystem UI Input を実行するだけでOK
/// </summary>
public static class EventSystemSetupTool
{
    private const string ActionsPath = "Assets/GameInputActions.inputactions";

    [MenuItem("Game/Setup EventSystem UI Input")]
    public static void Setup()
    {
        var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null) { Debug.LogError("[EventSystemSetup] EventSystem が見つかりません"); return; }

        var module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null)
        {
            module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[EventSystemSetup] InputSystemUIInputModule を追加しました");
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(ActionsPath);
        if (asset == null) { Debug.LogError($"[EventSystemSetup] {ActionsPath} が見つかりません"); return; }

        var so = new SerializedObject(module);

        // actionsAsset をアサイン
        so.FindProperty("m_ActionsAsset").objectReferenceValue = asset;

        // UI アクションマップから各アクション参照を設定するヘルパー
        void SetRef(string propName, string actionName)
        {
            var action = asset.FindAction($"UI/{actionName}");
            if (action == null) { Debug.LogWarning($"[EventSystemSetup] UI/{actionName} が見つかりません"); return; }

            var prop = so.FindProperty(propName);
            if (prop == null) { Debug.LogWarning($"[EventSystemSetup] プロパティ {propName} が見つかりません"); return; }

            // InputActionReference を取得（サブアセットとして存在）
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(ActionsPath);
            UnityEngine.InputSystem.InputActionReference actionRef = null;
            foreach (var a in allAssets)
            {
                if (a is UnityEngine.InputSystem.InputActionReference r && r.action?.name == actionName)
                {
                    actionRef = r;
                    break;
                }
            }

            if (actionRef == null)
            {
                // サブアセットがなければ作成
                actionRef = UnityEngine.InputSystem.InputActionReference.Create(action);
            }

            prop.objectReferenceValue = actionRef;
            Debug.Log($"[EventSystemSetup] {propName} = UI/{actionName}");
        }

        SetRef("m_PointAction",       "Point");
        SetRef("m_LeftClickAction",   "Click");
        SetRef("m_ScrollWheelAction", "ScrollWheel");
        SetRef("m_SubmitAction",      "Submit");
        SetRef("m_CancelAction",      "Cancel");
        SetRef("m_NavigateAction",    "Navigate");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(module);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[EventSystemSetup] InputSystemUIInputModule のセットアップ完了");
    }
}
