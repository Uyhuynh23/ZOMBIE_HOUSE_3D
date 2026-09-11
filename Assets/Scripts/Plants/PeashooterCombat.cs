using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(SphereCollider), typeof(CapsuleCollider))]
public class PeashooterCombat : PlantBase
{
    [Header("Combat Settings")]
    public float fireRate = 1f;
    public float projectileSpeed = 10f;
    public float aggroRadius = 5f;
    public float forwardConeThreshold = 0.3f; // dot product threshold (~72 degree cone)
    [Tooltip("Maximum lateral distance from the aim axis for a zombie to be considered in this lane.")]
    public float maxLaneDistance = 1.5f;
    [Tooltip("Minimum forward distance along the aim axis. Enemies behind this distance are ignored.")]
    public float minForwardDistance = 0.1f;
    [Tooltip("When true, the plant fires strictly straight ahead along its aim direction.")]
    public bool shootDirectlyOnly = true;
    [Tooltip("Fallback muzzle axis when a SpawnPoint is unavailable. The imported peashooter models use local +Z.")]
    public Vector3 localAimAxis = Vector3.forward;

    [Header("Body Collider Settings")]
    public float bodyHeight = 1.0f;
    public float bodyRadius = 0.35f;
    public Vector3 bodyCenter = new Vector3(0f, 0.5f, 0f);

    [Header("References")]
    public Transform spawnPoint;

    private SphereCollider aggroCollider;
    private CapsuleCollider bodyCollider;
    private HashSet<GameObject> zombiesInRange = new HashSet<GameObject>();
    private float fireTimer = 0f;
    private Animator animator;
    private GameObject currentTarget;
    private Vector3 lockedLaneDirection;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>Rotates the plant so its configured local firing axis faces a world-space lane direction.</summary>
    public void SetAimDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.001f) return;

        lockedLaneDirection = worldDirection.normalized;
        ApplyLockedLaneRotation();
    }

    /// <summary>
    /// Returns the authoritative world-space aim direction (flattened to the horizontal plane).
    /// Defaults to the square's forward (local +Z) or the plant's forward.
    /// </summary>
    public Vector3 GetAimDirection()
    {
        if (lockedLaneDirection.sqrMagnitude > 0.001f)
            return lockedLaneDirection;

        if (mySquare != null)
        {
            Vector3 sqForward = mySquare.transform.forward;
            sqForward.y = 0f;
            if (sqForward.sqrMagnitude > 0.001f)
                return sqForward.normalized;
        }

        Vector3 currentWorldAim = GetCurrentAimDirection();
        return currentWorldAim.sqrMagnitude > 0.001f ? currentWorldAim.normalized : Vector3.forward;
    }

    private void LateUpdate()
    {
        // Some imported animation clips can write to the root transform.
        // Re-apply the lane direction after animation evaluation so plants stay
        // pointed at the incoming-enemy side of their own lane.
        if (lockedLaneDirection.sqrMagnitude > 0.001f)
            ApplyLockedLaneRotation();
    }

    private void ApplyLockedLaneRotation()
    {
        if (lockedLaneDirection.sqrMagnitude < 0.001f) return;

        Vector3 currentWorldAim = GetCurrentAimDirection();
        if (currentWorldAim.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.FromToRotation(currentWorldAim.normalized, lockedLaneDirection)
                             * transform.rotation;
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        // Default aim direction from the plantable square's Z direction if available
        if (lockedLaneDirection.sqrMagnitude < 0.001f)
        {
            Vector3 initialAim = mySquare != null ? mySquare.transform.forward : transform.forward;
            initialAim.y = 0f;
            if (initialAim.sqrMagnitude > 0.001f)
            {
                SetAimDirection(initialAim);
            }
        }

        // Aggro trigger (SphereCollider)
        aggroCollider = GetComponent<SphereCollider>();
        aggroCollider.isTrigger = true;
        aggroCollider.radius = aggroRadius;

        // Physical body (CapsuleCollider) blocks character movement
        bodyCollider = GetComponent<CapsuleCollider>();
        bodyCollider.isTrigger = false;
        bodyCollider.height = bodyHeight;
        bodyCollider.radius = bodyRadius;
        bodyCollider.center = bodyCenter;
        bodyCollider.direction = 1; // Y-axis (upright)
        // The Peashooter prefab keeps this collider disabled in the asset so
        // it can be configured at runtime. Enemy plant detection intentionally
        // ignores triggers, so leaving it disabled makes enemies walk through
        // Peashooters without ever acquiring a target.
        bodyCollider.enabled = true;

        if (spawnPoint == null)
        {
            Transform sp = transform.Find("SpawnPoint");
            if (sp != null)
            {
                spawnPoint = sp;
            }
            else
            {
                Debug.LogError("PeashooterCombat: SpawnPoint not found!");
            }
        }
    }

    void Update()
    {
        if (!NetworkGameplayAuthority.CanMutate) return;
        // Clean up destroyed/dead enemies. Taking damage does not disable this
        // combat component, so a living plant keeps firing while being eaten.
        zombiesInRange.RemoveWhere(z =>
        {
            if (z == null || !z.activeInHierarchy) return true;
            ZombieHealth health = z.GetComponent<ZombieHealth>();
            return health == null || health.currentHealth <= 0;
        });

        Vector3 aimDir = GetAimDirection();
        Vector3 aimSide = Vector3.Cross(Vector3.up, aimDir).normalized;

        currentTarget = null;
        float closestForwardDistance = float.MaxValue;

        foreach (var z in zombiesInRange)
        {
            if (z == null) continue;
            Vector3 toZombie = z.transform.position - transform.position;
            toZombie.y = 0f;

            // 1. Distance along forward aim axis
            float forwardDist = Vector3.Dot(toZombie, aimDir);

            // Never shoot zombies behind or directly beside the plant
            if (forwardDist <= minForwardDistance)
                continue;

            // 2. Lateral lane offset (perpendicular to aim axis)
            float lateralDist = Mathf.Abs(Vector3.Dot(toZombie, aimSide));

            // Must be within this plant's direct lane corridor (ignores adjacent lanes)
            if (lateralDist > maxLaneDistance)
                continue;

            // 3. Forward cone threshold
            if (toZombie.sqrMagnitude > 0.001f &&
                Vector3.Dot(toZombie.normalized, aimDir) < forwardConeThreshold)
                continue;

            // Select closest enemy in front in the lane
            if (forwardDist < closestForwardDistance)
            {
                closestForwardDistance = forwardDist;
                currentTarget = z;
            }
        }

        if (currentTarget != null)
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                FireProjectile();
                fireTimer = 1f / fireRate;
            }
        }
        else
        {
            fireTimer = 0f; // Ready to fire immediately when a zombie enters
        }
    }

    void FireProjectile()
    {
        if (ObjectPoolManager.Instance == null || spawnPoint == null) return;

        bool networked = NetworkBootstrap.IsNetworkSession;
        GameObject pea = networked
            ? Instantiate(ObjectPoolManager.Instance.peaPrefab)
            : ObjectPoolManager.Instance.GetPea();
        pea.transform.position = spawnPoint.position;

        Vector3 aimDir = GetAimDirection();
        Vector3 fireDirection = aimDir;

        if (!shootDirectlyOnly && currentTarget != null)
        {
            Vector3 toTarget = currentTarget.transform.position + Vector3.up * 0.9f - spawnPoint.position;
            if (Vector3.Dot(toTarget.normalized, aimDir) > 0f)
            {
                fireDirection = toTarget.normalized;
            }
        }

        pea.transform.rotation = Quaternion.LookRotation(fireDirection);

        Rigidbody rb = pea.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = fireDirection * projectileSpeed;
        }

        PeaProjectile pp = pea.GetComponent<PeaProjectile>();
        if (pp == null)
        {
            pp = pea.AddComponent<PeaProjectile>();
        }
        pp.Initialize(gameObject);

        if (networked)
        {
            NetworkObject no = pea.GetComponent<NetworkObject>();
            if (no == null)
            {
                Debug.LogError("[NET][PLANT] Pea projectile is missing NetworkObject.");
                Destroy(pea);
                return;
            }
            no.Spawn(true);
            Debug.Log($"[NET][PLANT] Projectile spawned id={no.NetworkObjectId} ownerPlant={NetworkObjectId}.");
        }

        AudioManager.PlaySfx(AudioCue.PeashooterShot);

        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }

    // SpawnPoint is the authoritative visual firing direction. It also works
    // before Start caches the reference, when a just-planted prefab is first
    // aligned to its lane by PlayerController.
    private Vector3 GetCurrentAimDirection()
    {
        Transform point = spawnPoint != null ? spawnPoint : transform.Find("SpawnPoint");
        Vector3 direction = point != null
            ? point.position - transform.position
            : transform.TransformDirection(localAimAxis);

        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!NetworkGameplayAuthority.IsServer) return;
        ZombieHealth health = other != null ? other.GetComponentInParent<ZombieHealth>() : null;
        if (health != null && health.currentHealth > 0)
        {
            // Use the health owner rather than collider tags. Imported enemies
            // may expose untagged child colliders (notably Spider variants).
            zombiesInRange.Add(health.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!NetworkGameplayAuthority.IsServer) return;
        ZombieHealth health = other != null ? other.GetComponentInParent<ZombieHealth>() : null;
        if (health != null) zombiesInRange.Remove(health.gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 aimDir = Application.isPlaying ? GetAimDirection() : transform.forward;
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector3.forward;
        aimDir.Normalize();

        Vector3 aimSide = Vector3.Cross(Vector3.up, aimDir).normalized;
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        // Draw direct aim line
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, aimDir * aggroRadius);

        // Draw lane corridor boundaries
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Vector3 leftBound = origin - aimSide * maxLaneDistance;
        Vector3 rightBound = origin + aimSide * maxLaneDistance;
        Gizmos.DrawRay(leftBound, aimDir * aggroRadius);
        Gizmos.DrawRay(rightBound, aimDir * aggroRadius);
        Gizmos.DrawLine(leftBound, rightBound);
        Gizmos.DrawLine(leftBound + aimDir * aggroRadius, rightBound + aimDir * aggroRadius);

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, currentTarget.transform.position + Vector3.up * 0.5f);
        }
    }
#endif
}
