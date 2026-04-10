using UnityEngine;
using UnityEditor;

/// <summary>
/// 各シーンの Player を正しくセットアップするEditorツール
/// ・カプセルビジュアルを非表示にしてPOLYGONRig_01モデルを子として追加
/// ・Animator に Player_AC.controller を割り当て
/// ・PlayerAnimator コンポーネントが未アタッチなら追加
/// </summary>
public static class PlayerSetupTool
{
    private const string ModelPath      = "Assets/Synty/AnimationSwordCombat/Samples/Meshes/POLYGONRig_01.fbx";
    private const string ControllerPath = "Assets/Animations/Player_AC.controller";
    private const string ModelChildName = "POLYGONRig_01";

    [MenuItem("Game/Setup Player (Current Scene)")]
    public static void SetupPlayer()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[PlayerSetup] Player タグのオブジェクトが見つかりません");
            return;
        }

        // 1. ルートのカプセルビジュアルを非表示（MeshRenderer/MeshFilter は残す）
        var meshRenderer = player.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.enabled)
        {
            Undo.RecordObject(meshRenderer, "Disable Capsule Renderer");
            meshRenderer.enabled = false;
            Debug.Log("[PlayerSetup] ルートMeshRenderer を非表示にしました");
        }

        // 2. POLYGONRig_01 の子オブジェクトがなければ追加
        var existingModel = player.transform.Find(ModelChildName);
        if (existingModel == null)
        {
            var modelFbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelFbx == null)
            {
                Debug.LogError($"[PlayerSetup] モデルが見つかりません: {ModelPath}");
                return;
            }

            var modelGo = (GameObject)PrefabUtility.InstantiatePrefab(modelFbx, player.transform);
            modelGo.name = ModelChildName;
            modelGo.transform.localPosition = Vector3.zero;
            modelGo.transform.localRotation = Quaternion.identity;
            modelGo.transform.localScale    = Vector3.one;
            Undo.RegisterCreatedObjectUndo(modelGo, "Add Player Model");
            existingModel = modelGo.transform;
            Debug.Log("[PlayerSetup] POLYGONRig_01 を子として追加しました");
        }

        // 3. Animator に Player_AC.controller を割り当て
        var animator = existingModel.GetComponent<Animator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<Animator>(existingModel.gameObject);
            Debug.Log("[PlayerSetup] Animator を追加しました");
        }

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[PlayerSetup] Controllerが見つかりません: {ControllerPath}");
            return;
        }

        if (animator.runtimeAnimatorController != controller)
        {
            Undo.RecordObject(animator, "Assign Player_AC");
            animator.runtimeAnimatorController = controller;
            Debug.Log("[PlayerSetup] Player_AC.controller を割り当てました");
        }
        else
        {
            Debug.Log("[PlayerSetup] Controller は既に割り当て済みです");
        }

        // 4. PlayerAnimator がなければ追加
        if (player.GetComponent<PlayerAnimator>() == null)
        {
            Undo.AddComponent<PlayerAnimator>(player);
            Debug.Log("[PlayerSetup] PlayerAnimator を追加しました");
        }

        EditorUtility.SetDirty(player);
        Debug.Log("[PlayerSetup] 完了 → シーンを保存してください");
    }
}
