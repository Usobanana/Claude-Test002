using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Player / Enemy を Prefab 化し、各シーンのインスタンスを Prefab に差し替えるEditorツール
/// 実行順:
///   1. FieldScene を開いた状態で "Game/Create All Prefabs" を実行
///      → Assets/Prefabs/ に Player / Enemy_Normal / Enemy_Boss.prefab を作成
///      → FieldScene・BaseScene 両方を自動的に差し替えて保存
/// </summary>
public static class PrefabCreationTool
{
    private const string PrefabFolder       = "Assets/Prefabs";
    private const string PlayerPrefabPath   = "Assets/Prefabs/Player.prefab";
    private const string NormalPrefabPath   = "Assets/Prefabs/Enemy_Normal.prefab";
    private const string BossPrefabPath     = "Assets/Prefabs/Enemy_Boss.prefab";

    private const string FieldScenePath     = "Assets/Scenes/FieldScene.unity";
    private const string BaseScenePath      = "Assets/Scenes/BaseScene.unity";

    [MenuItem("Game/Create All Prefabs")]
    public static void CreateAllPrefabs()
    {
        // 未保存変更を確認
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[PrefabTool] キャンセルされました");
            return;
        }

        // Prefabs フォルダ作成
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
            Debug.Log("[PrefabTool] Assets/Prefabs フォルダを作成しました");
        }

        // ── Step 1: FieldScene で Prefab を作成 & インスタンスを接続 ────────
        var fieldScene = EditorSceneManager.OpenScene(FieldScenePath, OpenSceneMode.Single);

        bool playerCreated  = CreatePrefabFromScene("Player",       PlayerPrefabPath, byTag: true);
        bool normalCreated  = CreatePrefabFromScene("Enemy_Normal",  NormalPrefabPath);
        bool bossCreated    = CreatePrefabFromScene("Enemy_Boss",    BossPrefabPath);

        EditorSceneManager.SaveScene(fieldScene);
        Debug.Log("[PrefabTool] FieldScene を保存しました");

        // ── Step 2: BaseScene の Player を Prefab インスタンスに差し替え ─────
        if (playerCreated)
        {
            var baseScene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);
            ReplacePrefabInScene("Player", PlayerPrefabPath, byTag: true);
            EditorSceneManager.SaveScene(baseScene);
            Debug.Log("[PrefabTool] BaseScene を保存しました");
        }

        // FieldScene に戻す
        EditorSceneManager.OpenScene(FieldScenePath, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabTool] 全Prefab作成 & シーン差し替え完了");
    }

    // ─────────────────────────────────────────────────────────────
    // Prefab 作成: シーン上の GameObject を Prefab として保存し接続する
    // ─────────────────────────────────────────────────────────────

    private static bool CreatePrefabFromScene(string goName, string savePath, bool byTag = false)
    {
        var go = byTag ? GameObject.FindWithTag("Player") : GameObject.Find(goName);
        if (go == null)
        {
            Debug.LogWarning($"[PrefabTool] {goName} が見つかりません（スキップ）");
            return false;
        }

        // SaveAsPrefabAssetAndConnect: Prefab を保存しシーンインスタンスを接続
        bool success;
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.UserAction, out success);
        if (success)
            Debug.Log($"[PrefabTool] {savePath} を作成・接続しました");
        else
            Debug.LogError($"[PrefabTool] Prefab 作成失敗: {savePath}");

        return success;
    }

    // ─────────────────────────────────────────────────────────────
    // Prefab 差し替え: 既存 GameObject を削除して Prefab インスタンスに置換
    // ─────────────────────────────────────────────────────────────

    private static void ReplacePrefabInScene(string goName, string prefabPath, bool byTag = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[PrefabTool] Prefab が見つかりません: {prefabPath}");
            return;
        }

        var existing = byTag ? GameObject.FindWithTag("Player") : GameObject.Find(goName);

        // 既に Prefab インスタンスになっていたらスキップ
        if (existing != null && PrefabUtility.GetPrefabAssetType(existing) != PrefabAssetType.NotAPrefab)
        {
            Debug.Log($"[PrefabTool] {goName} は既にPrefabインスタンスです（スキップ）");
            return;
        }

        // 位置・回転を記録してから差し替え
        Vector3    pos = existing != null ? existing.transform.position : Vector3.zero;
        Quaternion rot = existing != null ? existing.transform.rotation : Quaternion.identity;

        if (existing != null)
            Object.DestroyImmediate(existing);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = pos;
        instance.transform.rotation = rot;
        instance.name = goName;

        Debug.Log($"[PrefabTool] {goName} を {prefabPath} のインスタンスに差し替えました");
    }
}
