using UnityEngine;

/// <summary>
/// プレイヤーの移動・回避・向き制御を担当するコンポーネント
/// トップダウンビュー用。CharacterControllerを使用。
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterEntity))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float moveSpeed   = 5f;
    [SerializeField] private float rotateSpeed = 720f;  // 度/秒

    [Header("回避")]
    [SerializeField] private float dodgeDistance     = 4f;
    [SerializeField] private float dodgeDuration      = 0.2f;
    [SerializeField] private float dodgeStaminaCost   = 25f;

    [Header("重力")]
    [SerializeField] private float gravity = -20f;

    // --- コンポーネント参照 ---
    private CharacterController cc;
    private CharacterEntity     entity;

    // --- 状態 ---
    private Vector3 velocity;          // Y方向の重力蓄積
    private bool    isDodging;
    private float   dodgeTimer;
    private Vector3 dodgeDirection;

    // --- 外部参照 ---
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;

    void Awake()
    {
        cc     = GetComponent<CharacterController>();
        entity = GetComponent<CharacterEntity>();
    }

    void Update()
    {
        // 戦闘中のみ死亡チェック（拠点など戦闘外シーンでも動けるようにする）
        bool combatDead = entity.Data != null && !entity.IsAlive;
        if (combatDead) return;

        if (isDodging)
        {
            UpdateDodge();
            return;
        }

        HandleMovement();
        HandleDodge();
        ApplyGravity();
    }

    // --- 移動 ---

    private void HandleMovement()
    {
        if (InputHandler.Instance == null) return;

        Vector2 input = InputHandler.Instance.MoveInput;
        Vector3 move  = new Vector3(input.x, 0f, input.y);

        if (move.sqrMagnitude > 0.01f)
        {
            move.Normalize();
            FacingDirection = move;
            RotateTowards(move);
        }

        cc.Move(move * moveSpeed * Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRot, rotateSpeed * Time.deltaTime
        );
    }

    // --- 回避 ---

    private void HandleDodge()
    {
        if (!InputHandler.Instance.DodgeInput) return;
        if (!entity.ConsumeStamina(dodgeStaminaCost)) return;

        Vector2 input = InputHandler.Instance.MoveInput;
        dodgeDirection = input.sqrMagnitude > 0.01f
            ? new Vector3(input.x, 0f, input.y).normalized
            : -transform.forward;   // 入力がなければ後方へ

        isDodging  = true;
        dodgeTimer = 0f;
    }

    private void UpdateDodge()
    {
        dodgeTimer += Time.deltaTime;
        float t = dodgeTimer / dodgeDuration;

        // 速度は時間とともに減衰
        float speed = Mathf.Lerp(dodgeDistance / dodgeDuration, 0f, t);
        cc.Move(dodgeDirection * speed * Time.deltaTime);

        if (dodgeTimer >= dodgeDuration)
        {
            isDodging = false;
        }
    }

    // --- 重力 ---

    private void ApplyGravity()
    {
        if (cc.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    // --- 公開プロパティ ---

    public bool IsDodging => isDodging;
}
