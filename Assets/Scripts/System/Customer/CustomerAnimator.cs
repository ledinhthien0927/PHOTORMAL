using UnityEngine;

[DisallowMultipleComponent]
public class CustomerAnimator : MonoBehaviour
{
    private Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private void Awake()
    {
        // Try to get animator from this object or children
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Sets the IsWalking parameter in the Animator.
    /// </summary>
    /// <param name="isWalking">True if moving, false if idle.</param>
    public void SetWalking(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, isWalking);
        }
    }
}
