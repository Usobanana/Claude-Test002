using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Synty.Tools.SyntyPropBoneTool;

/// <summary>
/// PlayerAppearance の Inspector にモデル・武器差し替えボタンを追加する。
/// 再生前（Edit Mode）でも差し替えが実行できる。
/// </summary>
[CustomEditor(typeof(PlayerAppearance))]
public class PlayerAppearanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var appearance = (PlayerAppearance)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("エディター操作", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(appearance.modelPrefab == null))
        {
            if (GUILayout.Button("モデルを差し替える"))
                SwapModelInEditor(appearance);
        }

        using (new EditorGUI.DisabledScope(appearance.weaponPrefab == null))
        {
            if (GUILayout.Button("武器を装備する"))
                AttachWeaponInEditor(appearance);
        }

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(
            appearance.modelPrefab == null && appearance.weaponPrefab == null))
        {
            if (GUILayout.Button("モデルと武器を両方セットアップ"))
            {
                if (appearance.modelPrefab != null)
                    SwapModelInEditor(appearance);
                else if (appearance.weaponPrefab != null)
                    AttachWeaponInEditor(appearance);
            }
        }
    }

    // ─────────────────────────────────────────
    // モデル差し替え
    // ─────────────────────────────────────────

    private static void SwapModelInEditor(PlayerAppearance appearance)
    {
        var player = appearance.gameObject;

        // 旧モデルの情報を削除前に保存
        PropBoneConfig savedConfig   = null;
        var            savedLocalPos = Vector3.zero;
        var            savedLocalRot = Quaternion.identity;

        var existingAnim = player.GetComponentInChildren<Animator>();
        if (existingAnim != null && existingAnim.gameObject != player)
        {
            var oldModel  = existingAnim.gameObject;
            var oldBinder = oldModel.GetComponentInChildren<PropBoneBinder>();
            if (oldBinder != null) savedConfig = oldBinder.propBoneConfig;
            savedLocalPos = oldModel.transform.localPosition;
            savedLocalRot = oldModel.transform.localRotation;

            Undo.DestroyObjectImmediate(oldModel);
        }

        // 新モデルをインスタンス化
        var newModel = (GameObject)PrefabUtility.InstantiatePrefab(
            appearance.modelPrefab, player.transform);
        newModel.transform.localPosition = savedLocalPos;
        newModel.transform.localRotation = savedLocalRot;
        newModel.transform.localScale    = Vector3.one;
        Undo.RegisterCreatedObjectUndo(newModel, "Swap Player Model");

        // AnimatorController をアサイン
        var newAnim = newModel.GetComponentInChildren<Animator>()
                   ?? newModel.GetComponent<Animator>();
        if (newAnim != null && appearance.animatorController != null)
            newAnim.runtimeAnimatorController = appearance.animatorController;

        // PropBoneBinder をセットアップ（Prop_R_Socket を再生成）
        if (savedConfig != null && newAnim != null)
        {
            var binder = newModel.GetComponentInChildren<PropBoneBinder>()
                      ?? Undo.AddComponent<PropBoneBinder>(newModel);
            binder.animator       = newAnim;
            binder.propBoneConfig = savedConfig;
            binder.CreatePropBones();
            binder.BindPropBones();
            EditorUtility.SetDirty(newModel);
        }

        // 武器も再アタッチ
        if (appearance.weaponPrefab != null)
            AttachWeaponInEditor(appearance);

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[PlayerAppearance] モデルを '{appearance.modelPrefab.name}' に差し替えました");
    }

    // ─────────────────────────────────────────
    // 武器装備
    // ─────────────────────────────────────────

    private static void AttachWeaponInEditor(PlayerAppearance appearance)
    {
        var player = appearance.gameObject;
        if (appearance.weaponPrefab == null) return;

        // 既存武器を削除
        var existing = FindInChildren(player.transform, "__Weapon__");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // ボーン検索
        var bone = FindInChildren(player.transform, appearance.weaponBoneName);
        if (bone == null)
        {
            if (appearance.weaponBoneName != "Hand_R")
            {
                Debug.LogWarning(
                    $"[PlayerAppearance] ボーン '{appearance.weaponBoneName}' が見つかりません。'Hand_R' にフォールバックします。");
                bone = FindInChildren(player.transform, "Hand_R");
            }
            if (bone == null)
            {
                Debug.LogWarning("[PlayerAppearance] ボーン 'Hand_R' も見つかりません");
                return;
            }
        }

        // 武器をインスタンス化
        GameObject weapon;
        if (PrefabUtility.GetPrefabAssetType(appearance.weaponPrefab) != PrefabAssetType.NotAPrefab)
            weapon = (GameObject)PrefabUtility.InstantiatePrefab(appearance.weaponPrefab, bone);
        else
            weapon = Object.Instantiate(appearance.weaponPrefab, bone);

        weapon.name                      = "__Weapon__";
        weapon.transform.localPosition   = appearance.weaponPositionOffset;
        weapon.transform.localRotation   = Quaternion.Euler(appearance.weaponRotationOffset);
        Undo.RegisterCreatedObjectUndo(weapon, "Attach Weapon");

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[PlayerAppearance] 武器 '{appearance.weaponPrefab.name}' を '{appearance.weaponBoneName}' にアタッチしました");
    }

    // ─────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────

    private static Transform FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
