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

    [Tooltip("Close-range fallback used when a manual-moving enemy reaches a plant collider.")]
    [SerializeField, Min(0.1f)] private float plantContactRadius = 0.9f;

    [Header("Animation")]
    [Tooltip("Disable for enemy prefabs that provide their own animation adapter.")]
    [SerializeField] private bool useSharedAttackAnimation = true;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField, Min(0.05f)] private float fallbackBiteDuration = 0.28f;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // ──────────────────────────────────────────────────────────
    // References — supports both AI modes
    // ──────────────────────────────────────────────────────────
    private EnemyNavAgent navAgent;           // NavMesh mode
    private ZombiePrototypeMover legacyMover; // Legacy mode

    private PlantBase currentTarget;
    private float attackTimer;
    private Coroutine fallbackBite;

    private void Awake()
    {
        navAgent    = GetComponent<EnemyNavAgent>();
        legacyMover = GetComponent<ZombiePrototypeMover>();

        if (useSharedAttackAnimation && animator == null)
            animator = GetComponentInChildren<Animator>();
        if (visualRoot == null && animator != null)
            visualRoot = animator.transform;
    }

    private void Update()
    {
        if (!NetworkGameplayAuthority.CanMutate) return;
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Pull state from whichever AI is active
        PlantBase blockingPlant = (navAgent != null && navAgent.enabled) ? navAgent.BlockingPlant
                                : (legacyMover != null && legacyMover.enabled) ? legacyMover.BlockingPlant
                                : null;

        bool isAtHouse = (navAgent != null && navAgent.enabled) ? navAgent.IsAtHouse
                       : (legacyMover != null && legacyMover.enabled) ? legacyMover.IsAtHouse
                       : false;

        currentTarget = blockingPlant;

        // EnemyNavAgent normally detects the plant ahead of its lane. This
        // fallback also catches real collider contact, preventing kinematic
        // manual movement from passing through a plant on either enemy prefab.
        if (currentTarget == null)
        {
            PlantBase contactedPlant = FindPlantAtContact();
            if (contactedPlant != null)
            {
                currentTarget = contactedPlant;
                SetBlockingPlant(contactedPlant);
            }
        }

        if (currentTarget == null)
        {
            // Attack house if at destination
            if (isAtHouse && HouseHealth.Instance != null)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    HouseHealth.Instance.TakeDamage(damagePerAttack);
                    AudioManager.PlaySfx(AudioCue.ZombieAttack);
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
        AudioManager.PlaySfx(AudioCue.ZombieAttack);
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

    private void OnCollisionEnter(Collision collision)
    {
        if (!NetworkGameplayAuthority.IsServer) return;
        TryBlockOnPlant(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!NetworkGameplayAuthority.IsServer) return;
        TryBlockOnPlant(collision.collider);
    }

    private void TryBlockOnPlant(Collider collider)
    {
        PlantBase plant = collider != null ? collider.GetComponentInParent<PlantBase>() : null;
        if (plant != null && plant.currentHealth > 0)
            SetBlockingPlant(plant);
    }

    private PlantBase FindPlantAtContact()
    {
        Vector3 center = transform.position + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(
            center, plantContactRadius, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        PlantBase closest = null;
        float closestDistance = float.MaxValue;
        foreach (Collider hit in hits)
        {
            PlantBase plant = hit.GetComponentInParent<PlantBase>();
            if (plant == null || plant.currentHealth <= 0) continue;

            Vector3 toPlant = (plant.transform.position - transform.position).normalized;
            toPlant.y = 0f;
            if (Vector3.Dot(transform.forward, toPlant) < -0.1f) continue;

            float distance = (hit.ClosestPoint(center) - center).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = plant;
                closestDistance = distance;
            }
        }
        return closest;
    }

    private void SetBlockingPlant(PlantBase plant)
    {
        if (plant == null || plant.currentHealth <= 0) return;

        currentTarget = plant;
        if (navAgent != null) navAgent.BlockOnPlant(plant);
        else if (legacyMover != null) legacyMover.BlockOnPlant(plant);
    }

    private void TriggerAttackAnimation()
    {
        if (!useSharedAttackAnimation) return;

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            StartFallbackBite();
            return;
        }

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == AttackHash && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(AttackHash);
                break;
            }
        }

        if (!HasAttackTrigger() && !animator.HasState(0, Animator.StringToHash(attackStateName)))
            StartFallbackBite();
        else if (!HasAttackTrigger())
            animator.CrossFadeInFixedTime(attackStateName, 0.05f);
    }

    private bool HasAttackTrigger()
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.nameHash == AttackHash && p.type == AnimatorControllerParameterType.Trigger) return true;
        return false;
    }

    private void StartFallbackBite()
    {
        if (visualRoot == null || fallbackBite != null) return;
        fallbackBite = StartCoroutine(FallbackBiteRoutine());
    }

    private System.Collections.IEnumerator FallbackBiteRoutine()
    {
        Quaternion baseRotation = visualRoot.localRotation;
        Vector3 basePosition = visualRoot.localPosition;
        float elapsed = 0f;
        while (elapsed < fallbackBiteDuration)
        {
            elapsed += Time.deltaTime;
            float bite = Mathf.Sin(Mathf.Clamp01(elapsed / fallbackBiteDuration) * Mathf.PI);
            visualRoot.localRotation = baseRotation * Quaternion.Euler(-12f * bite, 0f, 0f);
            visualRoot.localPosition = basePosition + Vector3.back * (0.08f * bite);
            yield return null;
        }
        visualRoot.localRotation = baseRotation;
        visualRoot.localPosition = basePosition;
        fallbackBite = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
