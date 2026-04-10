using UnityEngine;

/// <summary>
/// PlayerControllerの状態をAnimatorに反映する
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(CharacterEntity))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator         anim;
    private PlayerController controller;
    private CharacterEntity  entity;

    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DodgeHash  = Animator.StringToHash("Dodge");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private bool wasDodging;

    void Awake()
    {
        anim       = GetComponentInChildren<Animator>();
        controller = GetComponent<PlayerController>();
        entity     = GetComponent<CharacterEntity>();
    }

    void Start()
    {
        if (entity != null)
            entity.OnDeath += OnDeath;
    }

    void OnDestroy()
    {
        if (entity != null)
            entity.OnDeath -= OnDeath;
    }

    void Update()
    {
        if (InputHandler.Instance == null) return;

        // 移動速度をAnimatorに反映
        Vector2 input = InputHandler.Instance.MoveInput;
        anim.SetFloat(SpeedHash, input.magnitude);

        // 回避（開始時に1回だけトリガー）
        bool isDodging = controller.IsDodging;
        if (isDodging && !wasDodging)
            anim.SetTrigger(DodgeHash);
        wasDodging = isDodging;

        // オートアタック（AutoAttackSystemから呼ばれる想定だが、ここでは簡易的にトリガー）
    }

    /// <summary>
    /// AutoAttackSystem から攻撃アニメーションを起動する
    /// </summary>
    public void TriggerAttack()
    {
        anim.SetTrigger(AttackHash);
    }

    private void OnDeath()
    {
        anim.SetBool(IsDeadHash, true);
    }
}
