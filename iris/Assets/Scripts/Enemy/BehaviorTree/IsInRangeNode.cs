using UnityEngine;

/// <summary>
/// 条件ノード：ターゲットが range 以内なら Success。
/// 作成: Project → Game/Enemy/BT/Condition/IsInRange
/// </summary>
[CreateAssetMenu(fileName = "Cond_IsInRange", menuName = "Game/Enemy/BT/Condition/IsInRange")]
public class IsInRangeNode : BehaviorNode
{
    [Tooltip("判定する距離（半径）")]
    public float range = 2f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        return bb.distanceToTarget <= range ? NodeState.Success : NodeState.Failure;
    }
}
