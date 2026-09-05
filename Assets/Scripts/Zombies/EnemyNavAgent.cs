using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh-based AI controller for all enemy types (Zombie, Spider).
/// Replaces ZombiePrototypeMover in Map_Cloudy.
/// States: Moving -> AttackingPlant -> AttackingHouse -> Dead
///
/// Lane system: Once an enemy crosses a LaneEntrance trigger inside the fence,
/// it is constrained to move along a narrow corridor toward the house.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavAgent : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────────────────
    [Header("Target")]
    [Tooltip("The house GameObject that enemies march toward. Auto-found by tag 'HouseTarget' if null.")]
    public Transform houseTarget;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] public float moveSpeed = 1.5f;
    [SerializeField, Min(10f)]  private float angularSpeed = 360f;
    [Tooltip("Distance at which the agent stops walking (used as NavMeshAgent.stoppingDistance).")]
    [SerializeField, Min(0.1f)] private float stoppingDistance = 1.2f;
    [Tooltip("Max distance from house center to consider 'at the house'. House extents ~9.5m.")]
    [SerializeField, Min(1f)]   private float houseArrivalRadius = 9f;

    [Header("Lane Constraint")]
    [Tooltip("How strongly the enemy is snapped to its lane. Higher = tighter constraint.")]
    [SerializeField, Min(0f)] private float laneSnapStrength = 20f;
    [Tooltip("How wide the lane corridor is (half-width). Enemy stays within this tolerance of its lane coord.")]
    [SerializeField, Min(0.1f)] private float laneTolerance = 0.3f;


    [Header("Stuck Recovery")]
    [Tooltip("If the enemy hasn't moved more than this in stuckCheckInterval seconds, recalculate path.")]
    [SerializeField, Min(0.1f)] private float stuckDistanceThreshold = 0.3f;
    [Tooltip("How often (seconds) to check if the enemy is stuck.")]
    [SerializeField, Min(1f)]   private float stuckCheckInterval = 2.5f;

    [Header("Plant Detection")]
    [Tooltip("Radius of sphere cast ahead of the enemy to detect plants.")]
    [SerializeField, Min(0.1f)] private float plantDetectRadius = 0.8f;
    [Tooltip("Distance ahead of the agent to probe for plants.")]
    [SerializeField, Min(0f)]   private float plantDetectOffset = 1.1f;
    [Tooltip("Layer mask for plant detection. Leave as 'Everything' if plants have no specific layer.")]
    [SerializeField]            private LayerMask plantLayerMask = ~0;

    [Header("Hit Stagger")]
    [Tooltip("Speed multiplier when hit. 0 = full stop.")]
    [SerializeField, Range(0f, 1f)] private float hitSpeedMultiplier = 0.15f;
    [Tooltip("Duration of the hit stagger in seconds.")]
    [SerializeField, Min(0f)]       private float hitStaggerDuration = 0.4f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float animDampTime = 0.1f;
    [Tooltip("Animator float parameter name for the walk cycle.")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";

    // ──────────────────────────────────────────────────────────
    // Runtime state — public for ZombieAttack / LaneEntrance
    // ──────────────────────────────────────────────────────────
    public PlantBase BlockingPlant  { get; private set; }
    public bool      IsAtHouse      { get; private set; }
    public bool      IsDead         { get; private set; }
    public bool      HasLaneAssigned { get; private set; }

    // Lane data (set by LaneEntrance trigger)
    private LaneEntrance.Direction laneDirection;
    private int   assignedLaneIndex;
    private float laneConstraintCoord;   // X for N/S lanes, Z for E/W lanes

    // ──────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────
    private NavMeshAgent agent;
    private int   moveSpeedHash;
    private float baseSpeed;
    private bool  isStaggered;

    // Stuck detection
    private Vector3 lastStuckCheckPos;
    private float   stuckCheckTimer;

    private enum AIState { Moving, AttackingPlant, AttackingHouse, Dead }
    private AIState state = AIState.Moving;

    // ──────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────
    /// <summary>Called by LaneEntrance trigger when enemy crosses into defense zone.</summary>
    public void AssignLane(LaneEntrance.Direction dir, int laneIdx, float worldCoord)
    {
        if (HasLaneAssigned) return;
        laneDirection       = dir;
        assignedLaneIndex   = laneIdx;
        laneConstraintCoord = worldCoord;
        HasLaneAssigned     = true;

        // Recalculate destination with lane-constrained target
        SetDestinationToHouse();
    }

    public void ClearBlockingPlant()
    {
        BlockingPlant = null;
        if (!IsDead && AgentValid())
        {
            agent.isStopped = false;
            SetDestinationToHouse();
            state = AIState.Moving;
        }
    }

    // ──────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        moveSpeedHash = Animator.StringToHash(moveSpeedParam);
        baseSpeed = moveSpeed;
    }

    private void Start()
    {
        // Auto-find house
        if (houseTarget == null)
        {
            GameObject h = GameObject.FindWithTag("HouseTarget");
            if (h == null) h = GameObject.Find("Baker_house");
            if (h != null) houseTarget = h.transform;
            else Debug.LogWarning($"[EnemyNavAgent] {name}: No house target found!");
        }

        ConfigureAgent();
        SetDestinationToHouse();

        lastStuckCheckPos = transform.position;
        stuckCheckTimer   = stuckCheckInterval;
    }

    private void Update()
    {
        if (IsDead) return;

        // Pause during non-play game states
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            if (AgentValid()) agent.isStopped = true;
            SetAnimSpeed(0f);
            return;
        }

        switch (state)
        {
            case AIState.Moving:         UpdateMoving();         break;
            case AIState.AttackingPlant: UpdateAttackingPlant(); break;
            case AIState.AttackingHouse: UpdateAttackingHouse(); break;
        }

        // Sync animation to actual NavMesh velocity
        float vel = AgentValid() ? agent.velocity.magnitude : 0f;
        SetAnimSpeed(vel / Mathf.Max(0.01f, baseSpeed));
    }

    // ──────────────────────────────────────────────────────────
    // State: Moving
    // ──────────────────────────────────────────────────────────
    private void UpdateMoving()
    {
        if (!AgentValid()) return;

        // ── Lane constraint: snap perpendicular position to lane ──
        if (HasLaneAssigned)
            ApplyLaneConstraint();

        // ── Plant detection in forward arc ──
        Vector3 fwd = agent.velocity.sqrMagnitude > 0.01f
            ? agent.velocity.normalized
            : transform.forward;

        Vector3 sensorPos = transform.position + Vector3.up * 0.8f + fwd * plantDetectOffset;
        Collider[] hits = Physics.OverlapSphere(sensorPos, plantDetectRadius, plantLayerMask);
        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;
            PlantBase plant = hit.GetComponentInParent<PlantBase>();
            if (plant != null && plant.currentHealth > 0)
            {
                BlockingPlant = plant;
                agent.isStopped = true;
                state = AIState.AttackingPlant;
                return;
            }
        }

        // ── Arrival detection ──
        if (CheckArrivedAtHouse())
        {
            IsAtHouse = true;
            agent.isStopped = true;
            state = AIState.AttackingHouse;
            return;
        }

        // Keep moving
        IsAtHouse = false;
        agent.isStopped = false;

        // ── Path recovery ──
        if (!agent.pathPending && agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            SetDestinationToHouse();
        }

        // ── Stuck detection ──
        stuckCheckTimer -= Time.deltaTime;
        if (stuckCheckTimer <= 0f)
        {
            stuckCheckTimer = stuckCheckInterval;
            float moved = Vector3.Distance(transform.position, lastStuckCheckPos);
            if (moved < stuckDistanceThreshold && state == AIState.Moving)
            {
                Debug.Log($"[EnemyNavAgent] {name} appears stuck. Recalculating path.");
                SetDestinationToHouse();
            }
            lastStuckCheckPos = transform.position;
        }
    }

    /// <summary>
    /// Smoothly snaps the agent to its assigned lane coordinate on the
    /// perpendicular axis (X for N/S, Z for E/W). Uses warp if deviation is large.
    /// </summary>
    private void ApplyLaneConstraint()
    {
        Vector3 pos = transform.position;
        bool isNorthSouth = laneDirection == LaneEntrance.Direction.North ||
                            laneDirection == LaneEntrance.Direction.South;

        float currentCoord = isNorthSouth ? pos.x : pos.z;
        float delta = laneConstraintCoord - currentCoord;

        if (Mathf.Abs(delta) > laneTolerance)
        {
            float corrected = Mathf.MoveTowards(currentCoord, laneConstraintCoord, laneSnapStrength * Time.deltaTime);
            Vector3 newPos = pos;
            if (isNorthSouth) newPos.x = corrected;
            else              newPos.z = corrected;

            // Warp agent to corrected position (respects NavMesh)
            agent.Warp(newPos);
        }
    }

    private bool CheckArrivedAtHouse()
    {
        if (houseTarget == null) return false;

        float distToHouseCenter = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(houseTarget.position.x, 0f, houseTarget.position.z));

        return distToHouseCenter <= houseArrivalRadius;
    }

    // ──────────────────────────────────────────────────────────
    // State: AttackingPlant
    // ──────────────────────────────────────────────────────────
    private void UpdateAttackingPlant()
    {
        // Check if plant is gone
        if (BlockingPlant == null ||
            !BlockingPlant.gameObject.activeInHierarchy ||
            BlockingPlant.currentHealth <= 0)
        {
            BlockingPlant = null;
            IsAtHouse = false;
            if (AgentValid()) { agent.isStopped = false; SetDestinationToHouse(); }
            state = AIState.Moving;
            return;
        }

        if (AgentValid()) agent.isStopped = true;
        SetAnimSpeed(0f);
    }

    // ──────────────────────────────────────────────────────────
    // State: AttackingHouse
    // ──────────────────────────────────────────────────────────
    private void UpdateAttackingHouse()
    {
        IsAtHouse = true;
        if (AgentValid()) agent.isStopped = true;
        SetAnimSpeed(0f);
    }

    // ──────────────────────────────────────────────────────────
    // Hit stagger
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
        agent.stoppingDistance  = stoppingDistance;
        agent.autoBraking       = false;
        agent.updateRotation    = true;
        agent.updatePosition    = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);
        baseSpeed = moveSpeed;
    }

    private void SetDestinationToHouse()
    {
        if (houseTarget == null || !AgentValid()) return;

        // If lane is assigned, aim at a point on the house perimeter along the lane axis
        if (HasLaneAssigned)
        {
            Vector3 target = houseTarget.position;
            bool isNS = laneDirection == LaneEntrance.Direction.North ||
                        laneDirection == LaneEntrance.Direction.South;
            // Keep the lane coordinate on the perpendicular axis so the path stays in-lane
            if (isNS) target.x = laneConstraintCoord;
            else      target.z = laneConstraintCoord;
            agent.SetDestination(target);
        }
        else
        {
            agent.SetDestination(houseTarget.position);
        }
    }

    private bool AgentValid() =>
        agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private void SetAnimSpeed(float value)
    {
        if (animator != null)
            animator.SetFloat(moveSpeedHash, value, animDampTime, Time.deltaTime);
    }

    // ──────────────────────────────────────────────────────────
    // Scene Gizmos
    // ──────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (houseTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, houseTarget.position);
            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            Gizmos.DrawWireSphere(houseTarget.position, houseArrivalRadius);
        }

        // Lane constraint visualization
        if (HasLaneAssigned)
        {
            Gizmos.color = Color.cyan;
            bool isNS = laneDirection == LaneEntrance.Direction.North ||
                        laneDirection == LaneEntrance.Direction.South;
            Vector3 lanePos = transform.position;
            if (isNS) lanePos.x = laneConstraintCoord;
            else      lanePos.z = laneConstraintCoord;
            Gizmos.DrawLine(transform.position, lanePos);
        }

        // Plant sensor
        Vector3 fwd = transform.forward;
        if (Application.isPlaying && agent != null && agent.velocity.sqrMagnitude > 0.01f)
            fwd = agent.velocity.normalized;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(
            transform.position + Vector3.up * 0.8f + fwd * plantDetectOffset,
            plantDetectRadius);
    }
}
