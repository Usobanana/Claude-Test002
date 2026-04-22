using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ComboData の HitEventData に基づいて攻撃判定・ダメージ・打感・SEを処理するコンポーネント。
/// PlayerAnimator から UpdateDetection() を毎フレーム呼び出す。
/// </summary>
[RequireComponent(typeof(CharacterEntity))]
public class HitDetector : MonoBehaviour
{
    private CharacterEntity entity;
    private ComboStepData   currentStep;

    private bool[]               sePlayedFlags;
    private bool[]               windowOpenFlags;
    private HashSet<GameObject>[] hitSets;

    private bool isHitStopping;

    private static readonly Collider[] overlapBuffer = new Collider[16];

    void Awake()
    {
        entity = GetComponent<CharacterEntity>();
    }

    // ─────────────────────────────────────────
    // PlayerAnimator から呼ばれる公開API
    // ─────────────────────────────────────────

    public void StartStep(ComboStepData step)
    {
        ForceCloseAllWindows();

        currentStep = step;
        int count   = step?.hitEvents?.Length ?? 0;

        sePlayedFlags   = new bool[count];
        windowOpenFlags = new bool[count];
        hitSets         = new HashSet<GameObject>[count];
        for (int i = 0; i < count; i++)
            hitSets[i] = new HashSet<GameObject>();
    }

    public void StopStep()
    {
        ForceCloseAllWindows();
        currentStep     = null;
        sePlayedFlags   = null;
        windowOpenFlags = null;
        hitSets         = null;
    }

    private void ForceCloseAllWindows()
    {
        if (isHitStopping)
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            isHitStopping  = false;
        }
    }

    /// <summary>
    /// normalizedTime（0〜1+）を受け取り、各ヒットイベントの判定・SE再生を行う。
    /// PlayerAnimator.UpdateComboState() から毎フレーム呼ぶ。
    /// </summary>
    public void UpdateDetection(float normalizedTime)
    {
        if (currentStep?.hitEvents == null) return;

        float t = normalizedTime % 1f;

        for (int i = 0; i < currentStep.hitEvents.Length; i++)
        {
            var   evt    = currentStep.hitEvents[i];
            var   clip   = currentStep.clip;
            float startN = evt.GetStartNormalized(clip);
            float endN   = evt.GetEndNormalized(clip);
            float seN    = evt.GetSENormalized(clip);

            // SE タイミング
            if (!sePlayedFlags[i] && t >= seN)
            {
                AudioManager.Instance?.PlaySE(evt.swingSE);
                sePlayedFlags[i] = true;
            }

            bool inWindow = t >= startN && t < endN;

            if (inWindow && !windowOpenFlags[i])
            {
                windowOpenFlags[i] = true;
                hitSets[i].Clear();
            }
            else if (!inWindow && windowOpenFlags[i])
            {
                windowOpenFlags[i] = false;
            }

            if (windowOpenFlags[i])
                PerformDetection(evt, hitSets[i]);
        }
    }

    // ─────────────────────────────────────────
    // 当たり判定
    // ─────────────────────────────────────────

    private void PerformDetection(HitEventData evt, HashSet<GameObject> hitSet)
    {
        if (entity?.Stats == null) return;

        int count = QueryOverlap(evt);

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuffer[i];
            if (col.transform.root == transform.root) continue;

            var rootGO = col.transform.root.gameObject;
            if (hitSet.Contains(rootGO)) continue;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) continue;

            // 扇形チェック
            if (evt.shapeType == HitShapeType.Arc)
            {
                Vector3 dir   = (col.transform.position - transform.position).normalized;
                float   angle = Vector3.Angle(transform.forward, dir);
                if (angle > evt.arcAngle * 0.5f) continue;
            }

            hitSet.Add(rootGO);

            float defense = GetTargetDefense(rootGO);
            float damage  = DamageCalculator.Calculate(
                attackerStats:   entity.Stats,
                skillMultiplier: evt.damageRate / 100f,
                targetDefense:   defense,
                attackerLevel:   entity.Level
            );
            damageable.TakeDamage(damage);

            var brain = rootGO.GetComponent<EnemyBrain>();
            brain?.ApplyKnockback(transform.position, evt.knockbackForce, evt.knockbackDuration);

            OnHit(evt, col.transform.position);
        }
    }

    private int QueryOverlap(HitEventData evt)
    {
        switch (evt.shapeType)
        {
            case HitShapeType.Arc:
                return Physics.OverlapSphereNonAlloc(transform.position, evt.arcRange, overlapBuffer);

            case HitShapeType.Circle:
                var center = transform.position + transform.TransformDirection(evt.circleOffset);
                return Physics.OverlapSphereNonAlloc(center, evt.circleRadius, overlapBuffer);

            case HitShapeType.Rectangle:
                var boxCenter     = transform.position + transform.forward * (evt.rectForwardOffset + evt.rectLength * 0.5f);
                var halfExtents   = new Vector3(evt.rectWidth * 0.5f, 1f, evt.rectLength * 0.5f);
                return Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, overlapBuffer, transform.rotation);

            default:
                return 0;
        }
    }

    private void OnHit(HitEventData evt, Vector3 hitPos)
    {
        AudioManager.Instance?.PlaySE(evt.hitSE);
        EffectManager.Instance?.PlayHitEffect(hitPos);
        CameraFollow.Instance?.ShakePlayerAttackHit(evt.hitShakeMagnitude, evt.hitShakeDuration);
        if (!isHitStopping)
            StartCoroutine(HitStop(evt));
    }

    private IEnumerator HitStop(HitEventData evt)
    {
        isHitStopping  = true;
        Time.timeScale = evt.hitStopTimeScale;
        yield return new WaitForSecondsRealtime(evt.hitStopDuration);
        Time.timeScale = 1f;
        isHitStopping  = false;
    }

    private static float GetTargetDefense(GameObject target)
    {
        var enemy = target.GetComponentInParent<EnemyController>();
        if (enemy != null) return enemy.Defense;
        var boss  = target.GetComponentInParent<BossController>();
        if (boss  != null) return boss.Defense;
        return 0f;
    }

    // ─────────────────────────────────────────
    // Gizmo
    // ─────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (currentStep?.hitEvents == null) return;
        for (int i = 0; i < currentStep.hitEvents.Length; i++)
        {
            var  evt    = currentStep.hitEvents[i];
            bool active = windowOpenFlags != null && i < windowOpenFlags.Length && windowOpenFlags[i];
            DrawEventGizmo(evt, active);
        }
    }

    private void DrawEventGizmo(HitEventData evt, bool active)
    {
        Color fill = active ? new Color(1f, 0.3f, 0f, 0.35f) : new Color(1f, 1f, 0f, 0.08f);
        Color wire = active ? new Color(1f, 0.4f, 0f, 1f)    : new Color(1f, 1f, 0f, 0.4f);

        switch (evt.shapeType)
        {
            case HitShapeType.Arc:
                DrawArcGizmo(evt, fill, wire);
                break;
            case HitShapeType.Circle:
                Gizmos.color = wire;
                Gizmos.DrawWireSphere(transform.position + transform.TransformDirection(evt.circleOffset), evt.circleRadius);
                break;
            case HitShapeType.Rectangle:
                Gizmos.color = wire;
                var center = transform.position + transform.forward * (evt.rectForwardOffset + evt.rectLength * 0.5f);
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(evt.rectWidth, 2f, evt.rectLength));
                Gizmos.matrix = Matrix4x4.identity;
                break;
        }
    }

    private void DrawArcGizmo(HitEventData evt, Color fill, Color wire)
    {
        float   half     = evt.arcAngle * 0.5f;
        int     segments = 24;
        Vector3 origin   = transform.position;

        Vector3 prev = origin + Quaternion.Euler(0, -half, 0) * transform.forward * evt.arcRange;
        Gizmos.color = wire;
        Gizmos.DrawLine(origin, prev);

        for (int i = 1; i <= segments; i++)
        {
            float   a    = -half + evt.arcAngle / segments * i;
            Vector3 curr = origin + Quaternion.Euler(0, a, 0) * transform.forward * evt.arcRange;
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
        Gizmos.DrawLine(origin, prev);

        Gizmos.color = fill;
        for (int i = 0; i <= segments; i++)
        {
            float   a   = -half + evt.arcAngle / segments * i;
            Vector3 dir = origin + Quaternion.Euler(0, a, 0) * transform.forward * evt.arcRange;
            Gizmos.DrawLine(origin, dir);
        }
    }
}
