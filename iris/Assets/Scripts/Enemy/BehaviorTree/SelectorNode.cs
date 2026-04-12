using UnityEngine;

/// <summary>
/// Selector（OR）ノード。
/// 子を順番に実行し、最初に Success / Running を返した子でそのまま終了。
/// 全子が Failure を返した場合のみ Failure。
///
/// 使い方: 優先順位の高い行動から並べる（例: 攻撃 → 追跡 → 待機）
/// 作成: Project → Game/Enemy/BT/Selector
/// </summary>
[CreateAssetMenu(fileName = "Selector", menuName = "Game/Enemy/BT/Selector")]
public class SelectorNode : BehaviorNode
{
    public BehaviorNode[] children;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        foreach (var child in children)
        {
            if (child == null) continue;
            var result = child.Tick(bb);
            if (result != NodeState.Failure) return result;
        }
        return NodeState.Failure;
    }
}
