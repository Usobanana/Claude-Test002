using UnityEngine;

/// <summary>ビヘイビアツリーのノード実行結果</summary>
public enum NodeState
{
    Success,  // 成功
    Failure,  // 失敗
    Running,  // 実行中（次フレームも継続）
}

/// <summary>
/// ビヘイビアツリーの全ノード基底クラス（ScriptableObject）。
/// 新しい行動・条件を追加するには、このクラスを継承して Tick() を実装する。
/// </summary>
public abstract class BehaviorNode : ScriptableObject
{
    [TextArea(1, 2)]
    [Tooltip("このノードの説明（Inspector 表示用）")]
    public string description;

    /// <summary>毎フレーム呼ばれる。行動・条件を評価して NodeState を返す。</summary>
    public abstract NodeState Tick(EnemyBlackboard bb);
}
