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
    [SerializeField] private float moveSpeed              = 5f;
    [SerializeField] private float rotateSpeed            = 720f;  // 度/秒
    [Tooltip("攻撃・スキル・回避中の移動速度倍率（0=完全停止, 0.2=20%）")]
    [SerializeField] [Range(0f, 1f)] private float actingMoveMultiplier = 0.2f;

    [Header("ダッシュ")]
    [SerializeField] private float dashSpeed        = 9f;
    [SerializeField] private float dashStaminaCost  = 10f;  // /秒

    [Header("回避")]
    [SerializeField] private float dodgeDistance    = 4f;
    [SerializeField] private float dodgeDuration    = 0.2f;
    [SerializeField] private float dodgeStaminaCost = 25f;

    [Header("重力")]
    [SerializeField] private float gravity = -20f;

    // --- コンポーネント参照 ---
    private CharacterController cc;
    private CharacterEntity     entity;
    private PlayerAnimator      playerAnimator;

    // --- 状態 ---
    private Vector3 velocity;          // Y方向の重力蓄積
    private bool    isDodging;
    private float   dodgeTimer;
    private Vector3 dodgeDirection;
    private bool    isDashing;

    // --- 外部参照 ---
    public Vector3 FacingDirection { get; private set; } = Vector3.forward;
    public bool    IsDashing => isDashing;

    void Awake()
    {
        cc             = GetComponent<CharacterController>();
        entity         = GetComponent<CharacterEntity>();
        playerAnimator = GetComponent<PlayerAnimator>();
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
        HandleDash();
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

        // ダッシュ中は dashSpeed を使用（HandleDash で移動処理するため通常移動はスキップ）
        if (!isDashing)
        {
            float multiplier = (playerAnimator != null && playerAnimator.IsActing)
                ? actingMoveMultiplier
                : 1f;
            cc.Move(move * moveSpeed * multiplier * Time.deltaTime);
        }
    }

    // --- ダッシュ ---

    private void HandleDash()
    {
        if (InputHandler.Instance == null) return;

        bool dashInput = InputHandler.Instance.DashInput;
        isDashing = dashInput;

        if (!isDashing) return;

        Vector2 input = InputHandler.Instance.MoveInput;
        Vector3 move  = input.sqrMagnitude > 0.01f
            ? new Vector3(input.x, 0f, input.y).normalized
            : transform.forward;

        // スタミナ消費（秒あたり）。回復も止める。スタミナ切れでダッシュ停止
        entity.SuppressStaminaRegen();
        if (!entity.ConsumeStamina(dashStaminaCost * Time.deltaTime))
        {
            isDashing = false;
            return;
        }

        cc.Move(move * dashSpeed * Time.deltaTime);
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

    /// <summary>指定ワールド座標の方向へ即時回転（AutoAttackSystem 用）</summary>
    public void FaceToward(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();
        FacingDirection = dir;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
