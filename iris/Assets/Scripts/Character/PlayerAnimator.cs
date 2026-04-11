using UnityEngine;

/// <summary>
/// プレイヤーのアニメーション制御・コンボ管理
///
/// [Auto モード]  ターゲット検知後、attackInterval ごとにコンボが自動進行（1→2→3→1…）
/// [Input モード] 左クリック/右トリガーを押すたびにコンボが進む（コンボウィンドウ内のみ）
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(CharacterEntity))]
public class PlayerAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("コンボモード")]
    [SerializeField] private ComboMode comboMode = ComboMode.Input;

    [Header("コンボ設定")]
    [Tooltip("Input モード：コンボウィンドウ持続時間（秒）")]
    [SerializeField] private float comboWindowDuration = 1.0f;

    // ─────────────────────────────────────────
    // 内部状態
    // ─────────────────────────────────────────

    private Animator          anim;
    private PlayerController  controller;
    private CharacterEntity   entity;
    private AutoAttackSystem  autoAttack;

    // コンボ
    private int   comboIndex      = 0;   // 0=待機, 1=1打目, 2=2打目, 3=3打目
    private float comboWindowTimer = 0f;
    private bool  inComboWindow   = false;

    // アニメーターハッシュ
    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int AttackHash     = Animator.StringToHash("Attack");
    private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
    private static readonly int DodgeHash      = Animator.StringToHash("Dodge");
    private static readonly int IsDeadHash     = Animator.StringToHash("IsDead");

    private bool wasDodging;

    // ─────────────────────────────────────────
    // 公開プロパティ
    // ─────────────────────────────────────────

    public ComboMode Mode
    {
        get => comboMode;
        set => comboMode = value;
    }

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    void Awake()
    {
        anim       = GetComponentInChildren<Animator>();
        controller = GetComponent<PlayerController>();
        entity     = GetComponent<CharacterEntity>();
        autoAttack = GetComponent<AutoAttackSystem>();
    }

    void Start()
    {
        if (entity != null)
        {
            entity.OnDeath += OnDeath;
            entity.OnHurt  += OnHurt;
        }
    }

    void OnDestroy()
    {
        if (entity != null)
        {
            entity.OnDeath -= OnDeath;
            entity.OnHurt  -= OnHurt;
        }
    }

    void Update()
    {
        if (anim == null) return;

        // 移動速度
        Vector2 input = InputHandler.Instance != null ? InputHandler.Instance.MoveInput : Vector2.zero;
        anim.SetFloat(SpeedHash, input.magnitude);

        // 回避（開始時に1回だけ）
        bool isDodging = controller.IsDodging;
        if (isDodging && !wasDodging)
        {
            anim.SetTrigger(DodgeHash);
            AudioManager.Instance?.PlaySE(SFX.PlayerDodge);
        }
        wasDodging = isDodging;

        // Input モード：攻撃入力でコンボ進行
        if (comboMode == ComboMode.Input && InputHandler.Instance != null)
        {
            if (InputHandler.Instance.AttackInput)
                TriggerComboAttack();
        }

        // コンボウィンドウのタイムアウト管理
        UpdateComboWindow();
    }

    // ─────────────────────────────────────────
    // コンボ制御（外部から呼ばれる）
    // ─────────────────────────────────────────

    /// <summary>
    /// Auto モード用：AutoAttackSystem のタイマーが来たときに呼ぶ
    /// </summary>
    public void TriggerAutoAttack()
    {
        if (comboMode != ComboMode.Auto) return;
        AdvanceCombo();
    }

    /// <summary>
    /// Input モード用（Update 内 / 外部から呼んでもよい）
    /// コンボウィンドウ内、または1打目なら進行
    /// </summary>
    public void TriggerComboAttack()
    {
        if (comboMode != ComboMode.Input) return;

        // 1打目は常に受け付ける。2打目以降はウィンドウ内のみ
        if (comboIndex == 0 || inComboWindow)
            AdvanceCombo();
    }

    // ─────────────────────────────────────────
    // スキルアニメーション（外部から呼ぶ）
    // ─────────────────────────────────────────

    private static readonly int Skill1Hash = Animator.StringToHash("Skill1");
    private static readonly int Skill2Hash = Animator.StringToHash("Skill2");
    private static readonly int Skill3Hash = Animator.StringToHash("Skill3");
    private static readonly int UltHash    = Animator.StringToHash("Ult");

    public void TriggerSkill(int slot)
    {
        ResetCombo();
        int hash = slot switch
        {
            1 => Skill1Hash,
            2 => Skill2Hash,
            3 => Skill3Hash,
            4 => UltHash,
            _ => -1
        };
        if (hash != -1 && anim != null)
            anim.SetTrigger(hash);
    }

    // ─────────────────────────────────────────
    // 内部ロジック
    // ─────────────────────────────────────────

    private void AdvanceCombo()
    {
        comboIndex = comboIndex >= 3 ? 1 : comboIndex + 1;
        anim.SetInteger(ComboIndexHash, comboIndex);
        anim.SetTrigger(AttackHash);
        AudioManager.Instance?.PlaySE(SFX.PlayerAttack);

        // コンボウィンドウを即座に開く（アニメーションイベント不要）
        inComboWindow   = true;
        comboWindowTimer = comboWindowDuration;

        // Input モード時はダメージを AutoAttackSystem に委譲
        if (comboMode == ComboMode.Input)
            autoAttack?.OnComboHit();
    }

    private void UpdateComboWindow()
    {
        if (!inComboWindow) return;
        // Auto モードはタイマーによるリセット不要（attackInterval がタイミングを制御）
        if (comboMode == ComboMode.Auto) return;

        comboWindowTimer -= Time.deltaTime;
        if (comboWindowTimer <= 0f)
        {
            ResetCombo();
        }
    }

    /// <summary>
    /// アニメーションイベントから呼ぶ：コンボウィンドウを開く
    /// </summary>
    public void OnComboWindowOpen()
    {
        inComboWindow   = true;
        comboWindowTimer = comboWindowDuration;
    }

    /// <summary>
    /// アニメーションイベントから呼ぶ：コンボウィンドウを閉じてリセット
    /// </summary>
    public void OnComboEnd()
    {
        ResetCombo();
    }

    private void ResetCombo()
    {
        comboIndex       = 0;
        inComboWindow    = false;
        comboWindowTimer  = 0f;
        if (anim != null)
            anim.SetInteger(ComboIndexHash, 0);
    }

    // ─────────────────────────────────────────
    // イベントハンドラー
    // ─────────────────────────────────────────

    private void OnHurt()
    {
        AudioManager.Instance?.PlaySE(SFX.PlayerHurt);
        EffectManager.Instance?.PlayHitEffect(transform.position);
        ResetCombo();
    }

    private void OnDeath()
    {
        if (anim != null) anim.SetBool(IsDeadHash, true);
        AudioManager.Instance?.PlaySE(SFX.PlayerDeath);
        EffectManager.Instance?.PlayDeathEffect(transform.position);
        ResetCombo();
    }
}

/// <summary>コンボの進行方式</summary>
public enum ComboMode
{
    Auto,   // タイマーで自動進行
    Input,  // 入力のたびに進行
}
