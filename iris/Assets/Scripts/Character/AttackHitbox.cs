using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Playerの攻撃当たり判定をAnimation Eventと連動させるコンポーネント。
///
/// 【使い方】
/// 1. PlayerオブジェクトにこのコンポーネントをAdd。
/// 2. AnimationEventReceiver を Animatorと同じGameObject（モデル子）に置く。
/// 3. 攻撃アニメーションに Animation Event を追加:
///      - 剣を振り始めるフレーム → OpenAttackWindow
///      - 剣が振り終わるフレーム → CloseAttackWindow
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("判定設定")]
    [Tooltip("攻撃が届く距離")]
    public float arcRange = 1.5f;

    [Tooltip("前方からの判定角度（120 = 左右各60°）")]
    [Range(10f, 360f)]
    public float arcAngle = 120f;

    [Header("打感設定")]
    [Tooltip("ヒットストップ中の timeScale（0 = 完全停止）")]
    [SerializeField] private float hitStopTimeScale  = 0.05f;
    [Tooltip("ヒットストップ持続時間（実時間・秒）")]
    [SerializeField] private float hitStopDuration   = 0.06f;
    [Tooltip("ヒット時カメラシェイクの強さ")]
    [SerializeField] private float hitShakeMagnitude = 0.08f;
    [Tooltip("ヒット時カメラシェイクの持続時間")]
    [SerializeField] private float hitShakeDuration  = 0.1f;

    [Header("ノックバック設定")]
    [Tooltip("ノックバックの吹き飛び距離")]
    [SerializeField] private float knockbackForce    = 1.5f;
    [Tooltip("ノックバックの移動時間（秒）")]
    [SerializeField] private float knockbackDuration = 0.2f;

    /// <summary>現在ウィンドウが開いているか</summary>
    public bool IsWindowOpen { get; private set; }

    private CharacterEntity               entity;
    private readonly HashSet<GameObject>  hitThisSwing = new HashSet<GameObject>();
    private bool                          isHitStopping;

    void Awake()
    {
        entity = GetComponent<CharacterEntity>();
    }

    // ─────────────────────────────────────────
    // Animation Event から呼ばれるメソッド
    // （AnimationEventReceiver 経由）
    // ─────────────────────────────────────────

    /// <summary>攻撃ウィンドウを開く（Animation Event: 剣が振り始めるフレーム）</summary>
    public void OpenAttackWindow()
    {
        IsWindowOpen = true;
        hitThisSwing.Clear();
    }

    /// <summary>攻撃ウィンドウを閉じる（Animation Event: 剣が振り終わるフレーム）</summary>
    public void CloseAttackWindow()
    {
        IsWindowOpen = false;
    }

    // ─────────────────────────────────────────
    // 判定ループ
    // ─────────────────────────────────────────

    void Update()
    {
        if (!IsWindowOpen || entity?.Stats == null) return;

        var hits = Physics.OverlapSphere(transform.position, arcRange);
        foreach (var hit in hits)
        {
            // 自分自身を除外
            if (hit.transform.root == transform.root) continue;

            // ルートオブジェクトを基準に重複チェック（子コライダー対策）
            var rootGO = hit.transform.root.gameObject;
            if (hitThisSwing.Contains(rootGO)) continue;

            // IDamageable は親方向へ検索（EnemyController はルートにある）
            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) continue;

            // 扇形チェック（前方 arcAngle 度以内のみ）
            Vector3 dir   = (hit.transform.position - transform.position).normalized;
            float   angle = Vector3.Angle(transform.forward, dir);
            if (angle > arcAngle * 0.5f) continue;

            hitThisSwing.Add(rootGO);

            // ダメージ計算
            float defense = GetTargetDefense(rootGO);
            float damage  = DamageCalculator.Calculate(
                attackerStats:   entity.Stats,
                skillMultiplier: 1.0f,
                targetDefense:   defense,
                attackerLevel:   entity.Level
            );

            damageable.TakeDamage(damage);

            // ノックバック
            var brain = rootGO.GetComponent<EnemyBrain>();
            brain?.ApplyKnockback(transform.position, knockbackForce, knockbackDuration);

            OnAttackHit(hit.transform.position);
        }
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    /// <summary>Animation Event: 剣を振り始めるフレームに風切音を鳴らす</summary>
    public void PlaySwingSE()
    {
        AudioManager.Instance?.PlaySE(SFX.PlayerSwing);
    }

    private void OnAttackHit(Vector3 hitPosition)
    {
        AudioManager.Instance?.PlaySE(SFX.PlayerHit);
        EffectManager.Instance?.PlayHitEffect(hitPosition);
        CameraFollow.Instance?.ShakePlayerAttackHit(hitShakeMagnitude, hitShakeDuration);
        if (!isHitStopping)
            StartCoroutine(HitStop());
    }

    private IEnumerator HitStop()
    {
        isHitStopping  = true;
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
        isHitStopping  = false;
    }

    private static float GetTargetDefense(GameObject target)
    {
        var enemy = target.GetComponentInParent<EnemyController>();
        if (enemy != null) return enemy.Defense;
        var boss = target.GetComponentInParent<BossController>();
        if (boss  != null) return boss.Defense;
        return 0f;
    }

    // ─────────────────────────────────────────
    // Gizmo（シーンビュー可視化）
    // ─────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (IsWindowOpen)
            DrawArcGizmo(new Color(1f, 0.3f, 0f, 0.35f), new Color(1f, 0.4f, 0f, 1f));
        else
            DrawArcGizmo(new Color(1f, 1f, 0f, 0.08f),   new Color(1f, 1f, 0f, 0.4f));
    }

    private void DrawArcGizmo(Color fill, Color wire)
    {
        float   half     = arcAngle * 0.5f;
        int     segments = 24;
        Vector3 origin   = transform.position;

        // 外弧
        Vector3 prev = origin + Quaternion.Euler(0, -half, 0) * transform.forward * arcRange;
        Gizmos.color = wire;
        Gizmos.DrawLine(origin, prev);

        for (int i = 1; i <= segments; i++)
        {
            float   a    = -half + arcAngle / segments * i;
            Vector3 curr = origin + Quaternion.Euler(0, a, 0) * transform.forward * arcRange;
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
        Gizmos.DrawLine(origin, prev);

        // 塗り（半透明の放射線）
        Gizmos.color = fill;
        for (int i = 0; i <= segments; i++)
        {
            float   a   = -half + arcAngle / segments * i;
            Vector3 dir = origin + Quaternion.Euler(0, a, 0) * transform.forward * arcRange;
            Gizmos.DrawLine(origin, dir);
        }
    }
}
