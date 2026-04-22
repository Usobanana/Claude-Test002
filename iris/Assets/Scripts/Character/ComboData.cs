using UnityEngine;

/// <summary>
/// 1コンボ段の設定。フレーム番号で入力受付ウィンドウを指定する。
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
/// キャラクター固有のコンボ設定。CharacterData にアサインする。
/// 段数・各段のクリップ・入力ウィンドウを自由に設定できる。
/// </summary>
[CreateAssetMenu(fileName = "ComboData", menuName = "Game/Combo Data")]
public class ComboData : ScriptableObject
{
    [Tooltip("コンボの各段設定（上から順に1段目・2段目…）")]
    public ComboStepData[] steps = new ComboStepData[3];

    public int StepCount => steps != null ? steps.Length : 0;
}
