using UnityEngine;

/// <summary>
/// アクションノード：ターゲットに向かって移動する。Running を返し続ける。
/// 作成: Project → Game/Enemy/BT/Action/MoveToTarget
/// </summary>
[CreateAssetMenu(fileName = "Act_MoveToTarget", menuName = "Game/Enemy/BT/Action/MoveToTarget")]
public class MoveToTargetNode : BehaviorNode
{
    [Tooltip("NavMeshAgent の停止距離")]
    public float stoppingDistance = 0.5f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        if (bb.target == null) return NodeState.Failure;

        if (bb.agent != null && bb.agent.enabled && bb.agent.isOnNavMesh)
        {
            bb.agent.isStopped        = false;
            bb.agent.stoppingDistance = stoppingDistance;
            bb.agent.SetDestination(bb.target.position);
        }
        else
        {
            // NavMesh なし：直接移動
            float speed = bb.agent != null ? bb.agent.speed : 3f;
            Vector3 dir = (bb.target.position - bb.self.position).normalized;
            bb.self.position += dir * speed * Time.deltaTime;
            bb.self.LookAt(new Vector3(
                bb.target.position.x, bb.self.position.y, bb.target.position.z));
        }

        return NodeState.Running;
    }
}
