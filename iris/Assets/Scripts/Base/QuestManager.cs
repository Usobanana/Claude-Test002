using UnityEngine;
using System;

/// <summary>
/// クエストの受理・進行・完了を管理するシングルトン
/// DontDestroyOnLoad でシーンをまたいで状態を保持する
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public QuestData  ActiveQuest   { get; private set; }
    public QuestState CurrentState  { get; private set; } = QuestState.None;

    // リザルト用の結果データ
    public int   EarnedExp  { get; private set; }
    public int   EarnedGold { get; private set; }
    public bool  IsSuccess  { get; private set; }

    public event Action<QuestData> OnQuestAccepted;
    public event Action<bool>      OnQuestCompleted;  // bool = success

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AcceptQuest(QuestData quest)
    {
        ActiveQuest  = quest;
        CurrentState = QuestState.InProgress;
        GameManager.Instance?.AcceptQuest(quest.questId);
        OnQuestAccepted?.Invoke(quest);
        Debug.Log($"[QuestManager] クエスト受理: {quest.questName}");
    }

    public void CompleteQuest(bool success)
    {
        CurrentState = success ? QuestState.Completed : QuestState.Failed;
        IsSuccess    = success;

        if (ActiveQuest != null)
        {
            EarnedExp  = success ? ActiveQuest.rewardExp  : 0;
            EarnedGold = success ? ActiveQuest.rewardGold : 0;
        }

        OnQuestCompleted?.Invoke(success);
        Debug.Log($"[QuestManager] クエスト{(success ? "完了" : "失敗")}: {(ActiveQuest != null ? ActiveQuest.questName : "（テスト）")}");

        // リザルトシーンへ
        GameManager.Instance?.GoToResult();
    }

    public void ResetQuest()
    {
        ActiveQuest  = null;
        CurrentState = QuestState.None;
        GameManager.Instance?.ClearQuest();
    }
}

public enum QuestState
{
    None,
    InProgress,
    Completed,
    Failed,
}
