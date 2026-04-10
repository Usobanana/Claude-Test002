using System;
using UnityEngine;

/// <summary>
/// キャラクターの基礎ステータスを保持するデータクラス
/// </summary>
[Serializable]
public class CharacterStats
{
    [Header("基礎ステータス")]
    public float maxHp        = 1000f;
    public float attack       = 100f;
    public float defense      = 50f;
    public float maxStamina   = 100f;
    public float critRate     = 0.05f;  // 5%
    public float critDamage   = 1.5f;   // 150%

    [Header("スタミナ回復")]
    public float staminaRegenPerSec = 20f;

    [Header("Lv・EXP")]
    public int level = 1;
    public int exp   = 0;
    public int expToNextLevel = 100;

    /// <summary>
    /// 会心を考慮した期待ダメージ倍率
    /// </summary>
    public float CritExpectedMultiplier => 1f + critRate * (critDamage - 1f);

    /// <summary>
    /// 会心判定を行いダメージ倍率を返す
    /// </summary>
    public float RollCritMultiplier()
    {
        return UnityEngine.Random.value < critRate ? critDamage : 1f;
    }
}
