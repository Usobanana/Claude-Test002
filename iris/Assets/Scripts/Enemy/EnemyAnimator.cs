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

    void Awake()
    {
        anim  = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<EnemyController>();
        boss  = GetComponent<BossController>();
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
        if (anim == null) return;

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
        if (anim != null) anim.SetTrigger(HitReactHash);
        AudioManager.Instance?.PlaySE(SFX.EnemyHurt);
        EffectManager.Instance?.PlayHitEffect(transform.position);
    }

    private void TriggerDeath()
    {
        if (anim != null) anim.SetBool(IsDeadHash, true);
        AudioManager.Instance?.PlaySE(SFX.EnemyDeath);
        CameraFollow.Instance?.ShakeEnemyDeath();
        EffectManager.Instance?.PlayDeathEffect(transform.position);
    }

    private void TriggerBossDeath()
    {
        if (anim != null) anim.SetBool(IsDeadHash, true);
        AudioManager.Instance?.PlaySE(SFX.BossDeath);
        CameraFollow.Instance?.ShakeBossDeath();
        EffectManager.Instance?.PlayBossDeathEffect(transform.position);
    }

    private void TriggerPhaseTransition()
    {
        AudioManager.Instance?.PlaySE(SFX.BossPhaseTransition);
        AudioManager.Instance?.PlayBossBGM();
        CameraFollow.Instance?.ShakePhaseTrans();
    }
}
