using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 基本エネミーコントローラー（ザコ・エリートザコ・ボス共通基盤）
/// IDamageable を実装してダメージを受けられるようにする
/// NavMeshAgent がある場合はそれを使用し、なければ Transform で直接移動する
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("ステータス")]
    [SerializeField] private float maxHp         = 300f;
    [SerializeField] private float defense       = 20f;
    [SerializeField] private float attackPower   = 30f;
    [SerializeField] private float attackRange   = 1.5f;
    [SerializeField] private float attackInterval= 2f;
    [SerializeField] private float moveSpeed     = 3f;
    [SerializeField] private ElementType element = ElementType.Fire;
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;

    // --- IDamageable ---
    public bool        IsAlive   => currentHp > 0f;
    public float       Defense   => defense;
    public ElementType Element   => element;
    public EnemyType   EnemyType => enemyType;

    // アニメーションイベント
    public event System.Action OnAttackAnim;
    public event System.Action OnHitReactAnim;
    public event System.Action OnDeathAnim;

    private float        currentHp;
    private float        attackTimer;
    private Transform    playerTransform;
    private NavMeshAgent agent;

    void Awake()
    {
        agent     = GetComponent<NavMeshAgent>();
        currentHp = maxHp;
    }

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (!IsAlive) return;
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // 移動
        if (dist > attackRange)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(playerTransform.position);
            }
            else
            {
                // NavMesh がない場合は直接移動
                Vector3 dir = (playerTransform.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
            }
        }

        // 攻撃
        if (dist <= attackRange)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                AttackPlayer();
            }
        }
    }

    public void TakeDamage(float damage, ElementType attackElement = ElementType.Fire)
    {
        if (!IsAlive) return;
        // 防御補正（簡易：防御力の10%をダメージ軽減として適用）
        float mitigated = Mathf.Max(1f, damage - defense * 0.1f);
        currentHp = Mathf.Max(0f, currentHp - mitigated);
        Debug.Log($"[Enemy] {gameObject.name} が {mitigated} ダメージ受けた / HP: {currentHp}/{maxHp}");

        if (!IsAlive)
        {
            HandleDeath();
        }
        else
        {
            OnHitReactAnim?.Invoke();
        }
    }

    private void AttackPlayer()
    {
        var playerEntity = playerTransform.GetComponent<CharacterEntity>();
        if (playerEntity == null || !playerEntity.IsAlive) return;
        playerEntity.TakeDamage(attackPower);
        Debug.Log($"[Enemy] {gameObject.name} がプレイヤーに {attackPower} ダメージ");
        OnAttackAnim?.Invoke();
    }

    private void HandleDeath()
    {
        Debug.Log($"[Enemy] {gameObject.name} 死亡");
        if (agent != null) agent.enabled = false;
        OnDeathAnim?.Invoke();
        Destroy(gameObject, 1.5f);
    }

    // HP の割合（HUDなどで使用）
    public float HpRatio => maxHp > 0f ? currentHp / maxHp : 0f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

public enum EnemyType
{
    Normal,   // ザコ
    Elite,    // エリートザコ
    Boss,     // ボス
}
