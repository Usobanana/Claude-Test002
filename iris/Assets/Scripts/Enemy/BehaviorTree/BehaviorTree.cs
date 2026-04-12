using UnityEngine;

/// <summary>
/// ビヘイビアツリーのルートコンテナ（ScriptableObject）。
/// Inspector で rootNode に任意のノードをアサインしてツリーを組み立てる。
///
/// 作成方法: Project ウィンドウ右クリック → Game/Enemy/BehaviorTree
/// </summary>
[CreateAssetMenu(fileName = "BT_Enemy", menuName = "Game/Enemy/BehaviorTree")]
public class BehaviorTree : ScriptableObject
{
    [Tooltip("ツリーの起点となるルートノード")]
    public BehaviorNode rootNode;

    public NodeState Tick(EnemyBlackboard bb)
    {
        if (rootNode == null) return NodeState.Failure;
        return rootNode.Tick(bb);
    }
}
