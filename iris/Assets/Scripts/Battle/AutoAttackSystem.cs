using UnityEngine;

/// <summary>
/// オートアタックシステム
/// 攻撃範囲（円形）内のエネミーを向きで選定し、自動的に攻撃する
/// </summary>
[RequireComponent(typeof(CharacterEntity))]
[RequireComponent(typeof(PlayerController))]
public class AutoAttackSystem : MonoBehaviour
{
    private CharacterEntity  entity;
    private PlayerController controller;
    private Animator         anim;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private float attackTimer;

    // 現在のターゲット
    public IDamageable CurrentTarget { get; private set; }
    public Transform   TargetTransform { get; private set; }

    void Awake()
    {
        entity     = GetComponent<CharacterEntity>();
        controller = GetComponent<PlayerController>();
        anim       = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!entity.IsAlive || entity.Stats == null) return;

        attackTimer += Time.deltaTime;

        UpdateTarget();

        if (CurrentTarget != null && attackTimer >= entity.Data.attackInterval)
        {
            attackTimer = 0f;
            PerformAttack();
        }
    }

    /// <summary>
    /// 攻撃範囲内のエネミーから向きで最適なターゲットを選定する
    /// </summary>
    private void UpdateTarget()
    {
        float range = entity.Data.attackRange;
        var   hits  = Physics.OverlapSphere(transform.position, range);

        if (hits.Length == 0)
        {
            CurrentTarget    = null;
            TargetTransform  = null;
            return;
        }

        Vector3 facing      = controller.FacingDirection;
        float   bestScore   = -1f;
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
                bestScore     = score;
                bestTarget    = damageable;
                bestTransform = hit.transform;
            }
        }

        CurrentTarget   = bestTarget;
        TargetTransform = bestTransform;
    }

    /// <summary>
    /// 通常攻撃を実行する（スキル倍率1.0）
    /// </summary>
    private void PerformAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;

        float damage = DamageCalculator.Calculate(
            attackerStats:    entity.Stats,
            skillMultiplier:  1.0f,
            targetDefense:    GetTargetDefense(),
            attackerLevel:    entity.Level
        );

        CurrentTarget.TakeDamage(damage);
        anim?.SetTrigger(AttackHash);
        Debug.Log($"[AutoAttack] {entity.Data.characterName} → {TargetTransform.name} : {damage} ダメージ");
    }

    private float GetTargetDefense()
    {
        var targetEntity = TargetTransform?.GetComponent<CharacterEntity>();
        return targetEntity?.Stats?.defense ?? 0f;
    }

    /// <summary>
    /// 攻撃範囲をGizmosで可視化
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (entity?.Data == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, entity.Data.attackRange);

        if (TargetTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, TargetTransform.position);
        }
    }
}
