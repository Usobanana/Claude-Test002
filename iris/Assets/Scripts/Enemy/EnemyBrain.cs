using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ビヘイビアツリーを毎フレーム Tick して敵の行動を制御する。
/// EnemyController / BossController と共存する：
/// このコンポーネントが存在する場合、各コントローラーは自前の AI ループをスキップする。
/// </summary>
public class EnemyBrain : MonoBehaviour
{
    [Header("ビヘイビアツリー")]
    [Tooltip("実行する BehaviorTree アセット")]
    public BehaviorTree behaviorTree;

    [Header("被弾スタン")]
    [Tooltip("被弾時にBTと移動を止める時間（秒）。HitReactアニメーションの長さに合わせる。")]
    public float hitStunDuration = 0.6f;

    private EnemyBlackboard bb;
    private EnemyController enemy;
    private BossController  boss;

    private bool  isHitStunned;
    private float hitStunTimer;

    void Awake()
    {
        enemy = GetComponent<EnemyController>();
        boss  = GetComponent<BossController>();

        bb           = new EnemyBlackboard();
        bb.self      = transform;
        bb.agent     = GetComponent<NavMeshAgent>();
        bb.enemy     = enemy;
        bb.boss      = boss;
    }

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) bb.target = player.transform;

        // 被弾イベントを購読
        if (enemy != null) enemy.OnHitReactAnim += OnHit;
        if (boss  != null) boss.OnHitReactAnim  += OnHit;
    }

    void OnDestroy()
    {
        if (enemy != null) enemy.OnHitReactAnim -= OnHit;
        if (boss  != null) boss.OnHitReactAnim  -= OnHit;
    }

    void Update()
    {
        if (behaviorTree == null) return;

        // 死亡チェック
        bool alive = true;
        if (enemy != null && !enemy.IsAlive) alive = false;
        if (boss  != null && !boss.IsAlive)  alive = false;
        if (!alive) return;

        // 被弾スタン中: BTをスキップし、エージェントを停止
        if (isHitStunned)
        {
            hitStunTimer -= Time.deltaTime;
            if (hitStunTimer <= 0f)
                isHitStunned = false;
            else
            {
                StopAgent();
                return;
            }
        }

        // ターゲットへの距離を更新
        if (bb.target != null)
            bb.distanceToTarget = Vector3.Distance(transform.position, bb.target.position);

        behaviorTree.Tick(bb);
    }

    private void OnHit()
    {
        isHitStunned = true;
        hitStunTimer = hitStunDuration;
        StopAgent();
    }

    private void StopAgent()
    {
        if (bb?.agent != null && bb.agent.enabled && bb.agent.isOnNavMesh)
        {
            bb.agent.ResetPath();
            bb.agent.isStopped = true;
        }
    }

    /// <summary>攻撃ヒット時にノックバックを適用する。AttackHitbox から呼ばれる。</summary>
    public void ApplyKnockback(Vector3 attackerPosition, float force, float duration)
    {
        StartCoroutine(KnockbackRoutine(attackerPosition, force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 attackerPosition, float force, float duration)
    {
        if (bb?.agent == null) yield break;

        // ノックバック方向（Y成分除去・正規化）
        Vector3 dir = transform.position - attackerPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + dir * force;

        // NavMesh 上の有効な座標にクランプ
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, force + 1f, NavMesh.AllAreas))
            targetPos = navHit.position;
        else
            targetPos = startPos;

        // NavMeshAgent を一時無効化して自由移動
        bb.agent.enabled = false;

        // unscaledDeltaTime を使用してヒットストップ（timeScale 変化）の影響を受けない
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;

        // NavMeshAgent を再有効化し位置を同期
        bb.agent.enabled = true;
        if (bb.agent.isOnNavMesh)
            bb.agent.Warp(targetPos);
    }

    /// <summary>シーンビューで視野範囲を可視化する。</summary>
    void OnDrawGizmosSelected()
    {
        if (behaviorTree == null || behaviorTree.rootNode == null) return;

        // BT 内の IsPlayerInSightNode を再帰検索して視野を描画
        DrawSightGizmo(behaviorTree.rootNode);
    }

    private void DrawSightGizmo(BehaviorNode node)
    {
        if (node == null) return;

        if (node is IsPlayerInSightNode sight)
        {
            // 視野距離
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, sight.sightRange);

            // 視野角（360度未満の場合のみ扇形を表示）
            if (sight.fieldOfViewAngle < 360f)
            {
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
                float half = sight.fieldOfViewAngle * 0.5f * Mathf.Deg2Rad;
                Vector3 left  = Quaternion.Euler(0, -sight.fieldOfViewAngle * 0.5f, 0) * transform.forward * sight.sightRange;
                Vector3 right = Quaternion.Euler(0,  sight.fieldOfViewAngle * 0.5f, 0) * transform.forward * sight.sightRange;
                Gizmos.DrawLine(transform.position, transform.position + left);
                Gizmos.DrawLine(transform.position, transform.position + right);
            }
            return;
        }

        // Composite ノードを再帰的に辿る
        if (node is SelectorNode sel)
            foreach (var child in sel.children) DrawSightGizmo(child);
        else if (node is SequenceNode seq)
            foreach (var child in seq.children) DrawSightGizmo(child);
    }
}
