using UnityEngine;

/// <summary>
/// Handles zombie melee attack against plants and the player's house.
/// Works with both EnemyNavAgent (NavMesh mode) and ZombiePrototypeMover (legacy mode).
/// </summary>
public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage dealt per attack tick")]
    public int damagePerAttack = 10;

    [Tooltip("Time in seconds between attacks")]
    public float attackInterval = 1.2f;

    [Tooltip("Max distance to keep attacking a plant")]
    public float attackRange = 1.8f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // ──────────────────────────────────────────────────────────
    // References — supports both AI modes
    // ──────────────────────────────────────────────────────────
    private EnemyNavAgent navAgent;           // NavMesh mode
    private ZombiePrototypeMover legacyMover; // Legacy mode

    private PlantBase currentTarget;
    private float attackTimer;

    private void Awake()
    {
        navAgent    = GetComponent<EnemyNavAgent>();
        legacyMover = GetComponent<ZombiePrototypeMover>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Pull state from whichever AI is active
        PlantBase blockingPlant = navAgent != null  ? navAgent.BlockingPlant
                                : legacyMover != null ? legacyMover.BlockingPlant
                                : null;

        bool isAtHouse = navAgent != null      ? navAgent.IsAtHouse
                       : legacyMover != null   ? legacyMover.IsAtHouse
                       : false;

        currentTarget = blockingPlant;

        if (currentTarget == null)
        {
            // Attack house if at destination
            if (isAtHouse && HouseHealth.Instance != null)
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

        // Check still in range (plant might have been destroyed)
        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist > attackRange + 0.5f)
        {
            currentTarget = null;
            ClearBlockingPlant();
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

        if (currentTarget.currentHealth <= 0)
        {
            currentTarget = null;
            ClearBlockingPlant();
        }
    }

    private void ClearBlockingPlant()
    {
        if (navAgent != null)    navAgent.ClearBlockingPlant();
        if (legacyMover != null) legacyMover.ClearBlockingPlant();
    }

    private void TriggerAttackAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == AttackHash && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(AttackHash);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
