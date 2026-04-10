using UnityEngine;

/// <summary>
/// フィールド上でのクエスト進行を監視する
/// ボス死亡 → クエスト完了 → リザルトシーンへ
/// </summary>
public class QuestProgress : MonoBehaviour
{
    [SerializeField] private GameObject bossSpawnPoint;

    private BossController boss;
    private bool           questFinished;

    void Start()
    {
        // シーン内のボスを探して監視開始
        boss = FindAnyObjectByType<BossController>();
        if (boss != null)
        {
            boss.OnBossDeath += HandleBossDeath;
            Debug.Log("[QuestProgress] ボス監視開始");
        }
        else
        {
            Debug.LogWarning("[QuestProgress] ボスが見つかりません");
        }
    }

    void OnDestroy()
    {
        if (boss != null)
            boss.OnBossDeath -= HandleBossDeath;
    }

    private void HandleBossDeath()
    {
        if (questFinished) return;
        questFinished = true;

        Debug.Log("[QuestProgress] ボス討伐！クエスト完了");

        // 少し待ってからリザルトへ（演出用）
        Invoke(nameof(CompleteQuest), 2f);
    }

    private void CompleteQuest()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.CompleteQuest(success: true);
        else
            GameManager.Instance?.GoToResult();
    }
}
