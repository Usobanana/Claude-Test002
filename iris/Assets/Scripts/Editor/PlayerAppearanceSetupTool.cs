using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Playerのモデル差し替え・武器装備を Editor から実行するツール。
///
/// 使い方：
///   1. Player Inspector の PlayerAppearance に weaponPrefab をドラッグ（または自動設定）
///   2. [Game/Setup Player Appearance] を実行
///
/// 武器アセットの自動設定：
///   [Game/Setup Player Sword (Default)] を実行すると SM_Wep_Sword_01 が自動アサインされる
/// </summary>
public static class PlayerAppearanceSetupTool
{
    private const string SwordFbxPath = "Assets/Synty/PolygonFantasyKingdom/Models/SM_Wep_Sword_01.fbx";

    // ─────────────────────────────────────────
    // 剣をデフォルト設定してセットアップ（ワンクリック）
    // ─────────────────────────────────────────
    [MenuItem("Game/Setup Player Sword (Default)")]
    public static void SetupWithDefaultSword()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogError("[PlayerAppearance] Player タグのオブジェクトが見つかりません"); return; }

        var appearance = player.GetComponent<PlayerAppearance>();
        if (appearance == null)
            appearance = Undo.AddComponent<PlayerAppearance>(player);

        // SM_Wep_Sword_01 の GameObject サブアセットを取得してプレハブとして使用
        var swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordFbxPath);
        if (swordPrefab == null) { Debug.LogError($"[PlayerAppearance] {SwordFbxPath} が見つかりません"); return; }

        var so = new SerializedObject(appearance);
        so.FindProperty("weaponPrefab").objectReferenceValue    = swordPrefab;
        so.FindProperty("weaponBoneName").stringValue           = "Hand_R";
        so.FindProperty("weaponPositionOffset").vector3Value    = Vector3.zero;
        so.FindProperty("weaponRotationOffset").vector3Value    = Vector3.zero;
        so.ApplyModifiedProperties();

        AttachWeaponInEditor(player, appearance);

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PlayerAppearance] 剣のセットアップ完了");
    }

    // ─────────────────────────────────────────
    // 汎用セットアップ（Inspector で設定してから実行）
    // ─────────────────────────────────────────
    [MenuItem("Game/Setup Player Appearance")]
    public static void Setup()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogError("[PlayerAppearance] Player タグのオブジェクトが見つかりません"); return; }

        var appearance = player.GetComponent<PlayerAppearance>();
        if (appearance == null) { Debug.LogError("[PlayerAppearance] PlayerAppearance コンポーネントがありません"); return; }

        if (appearance.modelPrefab != null)
            SwapModel(player, appearance);

        if (appearance.weaponPrefab != null)
            AttachWeaponInEditor(player, appearance);

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[PlayerAppearance] セットアップ完了");
    }

    // ─────────────────────────────────────────
    // モデル差し替え
    // ─────────────────────────────────────────
    private static void SwapModel(GameObject player, PlayerAppearance appearance)
    {
        var animator = player.GetComponentInChildren<Animator>();
        if (animator != null && animator.gameObject != player)
        {
            Debug.Log($"[PlayerAppearance] 既存モデル '{animator.gameObject.name}' を削除");
            Undo.DestroyObjectImmediate(animator.gameObject);
        }

        var newModel = (GameObject)PrefabUtility.InstantiatePrefab(appearance.modelPrefab, player.transform);
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;
        newModel.transform.localScale    = Vector3.one;
        Undo.RegisterCreatedObjectUndo(newModel, "Swap Player Model");

        var newAnimator = newModel.GetComponentInChildren<Animator>() ?? newModel.GetComponent<Animator>();
        if (newAnimator != null)
        {
            var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player_AC.controller");
            if (ac != null) newAnimator.runtimeAnimatorController = ac;
        }

        Debug.Log($"[PlayerAppearance] モデルを '{appearance.modelPrefab.name}' に差し替えました");
    }

    // ─────────────────────────────────────────
    // 武器装備（Editor 上）
    // ─────────────────────────────────────────
    private static void AttachWeaponInEditor(GameObject player, PlayerAppearance appearance)
    {
        // 既存武器を削除
        var existing = FindInChildren(player.transform, "__Weapon__");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        var bone = FindInChildren(player.transform, appearance.weaponBoneName);
        if (bone == null)
        {
            Debug.LogWarning($"[PlayerAppearance] ボーン '{appearance.weaponBoneName}' が見つかりません");
            return;
        }

        // FBX の場合は InstantiatePrefab、なければ通常 Instantiate
        GameObject weapon;
        if (PrefabUtility.GetPrefabAssetType(appearance.weaponPrefab) != PrefabAssetType.NotAPrefab)
            weapon = (GameObject)PrefabUtility.InstantiatePrefab(appearance.weaponPrefab, bone);
        else
            weapon = Object.Instantiate(appearance.weaponPrefab, bone);

        weapon.name = "__Weapon__";
        weapon.transform.localPosition = appearance.weaponPositionOffset;
        weapon.transform.localRotation = Quaternion.Euler(appearance.weaponRotationOffset);
        Undo.RegisterCreatedObjectUndo(weapon, "Attach Weapon");

        Debug.Log($"[PlayerAppearance] 武器 '{appearance.weaponPrefab.name}' を '{appearance.weaponBoneName}' にアタッチしました");
    }

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
