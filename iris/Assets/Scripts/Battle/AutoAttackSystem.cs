using UnityEngine;

/// <summary>
/// オートアタックシステム
/// ・AUTO ON  : attackInterval ごとにターゲット更新 → 向き補正 → アニメーション再生
/// ・AUTO OFF : PlayerAnimator(Input モード)のコンボ進行時にターゲット更新・向き補正のみ
///
/// ダメージ処理は Animation Event 経由で AttackHitbox が担当する。
/// </summary>
[RequireComponent(typeof(CharacterEntity))]
[RequireComponent(typeof(PlayerController))]
public class AutoAttackSystem : MonoBehaviour
{
    private CharacterEntity       entity;
    private PlayerController      controller;
    private PlayerAnimator        playerAnim;
    private AttackRangeIndicator  rangeIndicator;

    private float attackTimer;

    // --- 公開プロパティ ---
    public IDamageable CurrentTarget   { get; private set; }
    public Transform   TargetTransform { get; private set; }
    public bool        IsAutoMode      { get; private set; } = false;

    void Awake()
    {
        entity         = GetComponent<CharacterEntity>();
        controller     = GetComponent<PlayerController>();
        playerAnim     = GetComponent<PlayerAnimator>();
        rangeIndicator = GetComponent<AttackRangeIndicator>();
    }

    // ─────────────────────────────────────────
    // 公開メソッド
    // ─────────────────────────────────────────

    /// <summary>AUTO モードを切り替える（PlayerHUD のボタンから呼ぶ）</summary>
    public void SetAutoMode(bool enabled)
    {
        IsAutoMode = enabled;

        if (playerAnim != null)
            playerAnim.Mode = enabled ? ComboMode.Auto : ComboMode.Input;

        if (rangeIndicator != null)
            rangeIndicator.SetVisible(enabled);

        if (!enabled) ClearTarget();
    }

    /// <summary>
    /// Input モード時に PlayerAnimator.AdvanceCombo() から呼ばれる。
    /// ターゲット更新と向き補正のみ行う。ダメージは AttackHitbox が処理。
    /// </summary>
    public void OnComboHit()
    {
        if (entity == null || entity.Stats == null) return;
        UpdateTarget();

        if (TargetTransform != null)
            controller.FaceToward(TargetTransform.position);
    }

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    void Update()
    {
        if (!entity.IsAlive || entity.Stats == null) return;
        if (!IsAutoMode) return;

        attackTimer += Time.deltaTime;
        UpdateTarget();

        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        if (attackTimer < entity.Data.attackInterval) return;

        attackTimer = 0f;

        // ターゲットへ向く
        if (TargetTransform != null)
            controller.FaceToward(TargetTransform.position);

        // アニメーション再生（ダメージは Animation Event で AttackHitbox が処理）
        if (playerAnim != null)
            playerAnim.TriggerAutoAttack();
    }

    // ─────────────────────────────────────────
    // 内部ロジック
    // ─────────────────────────────────────────

    private void UpdateTarget()
    {
        if (entity.Data == null) return;

        float range = entity.Data.attackRange;
        var   hits  = Physics.OverlapSphere(transform.position, range);

        if (hits.Length == 0) { ClearTarget(); return; }

        Vector3     facing        = controller.FacingDirection;
        float       bestScore     = -1f;
        IDamageable bestTarget    = null;
        Transform   bestTransform = null;

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) continue;

            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float   score    = Vector3.Dot(facing, toTarget);

            if (score > bestScore)
            {
                bestScore      = score;
                bestTarget     = damageable;
                bestTransform  = hit.transform;
            }
        }

        CurrentTarget   = bestTarget;
        TargetTransform = bestTransform;
    }

    private void ClearTarget()
    {
        CurrentTarget   = null;
        TargetTransform = null;
    }

    void OnDrawGizmos()
    {
        if (entity?.Data == null) return;

        // ターゲット索敵範囲（薄い黄色）
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, entity.Data.attackRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, entity.Data.attackRange);

        // 現在のターゲットへの線
        if (TargetTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, TargetTransform.position);
            Gizmos.DrawWireSphere(TargetTransform.position, 0.2f);
        }
    }
}
