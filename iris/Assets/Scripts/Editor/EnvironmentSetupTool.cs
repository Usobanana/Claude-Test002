using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// FieldScene / BaseScene に POLYGON Fantasy Kingdom のプロップ・建物を一括配置する
/// "Environment" 親オブジェクトにまとめるので、やり直す場合はそれを削除してから再実行
/// </summary>
public static class EnvironmentSetupTool
{
    // --- パス定数 ---
    private const string PrefabRoot = "Assets/Synty/PolygonFantasyKingdom/Prefabs";

    // 環境
    private const string TreeLarge   = PrefabRoot + "/Environments/SM_Env_Tree_Large_01.prefab";
    private const string TreeRound   = PrefabRoot + "/Environments/SM_Env_Tree_Round_01.prefab";
    private const string TreePine    = PrefabRoot + "/Environments/SM_Env_Tree_Thin_01.prefab";
    private const string Rock01      = PrefabRoot + "/Environments/SM_Env_Rock_01.prefab";
    private const string Rock02      = PrefabRoot + "/Environments/SM_Env_Rock_02.prefab";
    private const string Rock03      = PrefabRoot + "/Environments/SM_Env_Rock_03.prefab";
    private const string Bush01      = PrefabRoot + "/Environments/SM_Env_Bush_01.prefab";
    private const string Bush02      = PrefabRoot + "/Environments/SM_Env_Bush_02.prefab";
    private const string BushCluster = PrefabRoot + "/Environments/SM_Env_Bush_Cluster_01.prefab";
    private const string Flowers01   = PrefabRoot + "/Environments/SM_Env_Flowers_01.prefab";
    private const string StoneWall01 = PrefabRoot + "/Environments/SM_Env_StoneWall_01.prefab";
    private const string Fountain    = PrefabRoot + "/Environments/SM_Env_Fountain_01.prefab";

    // 建物
    private const string BldHouse01     = PrefabRoot + "/Buildings/Presets/SM_Bld_Preset_House_01_A_Optimized.prefab";
    private const string BldHouse02     = PrefabRoot + "/Buildings/Presets/SM_Bld_Preset_House_02_A_Optimized.prefab";
    private const string BldBlacksmith  = PrefabRoot + "/Buildings/Presets/SM_Bld_Preset_Blacksmith_01_Optimized.prefab";
    private const string BldChurch      = PrefabRoot + "/Buildings/Presets/SM_Bld_Preset_Church_01_A_Optimized.prefab";

    // プロップ
    private const string Torch01 = PrefabRoot + "/Props/SM_Prop_Torch_01.prefab";

    // =========================================================
    //  FieldScene 環境配置
    // =========================================================

    [MenuItem("Game/Setup FieldScene Environment")]
    public static void SetupFieldEnvironment()
    {
        if (SceneManager.GetActiveScene().name != "FieldScene")
        {
            Debug.LogWarning("[EnvironmentSetup] FieldScene をアクティブにしてから実行してください");
            return;
        }

        var env = GetOrCreateParent("FieldEnvironment");
        if (env.transform.childCount > 0)
        {
            Debug.LogWarning("[EnvironmentSetup] FieldEnvironment は既に子オブジェクトを持っています。削除してから再実行してください。");
            return;
        }

        // --- アリーナ外周の大木（半径23の円周上、12本）---
        float treeRadius = 23f;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * (360f / 12f);
            float rad   = angle * Mathf.Deg2Rad;
            var pos     = new Vector3(Mathf.Sin(rad) * treeRadius, 0f, Mathf.Cos(rad) * treeRadius);
            string prefab = (i % 3 == 0) ? TreeLarge : (i % 3 == 1 ? TreeRound : TreePine);
            PlacePrefab(prefab, env.transform, pos, Quaternion.Euler(0f, angle + 30f, 0f));
        }

        // --- 中層の岩（散在 6個）---
        var rockPositions = new Vector3[]
        {
            new Vector3(-8f,  0f,  10f),
            new Vector3( 12f, 0f,  8f),
            new Vector3(-12f, 0f, -6f),
            new Vector3( 6f,  0f, -12f),
            new Vector3(-5f,  0f, -15f),
            new Vector3( 15f, 0f, -4f),
        };
        string[] rockPrefabs = { Rock01, Rock02, Rock03, Rock01, Rock02, Rock03 };
        for (int i = 0; i < rockPositions.Length; i++)
        {
            float yRot = Random.Range(0f, 360f);
            PlacePrefab(rockPrefabs[i], env.transform, rockPositions[i], Quaternion.Euler(0f, yRot, 0f));
        }

        // --- 茂み（外周近くに点在 8個）---
        var bushPositions = new Vector3[]
        {
            new Vector3(-18f, 0f,  5f),
            new Vector3( 18f, 0f,  3f),
            new Vector3(-3f,  0f, 18f),
            new Vector3( 5f,  0f,-18f),
            new Vector3(-15f, 0f,-10f),
            new Vector3( 10f, 0f, 16f),
            new Vector3(-10f, 0f, 15f),
            new Vector3( 16f, 0f,-12f),
        };
        for (int i = 0; i < bushPositions.Length; i++)
        {
            string prefab = (i % 2 == 0) ? Bush01 : BushCluster;
            PlacePrefab(prefab, env.transform, bushPositions[i], Quaternion.identity);
        }

        // --- ボス戦エリア奥の石壁（ボス背後の舞台演出）---
        for (int i = -2; i <= 2; i++)
        {
            var wallPos = new Vector3(i * 3f, 0f, 22f);
            PlacePrefab(StoneWall01, env.transform, wallPos, Quaternion.identity);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnvironmentSetup] FieldScene 環境配置完了（木12 / 岩6 / 茂み8 / 石壁5）");
    }

    // =========================================================
    //  BaseScene 環境配置
    // =========================================================

    [MenuItem("Game/Setup BaseScene Environment")]
    public static void SetupBaseEnvironment()
    {
        if (SceneManager.GetActiveScene().name != "BaseScene")
        {
            Debug.LogWarning("[EnvironmentSetup] BaseScene をアクティブにしてから実行してください");
            return;
        }

        var env = GetOrCreateParent("BaseEnvironment");
        if (env.transform.childCount > 0)
        {
            Debug.LogWarning("[EnvironmentSetup] BaseEnvironment は既に子オブジェクトを持っています。削除してから再実行してください。");
            return;
        }

        // --- 建物 ---
        // 鍛冶屋（Guilcube の隣）
        PlacePrefab(BldBlacksmith, env.transform, new Vector3(10f, 0f,  6f), Quaternion.Euler(0f, -90f, 0f));

        // 民家 x2
        PlacePrefab(BldHouse01, env.transform, new Vector3(-10f, 0f,  8f), Quaternion.Euler(0f, 90f, 0f));
        PlacePrefab(BldHouse02, env.transform, new Vector3(-10f, 0f, -8f), Quaternion.Euler(0f, 90f, 0f));

        // 教会（奥に配置して格式を出す）
        PlacePrefab(BldChurch, env.transform, new Vector3(0f, 0f, 16f), Quaternion.Euler(0f, 180f, 0f));

        // --- 広場の噴水 ---
        PlacePrefab(Fountain, env.transform, new Vector3(0f, 0f, 4f), Quaternion.identity);

        // --- 外周の木（4隅 + α）---
        var treePositions = new Vector3[]
        {
            new Vector3(-20f, 0f,  20f),
            new Vector3( 20f, 0f,  20f),
            new Vector3(-20f, 0f, -20f),
            new Vector3( 20f, 0f, -20f),
            new Vector3(-20f, 0f,   0f),
            new Vector3( 20f, 0f,   0f),
            new Vector3(  0f, 0f, -20f),
        };
        string[] treePrefabs = { TreeLarge, TreeLarge, TreeRound, TreeRound, TreePine, TreePine, TreeLarge };
        for (int i = 0; i < treePositions.Length; i++)
        {
            float yRot = Random.Range(0f, 360f);
            PlacePrefab(treePrefabs[i], env.transform, treePositions[i], Quaternion.Euler(0f, yRot, 0f));
        }

        // --- 建物周辺の茂み・花 ---
        var decorPositions = new Vector3[]
        {
            new Vector3(-7f, 0f,  10f),
            new Vector3(-7f, 0f,   6f),
            new Vector3( 7f, 0f, -10f),
            new Vector3(-6f, 0f, -10f),
        };
        for (int i = 0; i < decorPositions.Length; i++)
        {
            PlacePrefab((i % 2 == 0) ? Bush02 : Flowers01, env.transform, decorPositions[i], Quaternion.identity);
        }

        // --- 松明（FieldExit とGuild近く）---
        PlacePrefab(Torch01, env.transform, new Vector3(-4f, 0f, 1.5f), Quaternion.Euler(0f,  0f, 0f));
        PlacePrefab(Torch01, env.transform, new Vector3(-6f, 0f, 1.5f), Quaternion.Euler(0f, 180f, 0f));
        PlacePrefab(Torch01, env.transform, new Vector3( 4f, 0f, 1.5f), Quaternion.Euler(0f,  0f, 0f));
        PlacePrefab(Torch01, env.transform, new Vector3( 6f, 0f, 1.5f), Quaternion.Euler(0f, 180f, 0f));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[EnvironmentSetup] BaseScene 環境配置完了（建物4 / 噴水 / 木7 / 茂み4 / 松明4）");
    }

    // =========================================================
    //  共通ユーティリティ
    // =========================================================

    private static GameObject GetOrCreateParent(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static GameObject PlacePrefab(string path, Transform parent, Vector3 pos, Quaternion rot)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[EnvironmentSetup] プレハブが見つかりません: {path}");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(pos, rot);
        Undo.RegisterCreatedObjectUndo(go, "Place " + prefab.name);
        return go;
    }
}
