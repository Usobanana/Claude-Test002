using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// フィールド中のプレイヤーHUD
/// HP/スタミナバー + スキルクールダウン表示 + AUTO ON/OFF ボタン
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("HP/スタミナ")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("スキルボタン（クールダウンOverlay）")]
    [SerializeField] private Image skill1Overlay;
    [SerializeField] private Image skill2Overlay;
    [SerializeField] private Image skill3Overlay;
    [SerializeField] private Image ultOverlay;
    [SerializeField] private Image chainOverlay;

    [Header("AUTO 切り替えボタン（右上）")]
    [SerializeField] private Button          autoToggleButton;
    [SerializeField] private TextMeshProUGUI autoToggleText;

    private CharacterEntity  entity;
    private SkillSystem      skillSystem;
    private AutoAttackSystem autoAttackSystem;

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        entity           = player.GetComponent<CharacterEntity>();
        skillSystem      = player.GetComponent<SkillSystem>();
        autoAttackSystem = player.GetComponent<AutoAttackSystem>();

        // シリアライズ参照が外れた場合は動的検索
        if (autoToggleButton == null)
        {
            var t = transform.Find("AutoToggleButton");
            if (t != null) autoToggleButton = t.GetComponent<Button>();
        }
        if (autoToggleText == null && autoToggleButton != null)
            autoToggleText = autoToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        // AUTO ボタン初期化
        if (autoToggleButton != null)
            autoToggleButton.onClick.AddListener(ToggleAutoMode);
        UpdateAutoButtonText();

        if (entity != null)
        {
            entity.OnHpChanged      += UpdateHp;
            entity.OnStaminaChanged += UpdateStamina;

            // 初期値セット
            if (entity.Data != null)
            {
                UpdateHp(entity.CurrentHp, entity.Data.baseStats.maxHp);
                UpdateStamina(entity.CurrentStamina, entity.Data.baseStats.maxStamina);
            }
        }
    }

    void OnDestroy()
    {
        if (entity != null)
        {
            entity.OnHpChanged      -= UpdateHp;
            entity.OnStaminaChanged -= UpdateStamina;
        }
    }

    void Update()
    {
        UpdateSkillCooldowns();
    }

    private void UpdateHp(float current, float max)
    {
        if (hpSlider != null) hpSlider.value = max > 0f ? current / max : 0f;
        if (hpText   != null) hpText.text    = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void UpdateStamina(float current, float max)
    {
        if (staminaSlider != null) staminaSlider.value = max > 0f ? current / max : 0f;
    }

    private void UpdateSkillCooldowns()
    {
        if (skillSystem == null) return;

        SetOverlay(skill1Overlay, skillSystem.GetSkillProgress(1));
        SetOverlay(skill2Overlay, skillSystem.GetSkillProgress(2));
        SetOverlay(skill3Overlay, skillSystem.GetSkillProgress(3));
        SetOverlay(ultOverlay,   skillSystem.GetSkillProgress(4));
        SetOverlay(chainOverlay, skillSystem.GetSkillProgress(5));
    }

    // progress 0=クールダウン中(全覆い) / 1=使用可能(覆いなし)
    private void SetOverlay(Image overlay, float progress)
    {
        if (overlay == null) return;
        overlay.fillAmount = 1f - progress;
    }

    // ─────────────────────────────────────────
    // AUTO ON/OFF
    // ─────────────────────────────────────────

    private void ToggleAutoMode()
    {
        if (autoAttackSystem == null) return;
        autoAttackSystem.SetAutoMode(!autoAttackSystem.IsAutoMode);
        UpdateAutoButtonText();
    }

    private void UpdateAutoButtonText()
    {
        if (autoToggleText == null) return;
        bool isAuto = autoAttackSystem != null && autoAttackSystem.IsAutoMode;
        autoToggleText.text = isAuto ? "AUTO ON" : "AUTO OFF";
    }
}
