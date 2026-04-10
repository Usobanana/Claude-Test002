using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 拠点の出口トリガー
/// クエスト受理済みのプレイヤーが入ったらフィールドへ遷移する
/// </summary>
public class FieldExitTrigger : MonoBehaviour
{
    [SerializeField] private float triggerRadius = 2f;

    private Transform playerTransform;
    private bool      playerInZone;

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

        if (playerInZone && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryDepart();
        }
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
