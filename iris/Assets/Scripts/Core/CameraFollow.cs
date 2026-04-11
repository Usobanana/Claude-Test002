using UnityEngine;

/// <summary>
/// トップダウンビュー用カメラ追従スクリプト
/// ・SmoothDampによるプレイヤー追従
/// ・カメラシェイク（プレイヤー被弾・敵死亡・ボス演出）
/// ・ボスフェーズ移行時のズームイン/アウト
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("追従")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3   normalOffset    = new Vector3(0f, 8f, -5f);
    [SerializeField] private Vector3   bossOffset      = new Vector3(0f, 6f, -4f);
    [SerializeField] private float     followSmooth    = 0.1f;
    [SerializeField] private float     zoomSmoothTime  = 1.5f;

    [Header("カメラシェイク")]
    [SerializeField] private float playerHurtMag    = 0.3f;
    [SerializeField] private float playerHurtDur    = 0.25f;
    [SerializeField] private float enemyDeathMag    = 0.2f;
    [SerializeField] private float enemyDeathDur    = 0.15f;
    [SerializeField] private float bossDeathMag     = 0.6f;
    [SerializeField] private float bossDeathDur     = 0.6f;
    [SerializeField] private float phaseTransMag    = 0.5f;
    [SerializeField] private float phaseTransDur    = 0.4f;

    // --- 内部状態 ---
    private Vector3 followVelocity;
    private Vector3 zoomVelocity;
    private Vector3 currentOffset;
    private Vector3 targetOffset;

    private float   shakeMagnitude;
    private float   shakeDuration;
    private float   shakeTimer;

    // --- イベント購読管理 ---
    private CharacterEntity playerEntity;
    private BossController  boss;

    // ─────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentOffset = normalOffset;
        targetOffset  = normalOffset;
        SubscribeEvents();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        // ターゲット未設定なら Player タグから自動取得
        if (target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                TrySubscribePlayer(player);
            }
            return;
        }

        // ズームオフセットを補間
        currentOffset = Vector3.SmoothDamp(
            currentOffset, targetOffset, ref zoomVelocity, zoomSmoothTime
        );

        // シェイク量を計算（時間経過とともに減衰）
        Vector3 shake = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float ratio = Mathf.Clamp01(shakeTimer / shakeDuration);
            shake = Random.insideUnitSphere * shakeMagnitude * ratio;
            shake.y = 0f;   // 上から見下ろしカメラなのでY揺れは抑制
        }

        Vector3 targetPos = target.position + currentOffset + shake;
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref followVelocity, followSmooth
        );
    }

    // ─────────────────────────────────────────
    // 公開メソッド
    // ─────────────────────────────────────────

    /// <summary>カメラシェイクを起動する</summary>
    public void Shake(float magnitude, float duration)
    {
        // 既に大きいシェイク中なら上書きしない
        if (shakeTimer > 0f && magnitude <= shakeMagnitude) return;
        shakeMagnitude = magnitude;
        shakeDuration  = duration;
        shakeTimer     = duration;
    }

    /// <summary>プレイヤー被弾シェイク（EnemyAnimator などから呼ぶ）</summary>
    public void ShakePlayerHurt()  => Shake(playerHurtMag, playerHurtDur);

    /// <summary>雑魚敵死亡シェイク（EnemyAnimator から呼ぶ）</summary>
    public void ShakeEnemyDeath()  => Shake(enemyDeathMag, enemyDeathDur);

    /// <summary>ボス死亡シェイク</summary>
    public void ShakeBossDeath()   => Shake(bossDeathMag,  bossDeathDur);

    /// <summary>ボス第2フェーズシェイク</summary>
    public void ShakePhaseTrans()  => Shake(phaseTransMag, phaseTransDur);

    /// <summary>ボス戦ズームイン（フェーズ移行時）</summary>
    public void ZoomToBoss()   => targetOffset = bossOffset;

    /// <summary>通常ズームに戻す</summary>
    public void ZoomToNormal() => targetOffset = normalOffset;

    // ─────────────────────────────────────────
    // イベント購読
    // ─────────────────────────────────────────

    private void SubscribeEvents()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            TrySubscribePlayer(player);
        }

        boss = Object.FindAnyObjectByType<BossController>();
        if (boss != null)
        {
            boss.OnPhaseTransitionAnim += OnBossPhaseTransition;
            boss.OnDeathAnim           += OnBossDeath;
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerEntity != null)
        {
            playerEntity.OnHurt  -= OnPlayerHurt;
            playerEntity.OnDeath -= OnPlayerDeathShake;
        }
        if (boss != null)
        {
            boss.OnPhaseTransitionAnim -= OnBossPhaseTransition;
            boss.OnDeathAnim           -= OnBossDeath;
        }
    }

    private void TrySubscribePlayer(GameObject player)
    {
        if (playerEntity != null) return;
        playerEntity = player.GetComponent<CharacterEntity>();
        if (playerEntity != null)
        {
            playerEntity.OnHurt  += OnPlayerHurt;
            playerEntity.OnDeath += OnPlayerDeathShake;
        }
    }

    // ─────────────────────────────────────────
    // イベントハンドラー
    // ─────────────────────────────────────────

    private void OnPlayerHurt()        => Shake(playerHurtMag,        playerHurtDur);
    private void OnPlayerDeathShake()  => Shake(playerHurtMag * 2f,   playerHurtDur * 2f);
    private void OnBossPhaseTransition() { Shake(phaseTransMag, phaseTransDur); ZoomToBoss(); }
    private void OnBossDeath()           { Shake(bossDeathMag,  bossDeathDur);  ZoomToNormal(); }
}
