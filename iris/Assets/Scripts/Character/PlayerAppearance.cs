using System.Collections;
using UnityEngine;
using Synty.Tools.SyntyPropBoneTool;

/// <summary>
/// Playerのモデル差し替え・武器装備を管理するコンポーネント。
///
/// 【ランタイム差し替えAPI】
///   ApplyCharacter(CharacterData) — モデルと武器を一括差し替え
///   SwapModel(GameObject)         — モデルのみ差し替え
///   SetWeapon(GameObject)         — 武器のみ差し替え
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
    [SerializeField] public string weaponBoneName = "Prop_R_Socket";
    [Tooltip("ボーンからの位置オフセット")]
    [SerializeField] public Vector3 weaponPositionOffset = Vector3.zero;
    [Tooltip("ボーンからの回転オフセット（オイラー角）")]
    [SerializeField] public Vector3 weaponRotationOffset = Vector3.zero;

    [Header("アニメーター設定")]
    [Tooltip("モデル差し替え時にアサインするAnimatorController")]
    [SerializeField] public RuntimeAnimatorController animatorController;

    // 現在のモデル GameObject（差し替え時の削除に使用）
    private GameObject currentModel;

    // ─────────────────────────────────────────

    void Start()
    {
        // 既存モデル（Animatorを持つ直接の子）を記録
        currentModel = FindModelChild();

        if (modelPrefab != null)
            SwapModel(modelPrefab);
        else
            AttachWeapon();
    }

    // ─────────────────────────────────────────
    // 公開API（ランタイム差し替え）
    // ─────────────────────────────────────────

    /// <summary>CharacterData に基づいてモデルと武器を一括差し替えする。</summary>
    public void ApplyCharacter(CharacterData data)
    {
        if (data == null) return;
        if (data.modelPrefab != null)       modelPrefab  = data.modelPrefab;
        if (data.weaponModelPrefab != null) weaponPrefab = data.weaponModelPrefab;

        if (modelPrefab != null) SwapModel(modelPrefab);
        else                     AttachWeapon();
    }

    /// <summary>
    /// キャラクターモデルをランタイムで差し替える。
    /// 既存モデルをPlayer直下から削除し、新モデルをPlayer直下に配置する。
    /// PlayerAnimator の Animator 参照も新モデルのものに更新する。
    /// </summary>
    public void SwapModel(GameObject newModelPrefab)
    {
        if (newModelPrefab == null) return;
        modelPrefab = newModelPrefab;

        // 旧モデルの PropBoneConfig と localPosition を削除前に保存
        PropBoneConfig savedPropBoneConfig = null;
        var savedLocalPosition = Vector3.zero;
        var savedLocalRotation = Quaternion.identity;
        if (currentModel == null)
            currentModel = FindModelChild();
        if (currentModel != null)
        {
            var oldBinder = currentModel.GetComponentInChildren<PropBoneBinder>();
            if (oldBinder != null) savedPropBoneConfig = oldBinder.propBoneConfig;
            savedLocalPosition = currentModel.transform.localPosition;
            savedLocalRotation = currentModel.transform.localRotation;
            Destroy(currentModel);
            currentModel = null;
        }

        // 新モデルをPlayer直下にインスタンス化
        var newModel = Instantiate(newModelPrefab, transform);
        newModel.transform.localPosition = savedLocalPosition;
        newModel.transform.localRotation = savedLocalRotation;
        newModel.transform.localScale    = Vector3.one;
        currentModel = newModel;

        // 新モデルのAnimatorを直接取得（Destroy遅延による参照ミスを防ぐ）
        var newAnim = newModel.GetComponentInChildren<Animator>()
                   ?? newModel.GetComponent<Animator>();

        // AnimatorController をアサイン
        if (animatorController != null && newAnim != null)
            newAnim.runtimeAnimatorController = animatorController;

        // PropBoneBinder をセットアップ（Prop_R_Socket を新モデルに再生成）
        if (savedPropBoneConfig != null && newAnim != null)
        {
            var binder = newModel.GetComponentInChildren<PropBoneBinder>()
                      ?? newModel.AddComponent<PropBoneBinder>();
            binder.animator       = newAnim;
            binder.propBoneConfig = savedPropBoneConfig;
            binder.CreatePropBones();
            binder.BindPropBones();
        }

        // PlayerAnimator の参照を新Animatorに更新
        GetComponent<PlayerAnimator>()?.RefreshAnimator(newAnim);

        // Destroy は遅延破棄のため、旧モデルのボーンが消えてから武器をアタッチする
        StartCoroutine(AttachWeaponNextFrame());
        Debug.Log($"[PlayerAppearance] モデルを '{newModelPrefab.name}' に差し替えました");
    }

    private IEnumerator AttachWeaponNextFrame()
    {
        yield return null; // 1フレーム待ち、Destroyされた旧モデルのボーンが消えてから実行
        AttachWeapon();
    }

    /// <summary>武器モデルのみをランタイムで差し替える。</summary>
    public void SetWeapon(GameObject newWeaponPrefab)
    {
        weaponPrefab = newWeaponPrefab;
        AttachWeapon();
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    /// <summary>Player直下でAnimatorを持つ子オブジェクトを返す。</summary>
    private GameObject FindModelChild()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Animator>() != null)
                return child.gameObject;
        }
        return null;
    }

    /// <summary>武器をボーンにアタッチする。</summary>
    public void AttachWeapon()
    {
        if (weaponPrefab == null) return;

        // 既存武器を削除（再帰検索）
        var existing = FindBone("__Weapon__");
        if (existing != null) Destroy(existing.gameObject);

        var bone = FindBone(weaponBoneName);
        if (bone == null)
        {
            if (weaponBoneName != "Hand_R")
            {
                Debug.LogWarning($"[PlayerAppearance] ボーン '{weaponBoneName}' が見つかりません。'Hand_R' にフォールバックします。");
                bone = FindBone("Hand_R");
            }
            if (bone == null)
            {
                Debug.LogWarning($"[PlayerAppearance] ボーン 'Hand_R' も見つかりません");
                return;
            }
        }

        var weapon = Instantiate(weaponPrefab, bone);
        weapon.name = "__Weapon__";
        weapon.transform.localPosition = weaponPositionOffset;
        weapon.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
    }

    /// <summary>子オブジェクト全体から指定名のボーンを再帰検索する。</summary>
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
