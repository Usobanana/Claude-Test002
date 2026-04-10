using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// ボスコントローラー
/// HPフェーズ（Phase1/2）で行動パターンが変わる
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossController : MonoBehaviour, IDamageable
{
    [Header("ステータス")]
    [SerializeField] private float maxHp          = 1500f;
    [SerializeField] private float defense        = 30f;
    [SerializeField] private ElementType element  = ElementType.Fire;

    [Header("Phase1 行動（HP 100〜50%）")]
    [SerializeField] private float p1MoveSpeed    = 3f;
    [SerializeField] private float p1AttackRange  = 2.5f;
    [SerializeField] private float p1AttackPower  = 25f;
    [SerializeField] private float p1AttackInterval = 2f;

    [Header("Phase2 行動（HP 50%以下）")]
    [SerializeField] private float p2MoveSpeed    = 4f;
    [SerializeField] private float p2AttackRange  = 3.5f;
    [SerializeField] private float p2AttackPower  = 40f;
    [SerializeField] private float p2AttackInterval = 1.5f;

    [Header("特殊攻撃（Phase2で解放）")]
    [SerializeField] private float chargeAttackCooldown = 12f;
    [SerializeField] private float chargeAttackPower    = 70f;
    [SerializeField] private float chargeAttackRange    = 6f;

    // --- IDamageable ---
    public bool        IsAlive  => currentHp > 0f;
    public float       HpRatio  => maxHp > 0f ? currentHp / maxHp : 0f;
    public ElementType Element  => element;

    // 現在のフェーズ
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

    private float currentHp;
    private float attackTimer;
    private float chargeTimer;
    private bool  isPhaseTransitioning;
    private bool  isChargingAttack;

    private Transform    playerTransform;
    private NavMeshAgent agent;

    // イベント
    public event System.Action<BossPhase> OnPhaseChanged;
    public event System.Action            OnBossDeath;

    void Awake()
    {
        agent     = GetComponent<NavMeshAgent>();
        currentHp = maxHp;
        ApplyPhaseSettings();
    }

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (!IsAlive || isPhaseTransitioning) return;
        if (playerTransform == null) return;

        CheckPhaseTransition();
        BehaviorUpdate();
    }

    // --- フェーズ判定 ---

    private void CheckPhaseTransition()
    {
        if (CurrentPhase == BossPhase.Phase1 && HpRatio <= 0.5f)
        {
            StartCoroutine(TransitionToPhase2());
        }
    }

    private IEnumerator TransitionToPhase2()
    {
        isPhaseTransitioning = true;
        CurrentPhase = BossPhase.Phase2;

        if (agent.isOnNavMesh) agent.isStopped = true;
        Debug.Log("[Boss] Phase2 移行！");

        // TODO: フェーズ移行演出（アニメーション・エフェクト）
        yield return new WaitForSeconds(1.5f);

        ApplyPhaseSettings();
        if (agent.isOnNavMesh) agent.isStopped = false;
        isPhaseTransitioning = false;

        OnPhaseChanged?.Invoke(BossPhase.Phase2);
    }

    private void ApplyPhaseSettings()
    {
        if (CurrentPhase == BossPhase.Phase1)
        {
            agent.speed = p1MoveSpeed;
        }
        else
        {
            agent.speed = p2MoveSpeed;
        }
    }

    // --- 行動ループ ---

    private void BehaviorUpdate()
    {
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        float atkRange    = CurrentPhase == BossPhase.Phase1 ? p1AttackRange : p2AttackRange;
        float atkInterval = CurrentPhase == BossPhase.Phase1 ? p1AttackInterval : p2AttackInterval;

        // 移動（NavMeshがベイク済みの場合のみ）
        if (dist > atkRange)
        {
            if (agent.isOnNavMesh)
                agent.SetDestination(playerTransform.position);
            else
            {
                Vector3 dir = (playerTransform.position - transform.position).normalized;
                transform.position += dir * (CurrentPhase == BossPhase.Phase1 ? p1MoveSpeed : p2MoveSpeed) * Time.deltaTime;
            }
        }
        else if (agent.isOnNavMesh)
            agent.ResetPath();

        // 通常攻撃（チャージ中は行わない）
        if (!isChargingAttack)
        {
            attackTimer += Time.deltaTime;
            if (dist <= atkRange && attackTimer >= atkInterval)
            {
                attackTimer = 0f;
                NormalAttack();
            }
        }

        // 特殊攻撃（Phase2のみ）
        if (CurrentPhase == BossPhase.Phase2 && !isChargingAttack)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= chargeAttackCooldown)
            {
                chargeTimer = 0f;
                StartCoroutine(ChargeAttack());
            }
        }
    }

    private void NormalAttack()
    {
        var target = playerTransform.GetComponent<CharacterEntity>();
        if (target == null || !target.IsAlive) return;

        float power = CurrentPhase == BossPhase.Phase1 ? p1AttackPower : p2AttackPower;
        target.TakeDamage(power);
        Debug.Log($"[Boss] 通常攻撃 → {power} ダメージ (Phase{(int)CurrentPhase + 1})");
    }

    private IEnumerator ChargeAttack()
    {
        isChargingAttack = true;
        Debug.Log("[Boss] チャージ攻撃 準備中...");
        if (agent.isOnNavMesh) agent.isStopped = true;

        // TODO: チャージ演出
        yield return new WaitForSeconds(1f);

        // 範囲内の全プレイヤーにダメージ
        var hits = Physics.OverlapSphere(transform.position, chargeAttackRange);
        foreach (var hit in hits)
        {
            var target = hit.GetComponent<CharacterEntity>();
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(chargeAttackPower);
                Debug.Log($"[Boss] チャージ攻撃 → {chargeAttackPower} ダメージ");
            }
        }

        if (agent.isOnNavMesh) agent.isStopped = false;
        isChargingAttack = false;
    }

    // --- IDamageable ---

    public void TakeDamage(float damage, ElementType attackElement = ElementType.Fire)
    {
        if (!IsAlive) return;
        float mitigated = Mathf.Max(1f, damage - defense * 0.1f);
        currentHp = Mathf.Max(0f, currentHp - mitigated);
        Debug.Log($"[Boss] {mitigated} ダメージ / HP: {currentHp:F0}/{maxHp} ({HpRatio * 100:F0}%)");

        if (!IsAlive) OnDeath();
    }

    private void OnDeath()
    {
        Debug.Log("[Boss] 撃破！");
        agent.enabled = false;
        OnBossDeath?.Invoke();
        // TODO: 死亡演出・クエストクリア通知
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        float atkRange = CurrentPhase == BossPhase.Phase1 ? p1AttackRange : p2AttackRange;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, atkRange);

        if (CurrentPhase == BossPhase.Phase2)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, chargeAttackRange);
        }
    }
}

public enum BossPhase
{
    Phase1,
    Phase2,
}
