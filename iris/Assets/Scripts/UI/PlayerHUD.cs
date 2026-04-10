using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// フィールド中のプレイヤーHUD
/// HP/スタミナバー + スキルクールダウン表示
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

    private CharacterEntity entity;
    private SkillSystem     skillSystem;

    void Start()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        entity      = player.GetComponent<CharacterEntity>();
        skillSystem = player.GetComponent<SkillSystem>();

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
}
