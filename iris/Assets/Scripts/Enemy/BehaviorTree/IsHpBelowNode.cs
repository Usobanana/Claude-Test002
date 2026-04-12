using UnityEngine;

/// <summary>
/// 条件ノード：HP が threshold 以下なら Success。
/// ボスのフェーズ移行判定などに使う。
/// 作成: Project → Game/Enemy/BT/Condition/IsHpBelow
/// </summary>
[CreateAssetMenu(fileName = "Cond_IsHpBelow", menuName = "Game/Enemy/BT/Condition/IsHpBelow")]
public class IsHpBelowNode : BehaviorNode
{
    [Range(0f, 1f)]
    [Tooltip("HP割合のしきい値（0.5 = 50%以下）")]
    public float threshold = 0.5f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        float ratio = 1f;
        if      (bb.enemy != null) ratio = bb.enemy.HpRatio;
        else if (bb.boss  != null) ratio = bb.boss.HpRatio;
        return ratio <= threshold ? NodeState.Success : NodeState.Failure;
    }
}
