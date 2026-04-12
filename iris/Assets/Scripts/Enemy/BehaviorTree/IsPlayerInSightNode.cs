using UnityEngine;

/// <summary>
/// 条件ノード：Playerが視野範囲内にいれば Success。
/// 距離チェックと視野角チェックを組み合わせる。
/// 作成: Project → Game/Enemy/BT/Condition/IsPlayerInSight
/// </summary>
[CreateAssetMenu(fileName = "Cond_IsPlayerInSight", menuName = "Game/Enemy/BT/Condition/IsPlayerInSight")]
public class IsPlayerInSightNode : BehaviorNode
{
    [Tooltip("視野距離（この距離以内なら検知可能）")]
    public float sightRange = 10f;

    [Tooltip("視野角（度）。360 にすると全方位検知。")]
    [Range(1f, 360f)]
    public float fieldOfViewAngle = 120f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        if (bb.target == null) return NodeState.Failure;

        // 距離チェック
        if (bb.distanceToTarget > sightRange) return NodeState.Failure;

        // 視野角チェック（360度なら省略）
        if (fieldOfViewAngle < 360f)
        {
            Vector3 dirToTarget = (bb.target.position - bb.self.position).normalized;
            float angle = Vector3.Angle(bb.self.forward, dirToTarget);
            if (angle > fieldOfViewAngle * 0.5f) return NodeState.Failure;
        }

        return NodeState.Success;
    }

    void OnDrawGizmosSelected() { /* シーンビューでの可視化は EnemyBrain 側で行う */ }
}
