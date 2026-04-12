using UnityEngine;

/// <summary>
/// アクションノード：指定秒数待機する。待機中は Running、完了後 Success。
/// 作成: Project → Game/Enemy/BT/Action/Wait
/// </summary>
[CreateAssetMenu(fileName = "Act_Wait", menuName = "Game/Enemy/BT/Action/Wait")]
public class WaitNode : BehaviorNode
{
    [Tooltip("待機時間（秒）")]
    public float duration = 1f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        bb.waitTimer += Time.deltaTime;
        if (bb.waitTimer < duration) return NodeState.Running;

        bb.waitTimer = 0f;
        return NodeState.Success;
    }
}
