using UnityEngine;
using System;

/// <summary>
/// スキルスロット管理システム
/// アクティブ×3、ウルト×1、連携×1のクールタイムを管理する
/// </summary>
public class SkillSystem : MonoBehaviour
{
    // --- クールタイム設定（暫定値、後でCharacterDataに移す）---
    [Header("クールタイム（秒）")]
    [SerializeField] private float skill1Cooldown = 8f;
    [SerializeField] private float skill2Cooldown = 10f;
    [SerializeField] private float skill3Cooldown = 12f;
    [SerializeField] private float ultCooldown    = 30f;
    [SerializeField] private float chainCooldown  = 15f;

    // --- 残りクールタイム ---
    public float Skill1Remaining { get; private set; }
    public float Skill2Remaining { get; private set; }
    public float Skill3Remaining { get; private set; }
    public float UltRemaining    { get; private set; }
    public float ChainRemaining  { get; private set; }

    // --- 使用可能か ---
    public bool CanUseSkill1 => Skill1Remaining <= 0f;
    public bool CanUseSkill2 => Skill2Remaining <= 0f;
    public bool CanUseSkill3 => Skill3Remaining <= 0f;
    public bool CanUseUlt    => UltRemaining    <= 0f;
    public bool CanUseChain  => ChainRemaining  <= 0f && chainUnlocked;

    // 連携スキル解放フラグ（発動条件が満たされたときONになる）
    private bool chainUnlocked;

    // イベント（UIへの通知用）
    public event Action<int, float> OnSkillUsed;  // (スロット番号, クールタイム)
    public event Action             OnChainUnlocked;

    private CharacterEntity entity;

    void Awake()
    {
        entity = GetComponent<CharacterEntity>();
    }

    void Update()
    {
        TickCooldowns();
        HandleInput();
    }

    private void TickCooldowns()
    {
        float dt = Time.deltaTime;
        Skill1Remaining = Mathf.Max(0f, Skill1Remaining - dt);
        Skill2Remaining = Mathf.Max(0f, Skill2Remaining - dt);
        Skill3Remaining = Mathf.Max(0f, Skill3Remaining - dt);
        UltRemaining    = Mathf.Max(0f, UltRemaining    - dt);
        ChainRemaining  = Mathf.Max(0f, ChainRemaining  - dt);
    }

    private void HandleInput()
    {
        if (InputHandler.Instance == null) return;

        if (InputHandler.Instance.Skill1Input && CanUseSkill1) UseSkill(1);
        if (InputHandler.Instance.Skill2Input && CanUseSkill2) UseSkill(2);
        if (InputHandler.Instance.Skill3Input && CanUseSkill3) UseSkill(3);
        if (InputHandler.Instance.UltInput    && CanUseUlt)    UseUlt();
        if (InputHandler.Instance.ChainInput  && CanUseChain)  UseChain();
    }

    private void UseSkill(int slot)
    {
        switch (slot)
        {
            case 1: Skill1Remaining = skill1Cooldown; break;
            case 2: Skill2Remaining = skill2Cooldown; break;
            case 3: Skill3Remaining = skill3Cooldown; break;
        }
        OnSkillUsed?.Invoke(slot, GetCooldown(slot));
        Debug.Log($"[SkillSystem] Skill{slot} 使用");
        // TODO: 各スキルのエフェクト・ダメージ処理を実装
    }

    private void UseUlt()
    {
        UltRemaining = ultCooldown;
        OnSkillUsed?.Invoke(4, ultCooldown);
        Debug.Log("[SkillSystem] ウルトスキル 使用");
        // TODO: ウルトのエフェクト・ダメージ処理を実装
    }

    private void UseChain()
    {
        ChainRemaining = chainCooldown;
        chainUnlocked  = false;
        OnSkillUsed?.Invoke(5, chainCooldown);
        Debug.Log("[SkillSystem] 連携スキル 使用");
        // TODO: 連携スキルのエフェクト・ダメージ処理を実装
    }

    /// <summary>
    /// 連携スキルの発動条件が満たされたときに外部から呼ぶ
    /// </summary>
    public void UnlockChainSkill()
    {
        if (ChainRemaining > 0f) return;
        chainUnlocked = true;
        OnChainUnlocked?.Invoke();
        Debug.Log("[SkillSystem] 連携スキル 解放");
    }

    private float GetCooldown(int slot) => slot switch
    {
        1 => skill1Cooldown,
        2 => skill2Cooldown,
        3 => skill3Cooldown,
        4 => ultCooldown,
        5 => chainCooldown,
        _ => 0f
    };

    /// <summary>
    /// クールタイム進捗（0〜1、1=使用可能）
    /// </summary>
    public float GetSkillProgress(int slot) => slot switch
    {
        1 => 1f - Skill1Remaining / skill1Cooldown,
        2 => 1f - Skill2Remaining / skill2Cooldown,
        3 => 1f - Skill3Remaining / skill3Cooldown,
        4 => 1f - UltRemaining    / ultCooldown,
        5 => 1f - ChainRemaining  / chainCooldown,
        _ => 1f
    };
}
