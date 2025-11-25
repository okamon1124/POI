using UnityEngine;

public class UiCardAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] private string attackAnimationName = "Attack";

    public System.Action OnAttackHitCallback;

    public void PlayAttackAnimation()
    {
        if (animator != null)
            animator.Play(attackAnimationName);
    }

    public void OnAttackHitAnimationEvent()
    {
        OnAttackHitCallback?.Invoke();
    }
}
