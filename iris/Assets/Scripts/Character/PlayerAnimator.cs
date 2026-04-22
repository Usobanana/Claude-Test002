using UnityEngine;

/// <summary>
/// プレイヤーのアニメーション制御・コンボ管理。
///
/// コンボは ComboData (ScriptableObject) で定義する。
/// AnimatorOverrideController でクリップをステップごとにスワップし、
/// normalizedTime を監視して入力ウィンドウを開閉する。
///
/// [Auto モード]  AutoAttackSystem のタイマーが来るたびにコンボが自動進行
/// [Input モード] 攻撃ボタンを押すたびにコンボが進む（ウィンドウ内のみ）
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

    [Header("コンボデータ")]
    [Tooltip("このキャラクターのコンボ設定。CharacterData.ApplyCharacter() で上書きされる")]
    [SerializeField] private ComboData comboData;

    [Tooltip("AnimatorController の ComboAttack ステートに使うプレースホルダークリップ。「Animator をセットアップ」ボタンで自動設定される")]
    [SerializeField] private AnimationClip comboPlaceholderClip;

    [Tooltip("WeaponArm レイヤーのデフォルトクリップ（OverrideController のキーとして使用）")]
    [SerializeField] private AnimationClip weaponArmPlaceholderClip;
    [Tooltip("WeaponHand レイヤーのデフォルトクリップ（OverrideController のキーとして使用）")]
    [SerializeField] private AnimationClip weaponHandPlaceholderClip;

    // ─────────────────────────────────────────
    // コンポーネント参照
    // ─────────────────────────────────────────

    private Animator         anim;
    private PlayerController controller;
    private CharacterEntity  entity;
    private AutoAttackSystem autoAttack;
    private HitDetector      hitDetector;

    private AnimatorOverrideController overrideController;

    // ─────────────────────────────────────────
    // コンボ状態
    // ─────────────────────────────────────────

    private int  currentStepIndex = -1;   // -1 = 非攻撃中
    private bool windowOpen       = false;
    private bool pendingAttack    = false;

    // ─────────────────────────────────────────
    // Animator パラメータハッシュ
    // ─────────────────────────────────────────

    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DodgeHash  = Animator.StringToHash("Dodge");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    // ─────────────────────────────────────────
    // Animator ステート識別（名前ではなくTag・名前を最小限に）
    // ─────────────────────────────────────────

    private const string LocomotionTag   = "Locomotion";
    private const string ComboAttackName = "ComboAttack";

    // ─────────────────────────────────────────
    // WeaponArm / WeaponHand レイヤー
    // ─────────────────────────────────────────

    private int  weaponArmLayerIdx  = -1;
    private int  weaponHandLayerIdx = -1;
    private bool weaponLayersActive = true;

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
    /// Locomotion（Idle/Run）以外のステートにいるとき true。
    /// Idleステート・Runステートには Animator で Tag "Locomotion" を付けること。
    /// </summary>
    public bool IsActing
    {
        get
        {
            if (anim == null) return false;
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (anim.IsInTransition(0))
            {
                var next = anim.GetNextAnimatorStateInfo(0);
                return !info.IsTag(LocomotionTag) || !next.IsTag(LocomotionTag);
            }
            return !info.IsTag(LocomotionTag);
        }
    }

    public bool IsAttacking => currentStepIndex >= 0;

    // ─────────────────────────────────────────
    // Unity ライフサイクル
    // ─────────────────────────────────────────

    void Awake()
    {
        controller  = GetComponent<PlayerController>();
        entity      = GetComponent<CharacterEntity>();
        autoAttack  = GetComponent<AutoAttackSystem>();
        hitDetector = GetComponent<HitDetector>();
        AcquireAnimator();
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

        UpdateSpeed();
        UpdateWeaponLayers();
        UpdateDodgeAnim();

        if (comboMode == ComboMode.Input && InputHandler.Instance != null)
        {
            if (InputHandler.Instance.AttackInput)
                TriggerComboAttack();
        }

        UpdateComboState();
    }

    // ─────────────────────────────────────────
    // 公開API
    // ─────────────────────────────────────────

    /// <summary>
    /// キャラクター切り替え時にコンボデータを更新する。
    /// PlayerAppearance.ApplyCharacter() から呼ぶ。
    /// </summary>
    public void SetComboData(ComboData data)
    {
        comboData = data;
        ResetCombo();
    }

    /// <summary>
    /// WeaponArm/WeaponHand レイヤーのポーズクリップをキャラクターに合わせて差し替える。
    /// PlayerAppearance.ApplyCharacter() から呼ぶ。null を渡したレイヤーは変更しない。
    /// </summary>
    public void SetWeaponPose(AnimationClip armClip, AnimationClip handClip)
    {
        if (overrideController == null) return;
        if (armClip  != null && weaponArmPlaceholderClip  != null) overrideController[weaponArmPlaceholderClip]  = armClip;
        if (handClip != null && weaponHandPlaceholderClip != null) overrideController[weaponHandPlaceholderClip] = handClip;
    }

    /// <summary>
    /// モデル差し替え後に Animator 参照を更新する。PlayerAppearance.SwapModel() から呼ぶ。
    /// </summary>
    public void RefreshAnimator(Animator newAnim = null)
    {
        SetupAnimator(newAnim != null ? newAnim : GetComponentInChildren<Animator>());
        ResetCombo();
    }

    /// <summary>Auto モード用：AutoAttackSystem のタイマーが来たときに呼ぶ</summary>
    public void TriggerAutoAttack()
    {
        if (comboMode != ComboMode.Auto) return;
        AdvanceCombo();
    }

    /// <summary>
    /// Input モード用。
    /// ウィンドウ内または1打目なら即進行、それ以外はバッファに積む。
    /// </summary>
    public void TriggerComboAttack()
    {
        if (comboMode != ComboMode.Input) return;
        if (currentStepIndex < 0 || windowOpen)
            AdvanceCombo();
        else
            pendingAttack = true;
    }

    // ─────────────────────────────────────────
    // スキルアニメーション
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
    // Animation Event 互換（クリップにイベントが設定されていれば使われる）
    // ─────────────────────────────────────────

    public void OnComboWindowOpen() => OpenComboWindow();
    public void OnComboEnd()        => ResetCombo();

    // ─────────────────────────────────────────
    // 内部ロジック
    // ─────────────────────────────────────────

    private void AcquireAnimator()
    {
        SetupAnimator(GetComponentInChildren<Animator>());
    }

    private void SetupAnimator(Animator target)
    {
        anim = target;
        if (anim == null)
        {
            overrideController = null;
            weaponArmLayerIdx  = -1;
            weaponHandLayerIdx = -1;
            return;
        }

        anim.applyRootMotion = false;
        weaponArmLayerIdx    = anim.GetLayerIndex("WeaponArm");
        weaponHandLayerIdx   = anim.GetLayerIndex("WeaponHand");

        // シーン保存等で runtimeAnimatorController が既に OverrideController になっている場合、
        // 元の AnimatorController まで遡ってから新しい OverrideController を作成する
        var baseController = GetBaseController(anim.runtimeAnimatorController);
        if (baseController == null)
        {
            Debug.LogWarning("[PlayerAnimator] AnimatorController が Animator に割り当てられていません");
            return;
        }
        overrideController = new AnimatorOverrideController(baseController);
        overrideController.hideFlags = HideFlags.DontSave; // シーンに保存させない
        anim.runtimeAnimatorController = overrideController;
        Debug.Log($"[PlayerAnimator] OverrideController 作成完了。ベース: {baseController.name}");
    }

    private static RuntimeAnimatorController GetBaseController(RuntimeAnimatorController rtc)
    {
        return rtc is AnimatorOverrideController ovc ? GetBaseController(ovc.runtimeAnimatorController) : rtc;
    }

    private void AdvanceCombo()
    {
        if (comboData == null || comboData.StepCount == 0)
        {
            Debug.LogWarning($"[PlayerAnimator] AdvanceCombo: comboData={comboData}, StepCount={comboData?.StepCount}");
            return;
        }

        int nextStep = currentStepIndex < 0 ? 0 : currentStepIndex + 1;

        // 最終段を超えたらリセット（最後の段が終わったあと入力があっても連続しない）
        if (nextStep >= comboData.StepCount)
        {
            ResetCombo();
            return;
        }

        PlayComboStep(nextStep);
    }

    private void PlayComboStep(int stepIndex)
    {
        if (overrideController == null || comboPlaceholderClip == null)
        {
            Debug.LogWarning("[PlayerAnimator] overrideController または comboPlaceholderClip が未設定です。Inspector の「Animator をセットアップ」を実行してください");
            return;
        }

        var step = comboData.steps[stepIndex];
        if (step?.clip == null)
        {
            Debug.LogWarning($"[PlayerAnimator] ComboStep[{stepIndex}] に clip が未設定です");
            return;
        }

        currentStepIndex = stepIndex;
        windowOpen       = false;
        pendingAttack    = false;

        overrideController[comboPlaceholderClip] = step.clip;
        anim.SetTrigger(AttackHash);
        hitDetector?.StartStep(step);

        if (comboMode == ComboMode.Input)
            autoAttack?.OnComboHit();
    }

    private void UpdateComboState()
    {
        if (currentStepIndex < 0 || comboData == null) return;

        var info = anim.GetCurrentAnimatorStateInfo(0);

        // ComboAttack ステートから離れた（アニメーション完了）
        if (!info.IsName(ComboAttackName))
        {
            ResetCombo();
            return;
        }

        float t    = info.normalizedTime % 1f;
        var   step = comboData.steps[currentStepIndex];

        hitDetector?.UpdateDetection(info.normalizedTime);

        bool shouldBeOpen = t >= step.WindowStartNormalized && t < step.WindowEndNormalized;

        if (shouldBeOpen && !windowOpen)
            OpenComboWindow();
        else if (!shouldBeOpen && windowOpen && t >= step.WindowEndNormalized)
            CloseComboWindow();
    }

    private void OpenComboWindow()
    {
        windowOpen = true;

        if (pendingAttack)
        {
            pendingAttack = false;
            AdvanceCombo();
        }
    }

    private void CloseComboWindow()
    {
        windowOpen = false;
    }

    private void ResetCombo()
    {
        currentStepIndex = -1;
        windowOpen       = false;
        pendingAttack    = false;
        hitDetector?.StopStep();
    }

    // ─────────────────────────────────────────
    // Animator 更新ヘルパー
    // ─────────────────────────────────────────

    private void UpdateSpeed()
    {
        Vector2 input   = InputHandler.Instance != null ? InputHandler.Instance.MoveInput : Vector2.zero;
        bool    dashing = controller.IsDashing;
        float   speed   = dashing ? Mathf.Max(input.magnitude, 1f) : input.magnitude;
        anim.SetFloat(SpeedHash, speed);
    }

    private void UpdateDodgeAnim()
    {
        bool isDodging = controller.IsDodging;
        if (isDodging && !wasDodging)
        {
            anim.SetTrigger(DodgeHash);
            AudioManager.Instance?.PlaySE(SFX.PlayerDodge);
        }
        wasDodging = isDodging;
    }

    /// <summary>
    /// WeaponArm/WeaponHand レイヤーは Locomotion 中のみ有効にする。
    /// 戦闘アニメーション中は全身アニメーションを優先するため無効にする。
    /// </summary>
    private void UpdateWeaponLayers()
    {
        if (weaponArmLayerIdx < 0 && weaponHandLayerIdx < 0) return;

        var  info         = anim.GetCurrentAnimatorStateInfo(0);
        bool inLocomotion = info.IsTag(LocomotionTag);

        if (anim.IsInTransition(0))
        {
            var next = anim.GetNextAnimatorStateInfo(0);
            inLocomotion = inLocomotion && next.IsTag(LocomotionTag);
        }

        if (inLocomotion == weaponLayersActive) return;
        weaponLayersActive = inLocomotion;

        if (weaponArmLayerIdx  >= 0) anim.SetLayerWeight(weaponArmLayerIdx,  inLocomotion ? 0.7f : 0f);
        if (weaponHandLayerIdx >= 0) anim.SetLayerWeight(weaponHandLayerIdx, inLocomotion ? 1.0f : 0f);
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
