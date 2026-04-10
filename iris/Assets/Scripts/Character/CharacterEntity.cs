using UnityEngine;
using System;

/// <summary>
/// キャラクターのランタイム状態（現在HP・スタミナなど）を管理するコンポーネント
/// PlayerController・EnemyControllerの両方から使用する
/// </summary>
public class CharacterEntity : MonoBehaviour
{
    [SerializeField] private CharacterData data;

    // --- ランタイム値 ---
    public float CurrentHp      { get; private set; }
    public float CurrentStamina { get; private set; }
    public int   Level          { get; private set; }
    public int   Exp            { get; private set; }
    // data未アサイン時は生存扱い（拠点など戦闘外シーンで動けるようにする）
    public bool  IsAlive        => data == null || CurrentHp > 0f;

    // --- イベント ---
    public event Action<float, float> OnHpChanged;       // (current, max)
    public event Action<float, float> OnStaminaChanged;  // (current, max)
    public event Action               OnHurt;            // ダメージを受けたが生存
    public event Action               OnDeath;

    public CharacterData Data => data;
    public CharacterStats Stats => data != null ? data.baseStats : null;

    void Awake()
    {
        if (data != null) Initialize(data);
    }

    public void Initialize(CharacterData characterData)
    {
        data = characterData;
        CurrentHp      = data.baseStats.maxHp;
        CurrentStamina = data.baseStats.maxStamina;
        Level          = data.baseStats.level;
        Exp            = data.baseStats.exp;
    }

    void Update()
    {
        RegenerateStamina();
    }

    // --- HP ---

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, data.baseStats.maxHp);
        if (CurrentHp <= 0f)
            OnDeath?.Invoke();
        else
            OnHurt?.Invoke();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        CurrentHp = Mathf.Min(data.baseStats.maxHp, CurrentHp + amount);
        OnHpChanged?.Invoke(CurrentHp, data.baseStats.maxHp);
    }

    // --- スタミナ ---

    public bool ConsumeStamina(float amount)
    {
        if (CurrentStamina < amount) return false;
        CurrentStamina -= amount;
        OnStaminaChanged?.Invoke(CurrentStamina, data.baseStats.maxStamina);
        return true;
    }

    private void RegenerateStamina()
    {
        if (data == null) return;
        if (CurrentStamina >= data.baseStats.maxStamina) return;
        CurrentStamina = Mathf.Min(
            data.baseStats.maxStamina,
            CurrentStamina + data.baseStats.staminaRegenPerSec * Time.deltaTime
        );
        OnStaminaChanged?.Invoke(CurrentStamina, data.baseStats.maxStamina);
    }

    // --- EXP / Lv ---

    public void GainExp(int amount)
    {
        Exp += amount;
        while (Exp >= data.baseStats.expToNextLevel)
        {
            Exp -= data.baseStats.expToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        // TODO: レベルアップ時のステータス成長（仕様保留中）
        Debug.Log($"[CharacterEntity] Level Up! -> Lv{Level}");
    }
}
