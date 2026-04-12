using UnityEngine;

/// <summary>
/// アクションノード：攻撃インターバルを管理しながら攻撃する。
/// EnemyController / BossController の DoAttack() を呼ぶ。
/// 作成: Project → Game/Enemy/BT/Action/Attack
/// </summary>
[CreateAssetMenu(fileName = "Act_Attack", menuName = "Game/Enemy/BT/Action/Attack")]
public class AttackNode : BehaviorNode
{
    [Tooltip("攻撃間隔（秒）。0 にするとインターバルなし。")]
    public float attackInterval = 2f;

    public override NodeState Tick(EnemyBlackboard bb)
    {
        bb.attackTimer += Time.deltaTime;
        if (bb.attackTimer < attackInterval) return NodeState.Running;

        bb.attackTimer = 0f;

        if      (bb.enemy != null) bb.enemy.DoAttack();
        else if (bb.boss  != null) bb.boss.DoNormalAttack();

        return NodeState.Success;
    }
}
