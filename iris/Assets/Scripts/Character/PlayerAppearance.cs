using UnityEngine;

/// <summary>
/// Playerのモデル差し替え・武器装備を管理するコンポーネント。
///
/// 使い方（Editorメニュー）:
///   1. Inspector の Model Prefab に使いたいモデルをドラッグ
///   2. Inspector の Weapon Prefab に使いたい武器をドラッグ
///   3. メニュー [Game/Setup Player Appearance] を実行
///
/// ランタイム:
///   Start() で自動的に武器を Hand_R ボーンへアタッチします。
///   モデル差し替えは Editor 専用（Setup メニューから）。
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    [Header("モデル設定")]
    [Tooltip("差し替えるキャラクターモデルの Prefab（未設定なら現状維持）")]
    [SerializeField] public GameObject modelPrefab;

    [Header("武器設定")]
    [Tooltip("装備させる武器の Prefab")]
    [SerializeField] public GameObject weaponPrefab;
    [Tooltip("武器を装着するボーン名")]
    [SerializeField] public string weaponBoneName = "Hand_R";
    [Tooltip("ボーンからの位置オフセット")]
    [SerializeField] public Vector3 weaponPositionOffset = Vector3.zero;
    [Tooltip("ボーンからの回転オフセット（オイラー角）")]
    [SerializeField] public Vector3 weaponRotationOffset = Vector3.zero;

    // ─────────────────────────────────────────

    void Start()
    {
        AttachWeapon();
    }

    /// <summary>
    /// 現在の子モデルから weaponBoneName ボーンを探し、武器をアタッチする。
    /// プレイ開始時に自動呼び出し。
    /// </summary>
    public void AttachWeapon()
    {
        if (weaponPrefab == null) return;

        // 既存の武器を削除
        var existing = transform.Find("__Weapon__");
        if (existing != null) Destroy(existing.gameObject);

        var bone = FindBone(weaponBoneName);
        if (bone == null)
        {
            Debug.LogWarning($"[PlayerAppearance] ボーン '{weaponBoneName}' が見つかりません");
            return;
        }

        var weapon = Instantiate(weaponPrefab, bone);
        weapon.name = "__Weapon__";
        weapon.transform.localPosition = weaponPositionOffset;
        weapon.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
    }

    /// <summary>子オブジェクト全体から指定名のボーンを再帰検索する</summary>
    public Transform FindBone(string boneName)
    {
        return FindInChildren(transform, boneName);
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
