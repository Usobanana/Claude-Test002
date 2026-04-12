using UnityEngine;

/// <summary>
/// Animator と同じ GameObject（モデル子）に置く薄いリレー。
/// Animation Event → 親の AttackHitbox / PlayerAnimator に転送する。
///
/// PlayerAppearance.SwapModel() でモデル差し替え時に自動追加される。
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private AttackHitbox   hitbox;
    private PlayerAnimator playerAnimator;

    void Awake()
    {
        hitbox         = GetComponentInParent<AttackHitbox>();
        playerAnimator = GetComponentInParent<PlayerAnimator>();
    }

    // ── AttackHitbox へのリレー ──────────────

    /// <summary>Animation Event: 剣を振り始めるフレームにセット（風切音）</summary>
    public void PlaySwingSE()        => hitbox?.PlaySwingSE();

    /// <summary>Animation Event: 剣を振り始めるフレームにセット</summary>
    public void OpenAttackWindow()   => hitbox?.OpenAttackWindow();

    /// <summary>Animation Event: 剣が当たり終わるフレームにセット</summary>
    public void CloseAttackWindow()  => hitbox?.CloseAttackWindow();

    // ── PlayerAnimator へのリレー（既存互換）──

    public void OnComboWindowOpen() => playerAnimator?.OnComboWindowOpen();
    public void OnComboEnd()        => playerAnimator?.OnComboEnd();
}
