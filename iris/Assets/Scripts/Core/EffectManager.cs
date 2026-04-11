using UnityEngine;

/// <summary>
/// ヒット・死亡パーティクルエフェクトを一元管理するシングルトン
/// BaseScene の SceneManagers にアタッチし DontDestroyOnLoad で全シーン共有
/// ParticleSystem プレハブは Inspector でアサインする（未アサインの場合は無音で動作）
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("パーティクルプレハブ")]
    [SerializeField] private ParticleSystem hitEffectPrefab;
    [SerializeField] private ParticleSystem deathEffectPrefab;
    [SerializeField] private ParticleSystem bossDeathEffectPrefab;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>通常ヒット時のエフェクト</summary>
    public void PlayHitEffect(Vector3 position)   => Play(hitEffectPrefab,      position);

    /// <summary>ザコ死亡時のエフェクト</summary>
    public void PlayDeathEffect(Vector3 position) => Play(deathEffectPrefab,    position);

    /// <summary>ボス死亡時のエフェクト</summary>
    public void PlayBossDeathEffect(Vector3 position) => Play(bossDeathEffectPrefab, position);

    private void Play(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;
        var ps = Instantiate(prefab, position, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
    }
}
