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
    [SerializeField] private float comboWindowDuration = 0.8f;
    [Tooltip("攻撃後 OnComboWindowOpen が来ない場合のリセット上限（秒）")]
    [SerializeField] private float comboResetTimeout   = 2.0f;

    // ─────────────────────────────────────────
    // 内部状態
    // ─────────────────────────────────────────

    private Animator          anim;
    private PlayerController  controller;
    private CharacterEntity   entity;
    private AutoAttackSystem  autoAttack;

    // コンボ
    private int   comboIndex        = 0;    // 0=待機, 1=1打目, 2=2打目, 3=3打目
    private float comboWindowTimer  = 0f;
    private float comboResetTimer   = 0f;   // Animation Event が来ない場合のフォールバック
    private bool  inComboWindow     = false;
    private bool  pendingAttack     = false; // ウィンドウ外入力バッファ

    // アニメーターハッシュ
    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int AttackHash     = Animator.StringToHash("Attack");
    private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
    private static readonly int DodgeHash      = Animator.StringToHash("Dodge");
    private static readonly int IsDeadHash     = Animator.StringToHash("IsDead");

    // ベースレイヤーのステートハッシュ（ロコモーション判定用）
    private static readonly int StateIdle       = Animator.StringToHash("Idle");
    private static readonly int StateRun        = Animator.StringToHash("Run");
    private static readonly int StateAttack1RTI = Animator.StringToHash("Attack1_RTI");
    private static readonly int StateAttack2RTI = Animator.StringToHash("Attack2_RTI");
    private static readonly int StateAttack3RTI = Animator.StringToHash("Attack3_RTI");

    // SwordArm / SwordHand レイヤーインデックス（-1 = 存在しない）
    private int  swordArmLayerIdx  = -1;
    private int  swordHandLayerIdx = -1;
    private bool swordLayersActive = true;

    private bool wasDodging;

    // ─────────────────────────────────────────
    // 公開プロパティ
    // ─────────────────────────────────────────

    public ComboMode Mode
    {
        get => comboMode;
        set => comboMode = value;
    }

    /// <summary>
    /// 攻撃・スキル・回避・被弾・死亡アニメーション中は true。
    /// PlayerController が移動速度を減衰させるために参照する。
    /// </summary>
    public bool IsActing
    {
        get
        {
            if (anim == null) return false;
            var info = anim.GetCurrentAnimatorStateInfo(0);
            bool currentIsLocomotion = info.shortNameHash == StateIdle
                                    || info.shortNameHash == StateRun;
            // トランジション中：遷移先が非ロコモーションなら即 Acting 扱い
            if (anim.IsInTransition(0))
            {
                var next = anim.GetNextAnimatorStateInfo(0);
                bool nextIsLocomotion = next.shortNameHash == StateIdle
                                     || next.shortNameHash == StateRun;
                return !currentIsLocomotion || !nextIsLocomotion;
            }
            return !currentIsLocomotion;
        }
    }

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        entity     = GetComponent<CharacterEntity>();
        autoAttack = GetComponent<AutoAttackSystem>();
        AcquireAnimator();
    }

    /// <summary>
    /// モデル差し替え後にAnimator参照を更新する。PlayerAppearance.SwapModel()から呼ぶ。
    /// newAnim を渡すと確実にそのAnimatorを使う（タイミング問題を回避）。
    /// </summary>
    public void RefreshAnimator(Animator newAnim = null)
    {
        if (newAnim != null)
            anim = newAnim;
        else
            anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            swordArmLayerIdx  = anim.GetLayerIndex("SwordArm");
            swordHandLayerIdx = anim.GetLayerIndex("SwordHand");
        }
        else
        {
            swordArmLayerIdx  = -1;
            swordHandLayerIdx = -1;
        }
        ResetCombo();
    }

    private void AcquireAnimator()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            swordArmLayerIdx  = anim.GetLayerIndex("SwordArm");
            swordHandLayerIdx = anim.GetLayerIndex("SwordHand");
        }
        else
        {
            swordArmLayerIdx  = -1;
            swordHandLayerIdx = -1;
        }
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

        // 移動速度（ダッシュ中は Speed を 2 にして Run アニメーションを維持）
        Vector2 input    = InputHandler.Instance != null ? InputHandler.Instance.MoveInput : Vector2.zero;
        bool    dashing  = controller.IsDashing;
        float   speed    = dashing ? Mathf.Max(input.magnitude, 1f) : input.magnitude;
        anim.SetFloat(SpeedHash, speed);

        // SwordArm/SwordHand レイヤー: ロコモーション中のみ有効、戦闘中は無効
        UpdateSwordLayers();

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
    /// コンボウィンドウ内または1打目なら即進行、それ以外はバッファに積む。
    /// バッファは OnComboWindowOpen() で自動消費される。
    /// </summary>
    public void TriggerComboAttack()
    {
        if (comboMode != ComboMode.Input) return;

        if (comboIndex == 0 || inComboWindow)
            AdvanceCombo();
        else
            pendingAttack = true; // ウィンドウが開くまで保持
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

        // ウィンドウは Animation Event (OnComboWindowOpen) が来るまで閉じたまま
        inComboWindow    = false;
        comboWindowTimer = 0f;
        comboResetTimer  = comboResetTimeout; // フォールバック用タイムアウト開始
        pendingAttack    = false;

        // Input モード時はダメージを AutoAttackSystem に委譲
        if (comboMode == ComboMode.Input)
            autoAttack?.OnComboHit();
    }

    private void UpdateComboWindow()
    {
        if (comboMode == ComboMode.Auto) return;

        // ── フェーズ1: RTI ステート検出でウィンドウを開く（Animation Event の取りこぼし対策）──
        if (!inComboWindow && comboIndex > 0)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            bool inRTI = info.shortNameHash == StateAttack1RTI
                      || info.shortNameHash == StateAttack2RTI
                      || info.shortNameHash == StateAttack3RTI;
            if (inRTI)
            {
                OpenComboWindow();
                return;
            }
        }

        // ── フェーズ2: フォールバックリセット（RTI にも入らなかった場合）──
        if (!inComboWindow && comboResetTimer > 0f)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f)
                ResetCombo();
            return;
        }

        // ── フェーズ3: ウィンドウのタイムアウト ──
        if (!inComboWindow) return;
        comboWindowTimer -= Time.deltaTime;
        if (comboWindowTimer <= 0f)
            ResetCombo();
    }

    /// <summary>
    /// コンボウィンドウを開き、pendingAttack があれば即消費する。
    /// RTI ステート検出・Animation Event の両方から呼ばれる。
    /// </summary>
    private void OpenComboWindow()
    {
        inComboWindow    = true;
        comboWindowTimer = comboWindowDuration;
        comboResetTimer  = 0f;

        if (pendingAttack)
        {
            pendingAttack = false;
            AdvanceCombo();
        }
    }

    /// <summary>
    /// アニメーションイベントから呼ぶ：コンボウィンドウを開く。
    /// ウィンドウ外で押されていた入力（pendingAttack）があれば即消費して次コンボへ進む。
    /// </summary>
    public void OnComboWindowOpen()
    {
        OpenComboWindow();
    }

    /// <summary>
    /// アニメーションイベントから呼ぶ：コンボウィンドウを閉じてリセット
    /// </summary>
    public void OnComboEnd()
    {
        pendingAttack = false;
        ResetCombo();
    }

    /// <summary>
    /// ベースレイヤーが Idle または Run の場合のみ SwordArm/SwordHand を有効にする。
    /// 戦闘アニメーション（Attack/Dodge/Hit/Death 等）は全身アニメーションを使うため無効にする。
    /// トランジション中は current/next 両方がロコモーションの場合のみ有効にする。
    /// （Idle→Attack のブレンド中に Idle_Sword が混入するのを防ぐ）
    /// </summary>
    private void UpdateSwordLayers()
    {
        if (swordArmLayerIdx < 0 && swordHandLayerIdx < 0) return;

        var  info                = anim.GetCurrentAnimatorStateInfo(0);
        bool currentIsLocomotion = info.shortNameHash == StateIdle || info.shortNameHash == StateRun;

        bool inLocomtion = currentIsLocomotion;

        // トランジション中は next ステートも確認：どちらかが非ロコモーションなら即無効
        if (anim.IsInTransition(0))
        {
            var  nextInfo          = anim.GetNextAnimatorStateInfo(0);
            bool nextIsLocomotion  = nextInfo.shortNameHash == StateIdle || nextInfo.shortNameHash == StateRun;
            inLocomtion = currentIsLocomotion && nextIsLocomotion;
        }

        if (inLocomtion == swordLayersActive) return; // 変化なし
        swordLayersActive = inLocomtion;

        if (swordArmLayerIdx  >= 0) anim.SetLayerWeight(swordArmLayerIdx,  inLocomtion ? 0.7f : 0f);
        if (swordHandLayerIdx >= 0) anim.SetLayerWeight(swordHandLayerIdx, inLocomtion ? 1.0f : 0f);
    }

    private void ResetCombo()
    {
        comboIndex       = 0;
        inComboWindow    = false;
        comboWindowTimer = 0f;
        comboResetTimer  = 0f;
        pendingAttack    = false;
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
