using UnityEngine;

/// <summary>
/// アクションノード：その場で停止する。常に Success を返す。
/// 作成: Project → Game/Enemy/BT/Action/Idle
/// </summary>
[CreateAssetMenu(fileName = "Act_Idle", menuName = "Game/Enemy/BT/Action/Idle")]
public class IdleNode : BehaviorNode
{
    public override NodeState Tick(EnemyBlackboard bb)
    {
        if (bb.agent != null && bb.agent.enabled && bb.agent.isOnNavMesh)
        {
            bb.agent.ResetPath();
            bb.agent.isStopped = true;
        }
        return NodeState.Success;
    }
}
