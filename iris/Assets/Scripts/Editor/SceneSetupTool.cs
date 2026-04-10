using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンの初期セットアップをメニューから実行するエディタツール
/// </summary>
public static class SceneSetupTool
{
    [MenuItem("Game/Setup Player and Camera")]
    public static void SetupAll()
    {
        SetupPlayer();
        SetupManagers();
        SetupCamera();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[SceneSetupTool] セットアップ完了");
    }

    [MenuItem("Game/Setup Player")]
    public static void SetupPlayer()
    {
        var existing = GameObject.Find("Player");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        go.tag  = "Player";
        go.transform.position = new Vector3(0f, 1f, 0f);

        // CapsuleCollider と CharacterController は競合するので削除
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());

        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;

        go.AddComponent<CharacterEntity>();
        go.AddComponent<PlayerController>();
        // PlayerInput は SceneManagers/InputHandler 側に集約するためここには追加しない

        Undo.RegisterCreatedObjectUndo(go, "Setup Player");
        Debug.Log("[SceneSetupTool] Player 配置完了");
    }

    [MenuItem("Game/Setup Managers")]
    public static void SetupManagers()
    {
        var existing = GameObject.Find("SceneManagers");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var root = new GameObject("SceneManagers");
        root.AddComponent<GameManager>();
        root.AddComponent<SceneLoader>();

        var ih = new GameObject("InputHandler");
        ih.transform.SetParent(root.transform);
        var pi = ih.AddComponent<PlayerInput>();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/GameInputActions.inputactions");
        if (actions != null) { pi.actions = actions; pi.defaultActionMap = "Player"; }
        ih.AddComponent<InputHandler>();

        Undo.RegisterCreatedObjectUndo(root, "Setup Managers");
        Debug.Log("[SceneSetupTool] Managers 配置完了");
    }

    [MenuItem("Game/Setup Camera")]
    public static void SetupCamera()
    {
        var camGo = GameObject.FindWithTag("MainCamera") ?? new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        if (camGo.GetComponent<Camera>() == null) camGo.AddComponent<Camera>();

        camGo.transform.position = new Vector3(0f, 15f, -8f);
        camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        if (camGo.GetComponent<CameraFollow>() == null) camGo.AddComponent<CameraFollow>();

        Debug.Log("[SceneSetupTool] Camera 設定完了");
    }
}
