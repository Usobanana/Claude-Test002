using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// アクションノード：視野外で NavMesh 上をランダム徘徊する。
/// 目的地に到達したら waitDuration 秒待機し、次の目的地をランダムに選ぶ。
/// 常に Running を返す。
/// 作成: Project → Game/Enemy/BT/Action/Wander
/// </summary>
[CreateAssetMenu(fileName = "Act_Wander", menuName = "Game/Enemy/BT/Action/Wander")]
public class WanderNode : BehaviorNode
{
    [Tooltip("徘徊する半径（現在地からの最大距離）")]
    public float wanderRadius = 8f;

    [Tooltip("目的地到達後の待機時間（秒）")]
    public float waitDuration = 2f;

    [Tooltip("NavMesh サンプリングの最大試行距離")]
    public float navSampleDistance = 2f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        if (bb.agent == null || !bb.agent.enabled || !bb.agent.isOnNavMesh)
            return NodeState.Running;

        bb.agent.isStopped = false;

        // 目的地に到達（または未設定）→ 待機してから次の目的地を選ぶ
        bool arrived = !bb.agent.pathPending
                    && bb.agent.remainingDistance <= bb.agent.stoppingDistance;

        if (arrived || !bb.hasWanderDestination)
        {
            bb.wanderWaitTimer += Time.deltaTime;

            if (bb.wanderWaitTimer >= waitDuration)
            {
                bb.wanderWaitTimer = 0f;
                TrySetNewDestination(bb);
            }
            else if (!bb.hasWanderDestination)
            {
                // 初回はすぐに目的地を選ぶ
                TrySetNewDestination(bb);
            }
        }

        return NodeState.Running;
    }

    private void TrySetNewDestination(EnemyBlackboard bb)
    {
        Vector3 origin = bb.self.position;
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir.y = 0f;
        Vector3 candidate = origin + randomDir;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navSampleDistance, NavMesh.AllAreas))
        {
            bb.wanderDestination    = hit.position;
            bb.hasWanderDestination = true;
            bb.agent.SetDestination(hit.position);
        }
    }
}
