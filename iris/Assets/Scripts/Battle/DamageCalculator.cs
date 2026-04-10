using UnityEngine;

/// <summary>
/// ダメージ計算式（ZZZ準拠）
/// 最終ダメージ = 攻撃力 × スキル倍率 × ダメージボーナス × 会心補正 × 防御補正 × 属性耐性補正
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// ダメージを計算して返す
    /// </summary>
    /// <param name="attackerStats">攻撃側のステータス</param>
    /// <param name="skillMultiplier">スキル倍率（通常攻撃=1.0、スキルごとに設定）</param>
    /// <param name="targetDefense">防御対象の防御力</param>
    /// <param name="targetResistance">防御対象の属性耐性（0.1 = 10%耐性）</param>
    /// <param name="attackerLevel">攻撃側のレベル（防御補正係数に使用）</param>
    public static float Calculate(
        CharacterStats attackerStats,
        float skillMultiplier,
        float targetDefense,
        float targetResistance = 0f,
        int   attackerLevel    = 1)
    {
        // 攻撃力
        float attack = attackerStats.attack;

        // ダメージボーナス補正（将来バフ等で加算）
        float damageBonus = 1f;

        // 会心補正
        float critMult = attackerStats.RollCritMultiplier();

        // 防御補正：Lv係数 / (Lv係数 + 有効防御力)
        float levelCoef      = LevelCoefficient(attackerLevel);
        float effectiveDef   = Mathf.Max(0f, targetDefense);
        float defenseMult    = levelCoef / (levelCoef + effectiveDef);

        // 属性耐性補正
        float resistanceMult = 1f - targetResistance;

        float result = attack * skillMultiplier * damageBonus * critMult * defenseMult * resistanceMult;
        return Mathf.Max(1f, Mathf.Round(result));
    }

    /// <summary>
    /// レベルによる係数（暫定：Lv * 100）
    /// </summary>
    private static float LevelCoefficient(int level) => level * 100f;
}
