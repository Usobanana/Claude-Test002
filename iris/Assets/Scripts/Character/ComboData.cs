using UnityEngine;

public enum HitShapeType
{
    Arc,        // 扇形（剣など）
    Rectangle,  // 矩形（槍など）
    Circle,     // 円（魔法など）
}

/// <summary>
/// 1コンボ段の中の1ヒットイベント。攻撃判定タイミング・形状・ダメージ・打感・SEを定義する。
/// </summary>
[System.Serializable]
public class HitEventData
{
    [Header("判定タイミング（フレーム）")]
    public int hitStartFrame = 5;
    public int hitEndFrame   = 15;

    [Header("判定形状")]
    public HitShapeType shapeType = HitShapeType.Arc;

    [Header("Arc 設定")]
    public float arcRange = 1.5f;
    [Range(10f, 360f)]
    public float arcAngle = 120f;

    [Header("Rectangle 設定")]
    public float rectLength        = 2f;
    public float rectWidth         = 1f;
    public float rectForwardOffset = 0f;

    [Header("Circle 設定")]
    public float   circleRadius = 1.5f;
    public Vector3 circleOffset;        // プレイヤーローカル座標からのオフセット

    [Header("ダメージ")]
    [Range(0f, 500f)]
    [Tooltip("ダメージ倍率（%）。100 = 攻撃力 × 1.0")]
    public float damageRate = 100f;

    [Header("打感")]
    [Tooltip("ヒットストップ中の timeScale（0 = 完全停止）")]
    public float hitStopTimeScale  = 0.05f;
    [Tooltip("ヒットストップ持続時間（実時間・秒）")]
    public float hitStopDuration   = 0.06f;
    [Tooltip("カメラシェイク強度")]
    public float hitShakeMagnitude = 0.08f;
    [Tooltip("カメラシェイク持続時間")]
    public float hitShakeDuration  = 0.1f;

    [Header("ノックバック")]
    public float knockbackForce    = 1.5f;
    public float knockbackDuration = 0.2f;

    [Header("SE")]
    [Tooltip("振り始めSE")]
    public SFX swingSE      = SFX.PlayerSwing;
    [Tooltip("振り始めSEを鳴らすフレーム（hitStartFrame と同じにする場合は -1）")]
    public int  swingSEFrame = -1;
    [Tooltip("ヒット時SE")]
    public SFX hitSE        = SFX.PlayerHit;

    // ─────────────────────────────────────────
    // 正規化時刻ヘルパー（ランタイム用）
    // ─────────────────────────────────────────

    public float GetStartNormalized(AnimationClip clip)
    {
        int total = GetTotalFrames(clip);
        return total > 0 ? Mathf.Clamp01((float)hitStartFrame / total) : 0f;
    }

    public float GetEndNormalized(AnimationClip clip)
    {
        int total = GetTotalFrames(clip);
        return total > 0 ? Mathf.Clamp01((float)hitEndFrame / total) : 1f;
    }

    public float GetSENormalized(AnimationClip clip)
    {
        int frame = swingSEFrame >= 0 ? swingSEFrame : hitStartFrame;
        int total = GetTotalFrames(clip);
        return total > 0 ? Mathf.Clamp01((float)frame / total) : 0f;
    }

    private static int GetTotalFrames(AnimationClip clip) =>
        clip != null ? Mathf.RoundToInt(clip.length * clip.frameRate) : 0;
}

/// <summary>
/// 1コンボ段の設定。入力受付ウィンドウとヒットイベントリストを持つ。
/// </summary>
[System.Serializable]
public class ComboStepData
{
    [Tooltip("この段で再生するアニメーションクリップ")]
    public AnimationClip clip;

    [Tooltip("入力受付を開始するフレーム番号（0始まり）")]
    public int windowStartFrame = 15;

    [Tooltip("入力受付を終了するフレーム番号（0 = クリップ末尾まで）")]
    public int windowEndFrame = 0;

    [Tooltip("攻撃判定イベント（1段に複数追加可）")]
    public HitEventData[] hitEvents = new HitEventData[] { new HitEventData() };

    public int TotalFrames => clip != null ? Mathf.RoundToInt(clip.length * clip.frameRate) : 0;

    public float WindowStartNormalized
    {
        get
        {
            int total = TotalFrames;
            return total > 0 ? Mathf.Clamp01((float)windowStartFrame / total) : 0f;
        }
    }

    public float WindowEndNormalized
    {
        get
        {
            if (windowEndFrame <= 0) return 1f;
            int total = TotalFrames;
            return total > 0 ? Mathf.Clamp01((float)windowEndFrame / total) : 1f;
        }
    }
}

/// <summary>
/// キャラクター固有のコンボ設定 ScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "ComboData", menuName = "Game/Combo Data")]
public class ComboData : ScriptableObject
{
    [Tooltip("コンボの各段設定（上から順に1段目・2段目…）")]
    public ComboStepData[] steps = new ComboStepData[3];

    public int StepCount => steps != null ? steps.Length : 0;
}
