using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵・ボス共通のAnimatorドライバー
/// EnemyController または BossController のどちらでも動作する
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    private Animator         anim;
    private NavMeshAgent     agent;
    private EnemyController  enemy;
    private BossController   boss;

    private static readonly int SpeedHash     = Animator.StringToHash("Speed");
    private static readonly int AttackHash    = Animator.StringToHash("Attack");
    private static readonly int HitReactHash  = Animator.StringToHash("HitReact");
    private static readonly int IsDeadHash    = Animator.StringToHash("IsDead");

    private bool isDead       = false;
    private bool isHitReacting = false;

    void Awake()
    {
        anim  = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<EnemyController>();
        boss  = GetComponent<BossController>();
    }

    /// <summary>
    /// モデル差し替え後にAnimator参照を更新する。EnemyAppearance.SwapModel()から呼ぶ。
    /// </summary>
    public void RefreshAnimator(Animator newAnim = null)
    {
        anim = newAnim != null ? newAnim : GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (enemy != null)
        {
            enemy.OnAttackAnim   += TriggerAttack;
            enemy.OnHitReactAnim += TriggerHitReact;
            enemy.OnDeathAnim    += TriggerDeath;
        }
        if (boss != null)
        {
            boss.OnAttackAnim        += TriggerAttack;
            boss.OnHitReactAnim      += TriggerHitReact;
            boss.OnDeathAnim         += TriggerBossDeath;
            boss.OnPhaseTransitionAnim += TriggerPhaseTransition;
        }
    }

    void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnAttackAnim   -= TriggerAttack;
            enemy.OnHitReactAnim -= TriggerHitReact;
            enemy.OnDeathAnim    -= TriggerDeath;
        }
        if (boss != null)
        {
            boss.OnAttackAnim        -= TriggerAttack;
            boss.OnHitReactAnim      -= TriggerHitReact;
            boss.OnDeathAnim         -= TriggerBossDeath;
            boss.OnPhaseTransitionAnim -= TriggerPhaseTransition;
        }
    }

    void Update()
    {
        if (anim == null || isDead) return;

        // NavMeshAgent の速度をSpeedパラメーターに反映
        float speed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            speed = agent.velocity.magnitude;

        anim.SetFloat(SpeedHash, speed);
    }

    private void TriggerAttack()
    {
        if (anim != null) anim.SetTrigger(AttackHash);
    }

    private void TriggerHitReact()
    {
        if (isDead) return;

        // アニメーションは毎ヒット必ず再トリガー（ノックバックと同時再生のため）
        if (anim != null) anim.SetTrigger(HitReactHash);

        // SE・エフェクト・終了待ちコルーチンは重複起動しない
        if (isHitReacting) return;

        isHitReacting = true;
        AudioManager.Instance?.PlaySE(SFX.EnemyHurt);
        EffectManager.Instance?.PlayHitEffect(transform.position);
        StartCoroutine(WaitForHitReactEnd());
    }

    /// <summary>HitReact アニメーションが終わるまで待ってフラグを解除する。</summary>
    private IEnumerator WaitForHitReactEnd()
    {
        if (anim == null) { isHitReacting = false; yield break; }

        // 遷移開始を待つ（最大20フレーム）
        int wait = 0;
        while (!anim.IsInTransition(0) && wait < 20) { yield return null; wait++; }

        // 遷移完了を待つ
        while (anim != null && anim.IsInTransition(0))
            yield return null;

        // HitReact ステートの再生が終わるまで待つ
        while (anim != null && anim.gameObject.activeInHierarchy)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.normalizedTime >= 0.9f) break;
            yield return null;
        }

        isHitReacting = false;
    }

    private void TriggerDeath()
    {
        isDead = true;
        if (anim != null)
        {
            anim.SetBool(IsDeadHash, true);
            StartCoroutine(FreezeOnDeathComplete());
        }
        AudioManager.Instance?.PlaySE(SFX.EnemyDeath);
        CameraFollow.Instance?.ShakeEnemyDeath();
        EffectManager.Instance?.PlayDeathEffect(transform.position);
    }

    private void TriggerBossDeath()
    {
        isDead = true;
        if (anim != null)
        {
            anim.SetBool(IsDeadHash, true);
            StartCoroutine(FreezeOnDeathComplete());
        }
        AudioManager.Instance?.PlaySE(SFX.BossDeath);
        CameraFollow.Instance?.ShakeBossDeath();
        EffectManager.Instance?.PlayBossDeathEffect(transform.position);
    }

    /// <summary>
    /// 死亡遷移の完了後に IsDead を false に戻して Any State の再発火を防ぎ、
    /// アニメーションが1周したら Animator をフリーズさせる。
    /// </summary>
    private IEnumerator FreezeOnDeathComplete()
    {
        if (anim == null) yield break;

        // ── Step1: 遷移が完了するまで待つ ──
        // 遷移開始を待つ（最大30フレーム）
        int waitFrames = 0;
        while (!anim.IsInTransition(0) && waitFrames < 30)
        {
            yield return null;
            waitFrames++;
        }
        // 遷移完了を待つ
        while (anim.IsInTransition(0))
            yield return null;

        if (anim == null) yield break;

        // ── Step2: IsDead を false に戻す ──
        // 死亡ステートに入った後なら Bool を戻しても死亡ステートは維持される。
        // Any State → Death が再発火するのを防ぐ。
        anim.SetBool(IsDeadHash, false);

        // ── Step3: アニメーションが終わったらフリーズ ──
        while (anim != null && anim.gameObject.activeInHierarchy)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.normalizedTime >= 0.95f)
            {
                anim.speed = 0f;
                yield break;
            }
            yield return null;
        }
    }

    private void TriggerPhaseTransition()
    {
        AudioManager.Instance?.PlaySE(SFX.BossPhaseTransition);
        AudioManager.Instance?.PlayBossBGM();
        CameraFollow.Instance?.ShakePhaseTrans();
    }
}
