using UnityEngine;

/// <summary>
/// Sequence（AND）ノード。
/// 子を順番に実行し、最初に Failure / Running を返した子でそのまま終了。
/// 全子が Success を返した場合のみ Success。
///
/// 使い方: 条件 → アクションの順に並べる（例: 射程内？→ 攻撃）
/// 作成: Project → Game/Enemy/BT/Sequence
/// </summary>
[CreateAssetMenu(fileName = "Sequence", menuName = "Game/Enemy/BT/Sequence")]
public class SequenceNode : BehaviorNode
{
    public BehaviorNode[] children;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        foreach (var child in children)
        {
            if (child == null) continue;
            var result = child.Tick(bb);
            if (result != NodeState.Success) return result;
        }
        return NodeState.Success;
    }
}
