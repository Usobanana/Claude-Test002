using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputSystemのアクションをゲーム全体で使いやすくラップするクラス
/// PlayerControllerなどから参照する
/// </summary>
public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    // --- 入力値（毎フレーム更新）---
    public Vector2 MoveInput   { get; private set; }
    public bool    AttackInput { get; private set; }  // 通常攻撃（押した瞬間）
    public bool    DodgeInput  { get; private set; }  // 回避：Shift 短押し（離した瞬間）
    public bool    DashInput   { get; private set; }  // ダッシュ：Shift 長押し中
    public bool    Skill1Input { get; private set; }
    public bool    Skill2Input { get; private set; }
    public bool    Skill3Input { get; private set; }
    public bool    UltInput    { get; private set; }
    public bool    ChainInput  { get; private set; }

    // Shift 短押し／長押し判定
    [Tooltip("この秒数以上押し続けるとダッシュ判定になる")]
    [SerializeField] private float dashHoldThreshold = 0.2f;
    private float shiftHoldTime = 0f;

    private PlayerInput playerInput;

    // Action references
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dodgeAction;
    private InputAction skill1Action;
    private InputAction skill2Action;
    private InputAction skill3Action;
    private InputAction ultAction;
    private InputAction chainAction;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            playerInput = FindAnyObjectByType<PlayerInput>();

        BindActions();
    }

    void BindActions()
    {
        if (playerInput == null)
        {
            Debug.LogError("[InputHandler] PlayerInput が見つかりません。");
            return;
        }
        var map = playerInput.actions.FindActionMap("Player", throwIfNotFound: true);

        moveAction   = map.FindAction("Move",       throwIfNotFound: true);
        attackAction = map.FindAction("Attack",     throwIfNotFound: true);
        dodgeAction  = map.FindAction("Dodge",      throwIfNotFound: true);
        skill1Action = map.FindAction("Skill1",     throwIfNotFound: true);
        skill2Action = map.FindAction("Skill2",     throwIfNotFound: true);
        skill3Action = map.FindAction("Skill3",     throwIfNotFound: true);
        ultAction    = map.FindAction("UltSkill",   throwIfNotFound: true);
        chainAction  = map.FindAction("ChainSkill", throwIfNotFound: true);
    }

    void Update()
    {
        MoveInput   = moveAction.ReadValue<Vector2>();
        AttackInput = attackAction.WasPressedThisFrame();
        Skill1Input = skill1Action.WasPressedThisFrame();
        Skill2Input = skill2Action.WasPressedThisFrame();
        Skill3Input = skill3Action.WasPressedThisFrame();
        UltInput    = ultAction.WasPressedThisFrame();
        ChainInput  = chainAction.WasPressedThisFrame();

        // Shift 短押し（回避）/ 長押し（ダッシュ）判定
        bool shiftIsPressed       = dodgeAction.IsPressed();
        bool shiftPressedThisFrame  = dodgeAction.WasPressedThisFrame();
        bool shiftReleasedThisFrame = dodgeAction.WasReleasedThisFrame();

        if (shiftPressedThisFrame)  shiftHoldTime  = 0f;
        if (shiftIsPressed)         shiftHoldTime += Time.deltaTime;

        // 長押し：閾値以上押し続けている間は true
        DashInput  = shiftIsPressed && shiftHoldTime >= dashHoldThreshold;
        // 短押し：閾値未満で離した瞬間に 1 フレームだけ true
        DodgeInput = shiftReleasedThisFrame && shiftHoldTime < dashHoldThreshold;

        if (shiftReleasedThisFrame) shiftHoldTime = 0f;
    }

    void OnEnable()  => playerInput?.actions.FindActionMap("Player")?.Enable();
    void OnDisable() => playerInput?.actions.FindActionMap("Player")?.Disable();
}
