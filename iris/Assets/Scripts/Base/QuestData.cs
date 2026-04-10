using UnityEngine;

/// <summary>
/// クエストの定義データ（ScriptableObject）
/// ギルドに登録するクエストごとに1アセット作成する
/// </summary>
[CreateAssetMenu(fileName = "QuestData", menuName = "Game/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("基本情報")]
    public int    questId;
    public string questName;
    [TextArea] public string description;

    [Header("報酬")]
    public int rewardExp;
    public int rewardGold;

    [Header("フィールド設定")]
    public string fieldSceneName = "FieldScene";  // 将来複数フィールド対応のため
}
