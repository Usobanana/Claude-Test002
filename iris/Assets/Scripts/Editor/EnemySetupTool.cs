using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

/// <summary>
/// エネミーのセットアップをメニューから実行するエディタツール
/// </summary>
public static class EnemySetupTool
{
    private const string EnemyACPath = "Assets/Animations/Enemy_AC.controller";

    [MenuItem("Game/Spawn Normal Enemy")]
    public static void SpawnNormalEnemy()
    {
        SpawnEnemy("Enemy_Normal", new Vector3(5f, 1f, 5f), isBoss: false);
    }

    [MenuItem("Game/Spawn Boss Enemy")]
    public static void SpawnBossEnemy()
    {
        SpawnEnemy("Enemy_Boss", new Vector3(10f, 1.5f, 10f), isBoss: true);
    }

    private static void SpawnEnemy(string name, Vector3 position, bool isBoss)
    {
        var go = isBoss
            ? GameObject.CreatePrimitive(PrimitiveType.Cube)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        go.name = name;
        go.tag  = "Enemy";
        go.transform.position = position;

        go.AddComponent<NavMeshAgent>();

        if (isBoss)
        {
            go.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            go.AddComponent<BossController>();
        }
        else
        {
            go.AddComponent<EnemyController>();
        }

        // Animator + EnemyAnimator を追加し Enemy_AC をアサイン
        var animator = go.AddComponent<Animator>();
        var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(EnemyACPath);
        if (ac != null)
            animator.runtimeAnimatorController = ac;
        else
            Debug.LogWarning($"[EnemySetupTool] Enemy_AC が見つかりません: {EnemyACPath}  →  Game/Setup Enemy Animator Controller を先に実行してください");

        go.AddComponent<EnemyAnimator>();

        Undo.RegisterCreatedObjectUndo(go, $"Spawn {name}");
        Selection.activeGameObject = go;
        Debug.Log($"[EnemySetupTool] {name} を配置しました（Animator + EnemyAnimator 付き）");
    }
}
