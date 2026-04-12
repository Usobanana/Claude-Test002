using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EnemyAppearance の Inspector にモデル・武器の操作ボタンを追加する。
/// 再生前（Edit Mode）でも差し替えが実行できる。
/// </summary>
[CustomEditor(typeof(EnemyAppearance))]
public class EnemyAppearanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var appearance = (EnemyAppearance)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("エディター操作", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(appearance.modelPrefab == null))
        {
            if (GUILayout.Button("モデルを差し替える"))
                SwapModelInEditor(appearance);
        }

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(appearance.weaponPrefab == null))
        {
            if (GUILayout.Button("武器を装備する"))
                AttachWeaponInEditor(appearance);
        }

        if (GUILayout.Button("武器を取り外す"))
            DetachWeaponInEditor(appearance);

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(
            appearance.modelPrefab == null && appearance.weaponPrefab == null))
        {
            if (GUILayout.Button("モデルと武器を両方セットアップ"))
            {
                if (appearance.modelPrefab != null) SwapModelInEditor(appearance);
                if (appearance.weaponPrefab != null) AttachWeaponInEditor(appearance);
            }
        }
    }

    // ─────────────────────────────────────────
    // モデル差し替え（Edit Mode）
    // ─────────────────────────────────────────

    private static void SwapModelInEditor(EnemyAppearance appearance)
    {
        var enemy = appearance.gameObject;

        var savedPos   = Vector3.zero;
        var savedRot   = Quaternion.identity;
        var savedScale = Vector3.one;

        var existingAnim = enemy.GetComponentInChildren<Animator>();
        if (existingAnim != null && existingAnim.gameObject != enemy)
        {
            var oldModel = existingAnim.gameObject;
            savedPos   = oldModel.transform.localPosition;
            savedRot   = oldModel.transform.localRotation;
            savedScale = oldModel.transform.localScale;
            Undo.DestroyObjectImmediate(oldModel);
        }

        var newModel = (GameObject)PrefabUtility.InstantiatePrefab(
            appearance.modelPrefab, enemy.transform);
        newModel.transform.localPosition = savedPos;
        newModel.transform.localRotation = savedRot;
        newModel.transform.localScale    = savedScale;
        Undo.RegisterCreatedObjectUndo(newModel, "Swap Enemy Model");

        var newAnim = newModel.GetComponentInChildren<Animator>()
                   ?? newModel.GetComponent<Animator>();
        if (newAnim != null && appearance.animatorController != null)
            newAnim.runtimeAnimatorController = appearance.animatorController;

        EditorUtility.SetDirty(enemy);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[EnemyAppearance] モデルを '{appearance.modelPrefab.name}' に差し替えました");
    }

    // ─────────────────────────────────────────
    // 武器装着（Edit Mode）
    // ─────────────────────────────────────────

    private static void AttachWeaponInEditor(EnemyAppearance appearance)
    {
        if (appearance.weaponPrefab == null) return;

        // 既存の武器を削除
        DetachWeaponInEditor(appearance);

        var bone = appearance.FindBone(appearance.weaponBoneName);
        if (bone == null)
        {
            Debug.LogWarning($"[EnemyAppearance] ボーン '{appearance.weaponBoneName}' が見つかりません");
            return;
        }

        var weapon = (GameObject)PrefabUtility.InstantiatePrefab(
            appearance.weaponPrefab, bone);
        weapon.transform.localPosition = appearance.weaponPositionOffset;
        weapon.transform.localRotation = Quaternion.Euler(appearance.weaponRotationOffset);
        weapon.transform.localScale    = Vector3.one;
        Undo.RegisterCreatedObjectUndo(weapon, "Attach Enemy Weapon");

        EditorUtility.SetDirty(appearance.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[EnemyAppearance] 武器 '{appearance.weaponPrefab.name}' を '{bone.name}' に装着しました");
    }

    private static void DetachWeaponInEditor(EnemyAppearance appearance)
    {
        // "__Weapon__" タグや特定の名前ではなく、武器プレハブ名で検索
        // シンプルに Hand_R 配下を丸ごと確認して既存インスタンスを削除
        if (appearance.weaponPrefab == null) return;

        var bone = appearance.FindBone(appearance.weaponBoneName);
        if (bone == null) return;

        foreach (Transform child in bone)
        {
            if (child.name.StartsWith(appearance.weaponPrefab.name))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                break;
            }
        }
    }
}
