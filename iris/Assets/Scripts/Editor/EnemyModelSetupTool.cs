using UnityEngine;
using UnityEditor;

/// <summary>
/// FieldScene の Enemy_Normal / Enemy_Boss にキャラクターモデルを追加し
/// Enemy_AC.controller を Animator に割り当てるEditorツール
/// ・ルートに直付きの空 Animator を削除してモデルFBXの子Animatorに差し替え
/// ・カプセルビジュアル（MeshRenderer）を非表示
/// ・EnemyAnimator コンポーネントが未アタッチなら追加
/// </summary>
public static class EnemyModelSetupTool
{
    private const string ControllerPath   = "Assets/Animations/Enemy_AC.controller";
    private const string NormalModelPath  = "Assets/Synty/AnimationSwordCombat/Samples/Meshes/PolygonSyntyCharacter.fbx";
    private const string BossModelPath    = "Assets/Synty/AnimationSwordCombat/Samples/Meshes/BigRig_01.fbx";

    [MenuItem("Game/Setup Enemy Models (FieldScene)")]
    public static void SetupEnemyModels()
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[EnemyModelSetup] Controller が見つかりません: {ControllerPath}");
            return;
        }

        // Enemy_Normal
        var normal = GameObject.Find("Enemy_Normal");
        if (normal != null)
            SetupEnemy(normal, NormalModelPath, controller, "PolygonSyntyCharacter", 1f);
        else
            Debug.LogWarning("[EnemyModelSetup] Enemy_Normal が見つかりません");

        // Enemy_Boss
        var boss = GameObject.Find("Enemy_Boss");
        if (boss != null)
            SetupEnemy(boss, BossModelPath, controller, "BigRig_01", 1.3f);
        else
            Debug.LogWarning("[EnemyModelSetup] Enemy_Boss が見つかりません");

        Debug.Log("[EnemyModelSetup] 完了 → シーンを保存してください");
    }

    private static void SetupEnemy(GameObject enemy, string modelPath,
        RuntimeAnimatorController controller, string childName, float scale)
    {
        // 1. カプセルビジュアルを非表示
        var meshRenderer = enemy.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.enabled)
        {
            Undo.RecordObject(meshRenderer, "Disable Capsule Renderer");
            meshRenderer.enabled = false;
        }

        // 2. ルートに直付きの Animator（avatar=null のもの）を削除
        var rootAnim = enemy.GetComponent<Animator>();
        if (rootAnim != null && rootAnim.avatar == null)
        {
            Undo.DestroyObjectImmediate(rootAnim);
            Debug.Log($"[EnemyModelSetup] {enemy.name}: ルートAnimatorを削除しました");
        }

        // 3. モデル子オブジェクトがなければ追加
        var existingChild = enemy.transform.Find(childName);
        if (existingChild == null)
        {
            var modelFbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelFbx == null)
            {
                Debug.LogError($"[EnemyModelSetup] モデルが見つかりません: {modelPath}");
                return;
            }

            var modelGo = (GameObject)PrefabUtility.InstantiatePrefab(modelFbx, enemy.transform);
            modelGo.name = childName;
            modelGo.transform.localPosition = Vector3.zero;
            modelGo.transform.localRotation = Quaternion.identity;
            modelGo.transform.localScale    = Vector3.one * scale;
            Undo.RegisterCreatedObjectUndo(modelGo, $"Add {childName}");
            existingChild = modelGo.transform;
            Debug.Log($"[EnemyModelSetup] {enemy.name}: {childName} を追加しました");
        }

        // 4. 子の Animator に Controller を割り当て
        var anim = existingChild.GetComponent<Animator>();
        if (anim == null)
        {
            anim = Undo.AddComponent<Animator>(existingChild.gameObject);
        }

        if (anim.runtimeAnimatorController != controller)
        {
            Undo.RecordObject(anim, "Assign Enemy_AC");
            anim.runtimeAnimatorController = controller;
            Debug.Log($"[EnemyModelSetup] {enemy.name}: Enemy_AC.controller を割り当てました");
        }

        // 5. EnemyAnimator がなければ追加
        if (enemy.GetComponent<EnemyAnimator>() == null)
        {
            Undo.AddComponent<EnemyAnimator>(enemy);
            Debug.Log($"[EnemyModelSetup] {enemy.name}: EnemyAnimator を追加しました");
        }

        EditorUtility.SetDirty(enemy);
    }
}
