using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// デバッグメニューを GameManager オブジェクトにアタッチするセットアップツール。
/// メニュー [Game/Setup Debug Animation Menu] を実行するだけでOK。
/// </summary>
public static class DebugMenuSetupTool
{
    [MenuItem("Game/Setup Debug Animation Menu")]
    public static void Setup()
    {
        // DebugAnimationMenu が既にシーンに存在するか確認
        var existing = Object.FindAnyObjectByType<DebugAnimationMenu>();
        if (existing != null)
        {
            Debug.Log($"[DebugMenu] 既にシーンに存在します: {existing.gameObject.name}");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // GameManager があればそこにアタッチ、なければ専用オブジェクトを作る
        GameObject target = GameObject.Find("GameManager")
                         ?? GameObject.Find("DebugManager")
                         ?? new GameObject("DebugManager");

        Undo.AddComponent<DebugAnimationMenu>(target);
        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = target;
        Debug.Log($"[DebugMenu] DebugAnimationMenu を '{target.name}' にアタッチしました。F12 で表示切替。");
    }
}
