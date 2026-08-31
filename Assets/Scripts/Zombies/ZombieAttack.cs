using UnityEngine;

/// <summary>
/// Handles zombie melee attack against plants.
/// Attach to the zombie root. Works alongside ZombiePrototypeMover.
/// </summary>
[RequireComponent(typeof(ZombiePrototypeMover))]
public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage dealt per attack tick")]
    public int damagePerAttack = 10;

    [Tooltip("Time in seconds between attacks")]
    public float attackInterval = 1.2f;

    [Tooltip("Max distance to keep attacking a plant")]
    public float attackRange = 1.0f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private PlantBase currentTarget;
    private float attackTimer = 0f;
    private ZombiePrototypeMover mover;

    private void Awake()
    {
        mover = GetComponent<ZombiePrototypeMover>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Ask the mover what plant it is blocked by.
        currentTarget = mover.BlockingPlant;

        if (currentTarget == null)
        {
            if (mover.IsAtHouse && HouseHealth.Instance != null)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    HouseHealth.Instance.TakeDamage(damagePerAttack);
                    TriggerAttackAnimation();
                    attackTimer = attackInterval;
                }
            }
            else
            {
                attackTimer = 0f;
            }
            return;
        }

        // Check still in range (plant might have been removed)
        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist > attackRange + 0.5f)
        {
            currentTarget = null;
            mover.ClearBlockingPlant();
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackInterval;
        }
    }

    private void PerformAttack()
    {
        if (currentTarget == null) return;

        currentTarget.TakeDamage(damagePerAttack);

        TriggerAttackAnimation();

        Debug.Log($"[ZombieAttack] {gameObject.name} hit {currentTarget.gameObject.name} for {damagePerAttack} dmg");
    }

    private void TriggerAttackAnimation()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == AttackHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(AttackHash);
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
