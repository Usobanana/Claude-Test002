using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ビヘイビアツリーのノード間で共有するランタイムデータ（ブラックボード）。
/// EnemyBrain が生成し、全ノードが参照する。
/// </summary>
public class EnemyBlackboard
{
    // ─── 参照 ───
    public Transform      self;
    public Transform      target;         // 攻撃対象（通常はPlayer）
    public NavMeshAgent   agent;
    public EnemyController enemy;        // 通常敵（nullable）
    public BossController  boss;         // ボス（nullable）

    // ─── 計算済みデータ ───
    public float distanceToTarget;

    // ─── ノードが書き込むタイマー・フラグ ───
    public float attackTimer;            // AttackNode が使用
    public float waitTimer;              // WaitNode が使用

    // ─── 徘徊（WanderNode）用 ───
    public Vector3 wanderDestination;    // 現在の徘徊目標地点
    public bool    hasWanderDestination; // 目標地点が設定済みか
    public float   wanderWaitTimer;      // 次の目標を選ぶまでの待機タイマー
}
