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

    private EnemyBlackboard bb;
    private EnemyController enemy;
    private BossController  boss;

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
    }

    void Update()
    {
        if (behaviorTree == null) return;

        // 死亡チェック
        bool alive = true;
        if (enemy != null && !enemy.IsAlive) alive = false;
        if (boss  != null && !boss.IsAlive)  alive = false;
        if (!alive) return;

        // ターゲットへの距離を更新
        if (bb.target != null)
            bb.distanceToTarget = Vector3.Distance(transform.position, bb.target.position);

        behaviorTree.Tick(bb);
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
