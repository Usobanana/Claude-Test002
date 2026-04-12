using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BGM・SEを一元管理するシングルトン
/// BaseSceneのSceneManagersにアタッチし DontDestroyOnLoad で全シーンで動作する
/// AudioClipはInspectorでアサインする（未アサインの場合は無音で動作）
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM クリップ")]
    [SerializeField] private AudioClip baseBGM;
    [SerializeField] private AudioClip fieldBGM;
    [SerializeField] private AudioClip bossBGM;

    [Header("SE クリップ — プレイヤー")]
    [SerializeField] private AudioClip   playerAttackSE;
    [SerializeField] private AudioClip[] playerSwingSE;
    [SerializeField] private AudioClip[] playerHitSE;
    [SerializeField] private AudioClip   playerDodgeSE;
    [SerializeField] private AudioClip   playerHurtSE;
    [SerializeField] private AudioClip   playerDeathSE;

    [Header("SE クリップ — エネミー")]
    [SerializeField] private AudioClip enemyHurtSE;
    [SerializeField] private AudioClip enemyDeathSE;

    [Header("SE クリップ — ボス")]
    [SerializeField] private AudioClip bossPhaseTransitionSE;
    [SerializeField] private AudioClip bossDeathSE;

    [Header("SE クリップ — UI")]
    [SerializeField] private AudioClip questClearSE;

    [Header("音量")]
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float seVolume  = 0.8f;

    private AudioSource bgmSource;
    private AudioSource seSource;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource を動的に追加（2つ目はSE専用）
        bgmSource        = gameObject.AddComponent<AudioSource>();
        bgmSource.loop   = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;

        seSource         = gameObject.AddComponent<AudioSource>();
        seSource.loop    = false;
        seSource.volume  = seVolume;
        seSource.playOnAwake = false;
    }

    // =========================================================
    //  BGM
    // =========================================================

    /// <summary>シーン名に応じたBGMを再生する（同じクリップが既に流れている場合はスキップ）</summary>
    public void PlayBGM(string sceneName)
    {
        AudioClip clip = sceneName switch
        {
            "BaseScene"   => baseBGM,
            "FieldScene"  => fieldBGM,
            "ResultScene" => null,       // リザルトは無音（またはfieldBGMをフェード）
            _             => null,
        };

        SetBGM(clip);
    }

    /// <summary>ボスBGMに切り替える（Phase2移行時などに呼ぶ）</summary>
    public void PlayBossBGM()
    {
        SetBGM(bossBGM);
    }

    private void SetBGM(AudioClip clip)
    {
        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip   = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    // =========================================================
    //  SE
    // =========================================================

    public void PlaySE(SFX sfx)
    {
        AudioClip clip = sfx switch
        {
            SFX.PlayerAttack         => playerAttackSE,
            SFX.PlayerSwing          => RandomClip(playerSwingSE),
            SFX.PlayerHit            => RandomClip(playerHitSE),
            SFX.PlayerDodge          => playerDodgeSE,
            SFX.PlayerHurt           => playerHurtSE,
            SFX.PlayerDeath          => playerDeathSE,
            SFX.EnemyHurt            => enemyHurtSE,
            SFX.EnemyDeath           => enemyDeathSE,
            SFX.BossPhaseTransition  => bossPhaseTransitionSE,
            SFX.BossDeath            => bossDeathSE,
            SFX.QuestClear           => questClearSE,
            _                        => null,
        };

        if (clip == null) return;
        seSource.PlayOneShot(clip, seVolume);
    }

    private static AudioClip RandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}

/// <summary>再生するSEの種類</summary>
public enum SFX
{
    PlayerAttack,
    PlayerSwing,
    PlayerHit,
    PlayerDodge,
    PlayerHurt,
    PlayerDeath,
    EnemyHurt,
    EnemyDeath,
    BossPhaseTransition,
    BossDeath,
    QuestClear,
}
