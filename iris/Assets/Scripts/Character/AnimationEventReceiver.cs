using UnityEngine;

/// <summary>
/// Animator と同じ GameObject（モデル子）に置く薄いリレー。
/// Animation Event → 親の PlayerAnimator に転送する。
///
/// PlayerAppearance.SwapModel() でモデル差し替え時に自動追加される。
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private PlayerAnimator playerAnimator;

    void Awake()
    {
        playerAnimator = GetComponentInParent<PlayerAnimator>();
    }

    public void OnComboWindowOpen() => playerAnimator?.OnComboWindowOpen();
    public void OnComboEnd()        => playerAnimator?.OnComboEnd();
}
