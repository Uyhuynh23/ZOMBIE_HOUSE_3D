using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hybrid AI controller — Plants vs. Zombies style.
///
/// STATE MACHINE
/// ─────────────────────────────────────────────────────────────
///  OutsideFence  : NavMesh moves freely toward the house/fence.
///                  Agent is active. Rotation uses desiredVelocity.
///
///  EnteringLane  : NavMesh disabled. Zombie walks manually to LanePath.laneEntry.
///                  Rotation faces manual movement direction.
///
///  MovingInLane  : Manual straight-line walk along the lane column.
///                  Direction = (laneEnd - laneEntry).normalized — locked forever.
///                  Plant detection active.
///
///  AttackingPlant: Stopped, ZombieAttack deals damage.
///                  On plant death → resume MovingInLane toward the SAME laneEnd.
///
///  AttackingHouse: Arrived at laneEnd (near house). ZombieAttack attacks house.
///
///  Dead          : Everything stopped.
///
/// ROTATION
/// ─────────────────────────────────────────────────────────────
///  Root (this.transform): always faces movement direction (smooth RotateTowards).
///  Visual child         : visualYawOffset applied ONCE in Awake, never touched again.
///                         Default 0° = model art already faces +Z.
///                         Use 180° for models whose art faces -Z.
///                         Use 90 / -90 for models whose art faces ±X.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavAgent : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────────────────
    [Header("Target")]
    [Tooltip("Baker_house transform. Auto-found by tag 'HouseTarget' or name 'Baker_house'.")]
    public Transform houseTarget;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] public float moveSpeed = 1.5f;
    [Tooltip("Root rotation speed (degrees/s). Applied in all states.")]
    [SerializeField, Min(10f)]  private float angularSpeed = 480f;
    [Tooltip("NavMeshAgent stopping distance (outside fence only).")]
    [SerializeField, Min(0.1f)] private float navStoppingDistance = 1.2f;
    [Tooltip("Flat distance from houseTarget to consider arrived (used in MovingInLane).")]
    [SerializeField, Min(0.5f)] private float houseArrivalRadius = 4f;
    [Tooltip("Flat distance from LanePath.laneEntry to start the lane (EnteringLane → MovingInLane).")]
    [SerializeField, Min(0.1f)] private float laneEntryRadius = 0.6f;

    [Header("Stuck Recovery (OutsideFence only)")]
    [SerializeField, Min(0.1f)] private float stuckDistanceThreshold = 0.3f;
    [SerializeField, Min(1f)]   private float stuckCheckInterval = 2.5f;

    [Header("Plant Detection")]
    [SerializeField, Min(0.1f)] private float plantDetectRadius = 0.8f;
    [SerializeField, Min(0f)]   private float plantDetectOffset = 1.1f;
    [SerializeField]            private LayerMask plantLayerMask = ~0;

    [Header("Hit Stagger")]
    [SerializeField, Range(0f, 1f)] private float hitSpeedMultiplier = 0.15f;
    [SerializeField, Min(0f)]       private float hitStaggerDuration = 0.4f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float animDampTime = 0.1f;
    [SerializeField] private string moveSpeedParam = "MoveSpeed";

    [Header("Visual Rotation")]
    [Tooltip("Child Transform that holds the 3-D mesh/animator. Auto-found if null.\n" +
             "Its localRotation is ONLY set once in Awake (visualYawOffset), never touched again at runtime.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Y-axis rotation baked onto the visual child to correct model import orientation.\n" +
             "  0°  = model art faces +Z  (default, try this first)\n" +
             "180°  = model art faces -Z\n" +
             " 90°  = model art faces -X\n" +
             "-90°  = model art faces +X")]
    [SerializeField] private float visualYawOffset = 0f;

    // ──────────────────────────────────────────────────────────
    // Public state — read by ZombieAttack, LaneEntrance
    // ──────────────────────────────────────────────────────────
    public PlantBase BlockingPlant   { get; private set; }
    public bool      IsAtHouse       { get; private set; }
    public bool      IsDead          { get; private set; }
    public bool      HasLaneAssigned { get; private set; }

    // ──────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────
    private enum AIState
    {
        OutsideFence,    // NavMesh free movement
        EnteringLane,    // Manual walk to laneEntry point
        MovingInLane,    // Manual straight walk laneEntry→laneEnd
        AttackingPlant,
        AttackingHouse,
        Dead
    }
    private AIState state = AIState.OutsideFence;

    // Lane
    private LanePath  assignedLane;
    private Vector3   laneDir;           // normalised direction laneEntry→laneEnd, locked at assignment
    private Vector3   manualMoveDir;     // current manual movement direction (for rotation)

    // Components
    private NavMeshAgent agent;
    private int   moveSpeedHash;
    private float baseSpeed;
    private bool  isStaggered;

    // Stuck detection (OutsideFence)
    private Vector3 lastStuckCheckPos;
    private float   stuckCheckTimer;

    // ──────────────────────────────────────────────────────────
    // Public API — called by LaneEntrance trigger
    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// Called by LaneEntrance the moment a zombie crosses into the fence.
    /// Disables NavMesh and begins manual lane movement.
    /// </summary>
    public void AssignLane(LanePath lane)
    {
        if (HasLaneAssigned || lane == null || !lane.IsValid) return;

        assignedLane    = lane;
        HasLaneAssigned = true;

        // Lock lane direction now — never recalculated
        Vector3 delta = lane.laneEnd.position - lane.laneEntry.position;
        delta.y = 0f;
        laneDir = delta.normalized;

        // Stop and disable NavMeshAgent — inside fence is scripted movement only
        if (agent != null && agent.isActiveAndEnabled)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        state = AIState.EnteringLane;
        Debug.Log($"[EnemyNavAgent] {name}: assigned lane '{lane.name}', entering lane.");
    }

    /// <summary>Called by ZombieAttack (via ClearBlockingPlant) when attacking plant dies.</summary>
    public void ClearBlockingPlant()
    {
        BlockingPlant = null;
        // Resume movement along the same lane — never call NavMesh again
        if (!IsDead && state == AIState.AttackingPlant)
            state = AIState.MovingInLane;
    }

    // ──────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Disable conflicting legacy mover
        var legacyMover = GetComponent<ZombiePrototypeMover>();
        if (legacyMover != null) legacyMover.enabled = false;

        // Find visual child (owns the mesh)
        if (visualRoot == null)
        {
            Transform found = transform.Find("Zombie Visual");
            if (found == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<RectTransform>() == null &&
                        child.GetComponent<Canvas>()        == null)
                    { found = child; break; }
                }
            }
            visualRoot = found;
        }

        // The visualYawOffset is now applied dynamically in RotateRoot()
        // instead of touching visualRoot.localRotation, because Animator overwrites it.

        moveSpeedHash = Animator.StringToHash(moveSpeedParam);
        baseSpeed     = moveSpeed;
    }

    private void Start()
    {
        if (houseTarget == null)
        {
            GameObject h = GameObject.FindWithTag("HouseTarget");
            if (h == null) h = GameObject.Find("Baker_house");
            if (h != null) houseTarget = h.transform;
            else Debug.LogWarning($"[EnemyNavAgent] {name}: No house target found!");
        }

        ConfigureAgent();

        // Start walking toward the fence / house
        if (AgentValid())
            agent.SetDestination(houseTarget != null ? houseTarget.position : transform.position);

        lastStuckCheckPos = transform.position;
        stuckCheckTimer   = stuckCheckInterval;
    }

    private void Update()
    {
        if (IsDead) return;

        bool paused = GameManager.Instance != null &&
                      GameManager.Instance.CurrentState != GameManager.GameState.Playing;
        if (paused)
        {
            if (AgentValid()) agent.isStopped = true;
            SetAnimSpeed(0f);
            return;
        }

        switch (state)
        {
            case AIState.OutsideFence:   UpdateOutsideFence();   break;
            case AIState.EnteringLane:   UpdateEnteringLane();   break;
            case AIState.MovingInLane:   UpdateMovingInLane();   break;
            case AIState.AttackingPlant: UpdateAttackingPlant(); break;
            case AIState.AttackingHouse: UpdateAttackingHouse(); break;
        }
    }

    // ──────────────────────────────────────────────────────────
    // State: OutsideFence  (NavMesh free)
    // ──────────────────────────────────────────────────────────
    private void UpdateOutsideFence()
    {
        if (!AgentValid()) return;

        agent.isStopped = false;

        // Rotate root using NavMesh desired velocity
        Vector3 desired = agent.desiredVelocity;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.01f) { desired = agent.velocity; desired.y = 0f; }
        RotateRoot(desired.normalized);

        // Sync anim
        SetAnimSpeed(agent.velocity.magnitude / Mathf.Max(0.01f, baseSpeed));

        // Stuck detection
        stuckCheckTimer -= Time.deltaTime;
        if (stuckCheckTimer <= 0f)
        {
            stuckCheckTimer = stuckCheckInterval;
            if (Vector3.Distance(transform.position, lastStuckCheckPos) < stuckDistanceThreshold)
            {
                if (houseTarget != null) agent.SetDestination(houseTarget.position);
            }
            lastStuckCheckPos = transform.position;
        }

        // Path recovery
        if (!agent.pathPending && agent.path.status == NavMeshPathStatus.PathInvalid)
            if (houseTarget != null) agent.SetDestination(houseTarget.position);
    }

    // ──────────────────────────────────────────────────────────
    // State: EnteringLane  (walk manually to laneEntry point)
    // ──────────────────────────────────────────────────────────
    private void UpdateEnteringLane()
    {
        Vector3 target = assignedLane.laneEntry.position;
        target.y = transform.position.y;

        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= laneEntryRadius)
        {
            // Snap exactly onto entry X/Z, keep Y
            Vector3 snapped = transform.position;
            snapped.x = assignedLane.laneEntry.position.x;
            snapped.z = assignedLane.laneEntry.position.z;
            transform.position = snapped;

            manualMoveDir = laneDir;
            state = AIState.MovingInLane;
            return;
        }

        manualMoveDir = toTarget.normalized;
        MoveManually(manualMoveDir);
    }

    // ──────────────────────────────────────────────────────────
    // State: MovingInLane  (straight scripted walk)
    // ──────────────────────────────────────────────────────────
    private void UpdateMovingInLane()
    {
        manualMoveDir = laneDir;

        // Plant detection ahead along lane direction
        if (DetectPlantAhead(laneDir))
        {
            state = AIState.AttackingPlant;
            SetAnimSpeed(0f);
            return;
        }

        // Arrival at house (physically touching the house collider)
        if (DetectHouseAhead(laneDir))
        {
            IsAtHouse = true;
            state     = AIState.AttackingHouse;
            SetAnimSpeed(0f);
            return;
        }

        MoveManually(laneDir);
    }

    // ──────────────────────────────────────────────────────────
    // State: AttackingPlant
    // ──────────────────────────────────────────────────────────
    private void UpdateAttackingPlant()
    {
        // ZombieAttack calls ClearBlockingPlant() → state becomes MovingInLane
        // We just keep still and face the plant
        if (BlockingPlant != null)
        {
            Vector3 toPlant = BlockingPlant.transform.position - transform.position;
            toPlant.y = 0f;
            RotateRoot(toPlant.normalized);
        }
        SetAnimSpeed(0f);
    }

    // ──────────────────────────────────────────────────────────
    // State: AttackingHouse
    // ──────────────────────────────────────────────────────────
    private void UpdateAttackingHouse()
    {
        IsAtHouse = true;
        SetAnimSpeed(0f);
    }

    // ──────────────────────────────────────────────────────────
    // Manual movement helpers
    // ──────────────────────────────────────────────────────────
    private void MoveManually(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        dir.y = 0f;
        dir.Normalize();

        float speed = isStaggered ? baseSpeed * hitSpeedMultiplier : baseSpeed;
        transform.position += dir * speed * Time.deltaTime;

        RotateRoot(dir);
        SetAnimSpeed(speed / Mathf.Max(0.01f, baseSpeed));
    }

    private void RotateRoot(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
        // Apply visualYawOffset to the root's target rotation.
        // This makes the root face differently, allowing the backwards-facing visual child 
        // to face the movement direction, without touching localRotation (which Animator overrides).
        Quaternion target = lookRot * Quaternion.Euler(0f, visualYawOffset, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, angularSpeed * Time.deltaTime);
    }

    // ──────────────────────────────────────────────────────────
    // Plant detection
    // ──────────────────────────────────────────────────────────
    private bool DetectPlantAhead(Vector3 dir)
    {
        Vector3 sensorPos = transform.position + Vector3.up * 0.8f + dir * plantDetectOffset;
        Collider[] hits = Physics.OverlapSphere(sensorPos, plantDetectRadius, plantLayerMask);
        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;
            PlantBase plant = hit.GetComponentInParent<PlantBase>();
            if (plant != null && plant.currentHealth > 0)
            {
                BlockingPlant = plant;
                return true;
            }
        }
        return false;
    }

    // ──────────────────────────────────────────────────────────
    // Arrival check (House collision)
    // ──────────────────────────────────────────────────────────
    private bool DetectHouseAhead(Vector3 dir)
    {
        Vector3 sensorPos = transform.position + Vector3.up * 0.8f + dir * plantDetectOffset;
        Collider[] hits = Physics.OverlapSphere(sensorPos, plantDetectRadius); // Check all layers for the house
        foreach (var hit in hits)
        {
            if (hit.CompareTag("HouseTarget") || hit.name == "Baker_house")
            {
                return true;
            }
        }
        return false;
    }

    // ──────────────────────────────────────────────────────────
    // Hit stagger (works in any state)
    // ──────────────────────────────────────────────────────────
    public void TriggerHitStagger()
    {
        if (IsDead || isStaggered) return;
        StartCoroutine(HitStaggerRoutine());
    }

    private IEnumerator HitStaggerRoutine()
    {
        isStaggered = true;
        if (AgentValid()) agent.speed = baseSpeed * hitSpeedMultiplier;

        yield return new WaitForSeconds(hitStaggerDuration);

        isStaggered = false;
        if (!IsDead && AgentValid()) agent.speed = baseSpeed;
    }

    // ──────────────────────────────────────────────────────────
    // Death
    // ──────────────────────────────────────────────────────────
    public void OnDeath()
    {
        IsDead = true;
        state  = AIState.Dead;
        if (AgentValid()) { agent.isStopped = true; agent.enabled = false; }
        SetAnimSpeed(0f);
        StopAllCoroutines();
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────
    private void ConfigureAgent()
    {
        if (!AgentValid()) return;
        agent.speed             = moveSpeed;
        agent.angularSpeed      = angularSpeed;
        agent.acceleration      = 8f;
        agent.stoppingDistance  = navStoppingDistance;
        agent.autoBraking       = false;
        agent.updateRotation    = false;   // root rotated manually in all states
        agent.updatePosition    = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);
        baseSpeed = moveSpeed;
    }

    private bool AgentValid() =>
        agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private void SetAnimSpeed(float value)
    {
        if (animator != null)
            animator.SetFloat(moveSpeedHash, value, animDampTime, Time.deltaTime);
    }

    // ──────────────────────────────────────────────────────────
    // Gizmos
    // ──────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (houseTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, houseTarget.position);
        }
        if (assignedLane != null && assignedLane.IsValid)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, assignedLane.laneEntry.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(assignedLane.laneEntry.position, assignedLane.laneEnd.position);
        }
        // Plant sensor
        Vector3 fwd = HasLaneAssigned ? laneDir : transform.forward;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f + fwd * plantDetectOffset, plantDetectRadius);
    }
}
