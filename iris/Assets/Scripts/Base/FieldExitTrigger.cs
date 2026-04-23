using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 拠点の出口トリガー。
/// 近づくとボタンが表示され、押すか E キーでフィールドへ遷移する。
/// </summary>
public class FieldExitTrigger : MonoBehaviour
{
    [SerializeField] private float triggerRadius = 2f;

    private Transform playerTransform;
    private bool      playerInZone;
    private bool      wasInZone;

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        playerInZone = dist <= triggerRadius;

        // 範囲に入ったらボタン表示、出たら非表示
        if (playerInZone && !wasInZone)
            InteractionPromptUI.Instance?.Show("クエストへ向かう", TryDepart);
        else if (!playerInZone && wasInZone)
            InteractionPromptUI.Instance?.Hide();

        wasInZone = playerInZone;

        // E キー / コントローラー B ボタン
        if (playerInZone)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryDepart();
            else if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                TryDepart();
        }
    }

    void OnDisable()
    {
        if (playerInZone) InteractionPromptUI.Instance?.Hide();
    }

    private void TryDepart()
    {
        if (QuestManager.Instance == null ||
            QuestManager.Instance.CurrentState != QuestState.InProgress)
        {
            Debug.Log("[FieldExit] クエストを受理してください（ギルドへ）");
            return;
        }

        Debug.Log("[FieldExit] フィールドへ出発！");
        GameManager.Instance?.GoToField();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
