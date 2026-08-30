using UnityEngine;

/// <summary>
/// Moves a zombie from spawn point toward the player's base along a lane.
/// Supports two modes:
///   - Patrol: legacy back-and-forth for the ZombiePrototype scene.
///   - Lane:   walks straight toward -X (toward base) and stops when blocked by a plant.
/// </summary>
public sealed class ZombiePrototypeMover : MonoBehaviour
{
    public enum MoveMode { Patrol, Lane }

    // ──────────────────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────────────────
    [Header("Mode")]
    [SerializeField] private MoveMode moveMode = MoveMode.Lane;

    [Header("Patrol (legacy)")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;

    [Header("Lane Walk")]
    [Tooltip("Direction the zombie advances toward (world space). Default: -X.")]
    [SerializeField] private Vector3 advanceDirection = Vector3.left;
    [Tooltip("X coordinate that counts as the base / lose condition.")]
    [SerializeField] public float baseBoundaryX = -7f;
    [Tooltip("X coordinate where a diagonal spawn approach becomes a straight lane walk.")]
    [SerializeField] private float laneEntryX = 6.2f;
    [SerializeField] private float assignedLaneZ;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 0.95f;
    [SerializeField, Min(1f)] private float turnSpeed = 220f;
    [SerializeField, Min(0f)] private float pauseDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.04f;

    [Header("Plant Detection")]
    [Tooltip("Radius of the forward sensor sphere that detects PlantBase colliders.")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 0.55f;
    [Tooltip("How far ahead to cast the detection sphere.")]
    [SerializeField, Min(0f)] private float detectionOffset = 0.8f;
    [Tooltip("Layer mask for plant body colliders.")]
    [SerializeField] private LayerMask plantLayerMask = ~0;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float animationDampTime = 0.12f;
    [Tooltip("Set to true if the imported model's forward axis is local -Z.")]
    [SerializeField] private bool visualFacesBackward = true;

    // ──────────────────────────────────────────────────────────
    // Runtime state
    // ──────────────────────────────────────────────────────────
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

    // Patrol mode
    private Transform currentTarget;
    private float pauseTimer;

    // Lane mode — exposed so ZombieAttack can read it
    private PlantBase blockingPlant;
    public PlantBase BlockingPlant => blockingPlant;
    public bool IsAtHouse { get; private set; }

    public void ClearBlockingPlant()
    {
        blockingPlant = null;
    }

    // ──────────────────────────────────────────────────────────
    // Public API (used by scene builders / spawner)
    // ──────────────────────────────────────────────────────────
    public void ConfigurePatrol(Animator targetAnimator, Transform pointA, Transform pointB)
    {
        animator = targetAnimator;
        patrolPointA = pointA;
        patrolPointB = pointB;
        moveMode = MoveMode.Patrol;
    }

    public void ConfigureLane(Animator targetAnimator, Vector3 direction, float baseBoundary)
    {
        animator = targetAnimator;
        advanceDirection = direction.normalized;
        baseBoundaryX = baseBoundary;
        moveMode = MoveMode.Lane;
    }

    public void AssignLane(float laneZ, float entryX = 6.2f)
    {
        assignedLaneZ = laneZ;
        laneEntryX = entryX;
    }

    // ──────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (moveMode == MoveMode.Patrol)
            currentTarget = patrolPointB != null ? patrolPointB : patrolPointA;

        SetAnimationSpeed(0f);
    }

    private void Update()
    {
        if (moveMode == MoveMode.Patrol)
            UpdatePatrol();
        else
            UpdateLane();
    }

    // ──────────────────────────────────────────────────────────
    // Patrol mode (original ZombiePrototype behaviour)
    // ──────────────────────────────────────────────────────────
    private void UpdatePatrol()
    {
        if (currentTarget == null) { SetAnimationSpeed(0f); return; }

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            SetAnimationSpeed(0f);
            return;
        }

        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            transform.position = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
            currentTarget = currentTarget == patrolPointA ? patrolPointB : patrolPointA;
            pauseTimer = pauseDuration;
            SetAnimationSpeed(0f);
            return;
        }

        MoveToward(toTarget.normalized);
    }

    // ──────────────────────────────────────────────────────────
    // Lane mode (used in integration scene)
    // ──────────────────────────────────────────────────────────
    private void UpdateLane()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            SetAnimationSpeed(0f);
            return;
        }

        // If blocked by a plant that's still alive → stop and let ZombieAttack handle it
        if (blockingPlant != null)
        {
            if (blockingPlant == null || !blockingPlant.gameObject.activeInHierarchy || blockingPlant.currentHealth <= 0)
            {
                blockingPlant = null; // plant is dead, resume walking
            }
            else
            {
                SetAnimationSpeed(0f);
                return;
            }
        }

        // Check for plant obstacle ahead
        Vector3 sensorCenter = transform.position + Vector3.up * 0.78f + advanceDirection * detectionOffset;
        Collider[] hits = Physics.OverlapSphere(sensorCenter, detectionRadius, plantLayerMask);
        foreach (var hit in hits)
        {
            // Only physical (non-trigger) colliders count as blockers
            if (hit.isTrigger) continue;
            PlantBase plant = hit.GetComponentInParent<PlantBase>();
            if (plant != null)
            {
                blockingPlant = plant;
                SetAnimationSpeed(0f);
                return;
            }
        }

        // Reached the house attack line? Stop here; ZombieAttack damages HouseHealth.
        if (transform.position.x <= baseBoundaryX)
        {
            SetAnimationSpeed(0f);
            IsAtHouse = true;
            return;
        }

        IsAtHouse = false;

        // Zombies can enter from slightly different directions, then lock to a straight lane.
        Vector3 moveDirection = advanceDirection;
        if (transform.position.x > laneEntryX && Mathf.Abs(transform.position.z - assignedLaneZ) > 0.03f)
        {
            Vector3 laneEntry = new Vector3(laneEntryX, transform.position.y, assignedLaneZ);
            moveDirection = (laneEntry - transform.position).normalized;
        }
        else
        {
            Vector3 snapped = transform.position;
            snapped.z = Mathf.MoveTowards(snapped.z, assignedLaneZ, moveSpeed * 1.5f * Time.deltaTime);
            transform.position = snapped;
        }

        MoveToward(moveDirection);
    }

    private void MoveToward(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) { SetAnimationSpeed(0f); return; }

        Vector3 facingDir = visualFacesBackward ? -direction : direction;
        Quaternion targetRot = Quaternion.LookRotation(facingDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);

        float alignment = Vector3.Dot(transform.forward, facingDir);
        if (alignment > 0.75f)
            transform.position += direction * moveSpeed * Time.deltaTime;

        SetAnimationSpeed(1f);
    }

    private void SetAnimationSpeed(float value)
    {
        if (animator != null)
            animator.SetFloat(MoveSpeedHash, value, animationDampTime, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector3 sensorCenter = transform.position + Vector3.up * 0.78f + advanceDirection * detectionOffset;
        Gizmos.DrawWireSphere(sensorCenter, detectionRadius);
    }
}
