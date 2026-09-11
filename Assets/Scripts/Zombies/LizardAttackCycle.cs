using UnityEngine;

/// <summary>
/// Automatically cycles attack variations for the Lizard Monster.
/// Attached to Attack1 and Attack2 states in Lizard_Controller.
/// When Attack1 begins, it prepares the next attack to be Attack2 (AttackType = 1).
/// When Attack2 begins, it prepares the next attack to be Attack1 (AttackType = 0).
/// </summary>
public class LizardAttackCycle : StateMachineBehaviour
{
    [Tooltip("Target AttackType value to set on entering this attack state.")]
    public int nextAttackType = 1;

    [Tooltip("Parameter name for the attack selector in the animator.")]
    public string paramName = "AttackType";

    private int paramHash;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (paramHash == 0)
            paramHash = Animator.StringToHash(string.IsNullOrEmpty(paramName) ? "AttackType" : paramName);

        animator.SetInteger(paramHash, nextAttackType);
    }
}
