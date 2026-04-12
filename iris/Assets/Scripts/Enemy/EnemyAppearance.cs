using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy のモデル・武器差し替えを管理するコンポーネント。
/// PlayerAppearance の Enemy 版（PropBone なし、武器はオプション）。
///
/// 【ランタイム差し替えAPI】
///   SwapModel(GameObject)  — モデルをランタイムで差し替え
///   AttachWeapon()         — weaponPrefab を骨に装着
///   SetWeapon(GameObject)  — 武器プレハブを変更して装着
///   DetachWeapon()         — 現在の武器を取り外す
///
/// 【Inspector ボタン（Edit Mode）】
///   EnemyAppearanceEditor から実行
/// </summary>
public class EnemyAppearance : MonoBehaviour
{
    [Header("モデル設定")]
    [Tooltip("差し替えるモデルの Prefab（未設定なら現状維持）")]
    [SerializeField] public GameObject modelPrefab;

    [Header("武器設定（オプション）")]
    [Tooltip("装備させる武器 Prefab（未設定なら武器なし）")]
    [SerializeField] public GameObject weaponPrefab;
    [Tooltip("武器を装着するボーン名（例: Hand_R）")]
    [SerializeField] public string weaponBoneName = "Hand_R";
    [Tooltip("武器の位置オフセット")]
    [SerializeField] public Vector3 weaponPositionOffset = Vector3.zero;
    [Tooltip("武器の回転オフセット")]
    [SerializeField] public Vector3 weaponRotationOffset = new Vector3(180f, 0f, 0f);

    [Header("アニメーター設定")]
    [Tooltip("モデル差し替え時にアサインするAnimatorController")]
    [SerializeField] public RuntimeAnimatorController animatorController;

    private GameObject currentModel;
    private GameObject currentWeapon;

    // ─────────────────────────────────────────

    void Start()
    {
        currentModel = FindModelChild();
        if (modelPrefab != null)
            SwapModel(modelPrefab);
        else if (weaponPrefab != null)
            AttachWeapon();
    }

    // ─────────────────────────────────────────
    // モデル操作 API
    // ─────────────────────────────────────────

    /// <summary>モデルをランタイムで差し替える。</summary>
    public void SwapModel(GameObject newModelPrefab)
    {
        if (newModelPrefab == null) return;
        modelPrefab = newModelPrefab;

        // 旧モデルの Transform を保存
        var savedPos   = Vector3.zero;
        var savedRot   = Quaternion.identity;
        var savedScale = Vector3.one;

        if (currentModel == null)
            currentModel = FindModelChild();
        if (currentModel != null)
        {
            savedPos   = currentModel.transform.localPosition;
            savedRot   = currentModel.transform.localRotation;
            savedScale = currentModel.transform.localScale;
            Destroy(currentModel);
            currentModel = null;
        }

        // 新モデルをインスタンス化
        var newModel = Instantiate(newModelPrefab, transform);
        newModel.transform.localPosition = savedPos;
        newModel.transform.localRotation = savedRot;
        newModel.transform.localScale    = savedScale;
        currentModel = newModel;

        // 新モデルのAnimatorを直接取得（Destroy遅延による参照ミスを防ぐ）
        var newAnim = newModel.GetComponentInChildren<Animator>()
                   ?? newModel.GetComponent<Animator>();

        if (animatorController != null && newAnim != null)
            newAnim.runtimeAnimatorController = animatorController;

        GetComponent<EnemyAnimator>()?.RefreshAnimator(newAnim);

        // 武器は Destroy 完了後に再装着（1フレーム待つ）
        if (weaponPrefab != null)
            StartCoroutine(AttachWeaponNextFrame());

        Debug.Log($"[EnemyAppearance] モデルを '{newModelPrefab.name}' に差し替えました");
    }

    // ─────────────────────────────────────────
    // 武器操作 API
    // ─────────────────────────────────────────

    /// <summary>weaponPrefab を指定ボーンに装着する。</summary>
    public void AttachWeapon()
    {
        if (weaponPrefab == null) return;

        DetachWeapon(); // 既存の武器を取り外してから装着

        Transform bone = FindBone(weaponBoneName);
        if (bone == null)
        {
            Debug.LogWarning($"[EnemyAppearance] ボーン '{weaponBoneName}' が見つかりません");
            return;
        }

        currentWeapon = Instantiate(weaponPrefab, bone);
        currentWeapon.transform.localPosition = weaponPositionOffset;
        currentWeapon.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
        currentWeapon.transform.localScale    = Vector3.one;
        Debug.Log($"[EnemyAppearance] 武器 '{weaponPrefab.name}' を '{bone.name}' に装着しました");
    }

    /// <summary>武器プレハブを変更して装着する。</summary>
    public void SetWeapon(GameObject newWeaponPrefab)
    {
        weaponPrefab = newWeaponPrefab;
        if (weaponPrefab != null)
            AttachWeapon();
        else
            DetachWeapon();
    }

    /// <summary>現在装着している武器を取り外す。</summary>
    public void DetachWeapon()
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
        }
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    private IEnumerator AttachWeaponNextFrame()
    {
        yield return null; // Destroy 完了を待つ
        AttachWeapon();
    }

    /// <summary>Animator を持つ直接の子を返す。</summary>
    private GameObject FindModelChild()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Animator>() != null)
                return child.gameObject;
        }
        return null;
    }

    /// <summary>ボーンを再帰検索する。</summary>
    public Transform FindBone(string boneName)
    {
        return FindBoneRecursive(transform, boneName);
    }

    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName) return parent;
        foreach (Transform child in parent)
        {
            var result = FindBoneRecursive(child, boneName);
            if (result != null) return result;
        }
        return null;
    }
}
