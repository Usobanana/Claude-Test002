using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ギルドオブジェクトへのインタラクション
/// 近づいてEキーを押すとクエスト受理UI（暫定）が出る
/// </summary>
public class GuildInteraction : MonoBehaviour
{
    [SerializeField] private QuestData[] availableQuests;
    [SerializeField] private float interactRange = 3f;

    private Transform playerTransform;
    private bool      isInRange;

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isInRange = dist <= interactRange;

        if (isInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenGuild();
        }
    }

    private void OpenGuild()
    {
        if (availableQuests == null || availableQuests.Length == 0)
        {
            Debug.LogWarning("[Guild] クエストが登録されていません");
            return;
        }

        // 暫定：最初のクエストを自動受理（Phase6でUI実装後に選択式にする）
        var quest = availableQuests[0];
        QuestManager.Instance?.AcceptQuest(quest);
        Debug.Log($"[Guild] クエスト受理: {quest.questName} → 出口からフィールドへ");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
