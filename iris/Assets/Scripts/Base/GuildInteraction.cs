using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ギルドオブジェクトへのインタラクション。
/// 近づくとボタンが表示され、押すか E キーでクエストを受理する。
/// </summary>
public class GuildInteraction : MonoBehaviour
{
    [SerializeField] private QuestData[] availableQuests;
    [SerializeField] private float interactRange = 3f;

    private Transform playerTransform;
    private bool      isInRange;
    private bool      wasInRange;

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

        // 範囲に入ったらボタン表示、出たら非表示
        if (isInRange && !wasInRange)
            InteractionPromptUI.Instance?.Show("クエストを受ける", OpenGuild);
        else if (!isInRange && wasInRange)
            InteractionPromptUI.Instance?.Hide();

        wasInRange = isInRange;

        // E キー / コントローラー B ボタン
        if (isInRange)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                OpenGuild();
            else if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                OpenGuild();
        }
    }

    void OnDisable()
    {
        if (isInRange) InteractionPromptUI.Instance?.Hide();
    }

    private void OpenGuild()
    {
        if (availableQuests == null || availableQuests.Length == 0)
        {
            Debug.LogWarning("[Guild] クエストが登録されていません");
            return;
        }

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
