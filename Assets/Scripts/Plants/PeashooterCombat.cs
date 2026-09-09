using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(CapsuleCollider))]
public class PeashooterCombat : PlantBase
{
    [Header("Combat Settings")]
    public float fireRate = 1f;
    public float projectileSpeed = 10f;
    public float aggroRadius = 5f;
    public float forwardConeThreshold = 0.3f; // dot product threshold (~72 degree cone)
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
        // Clean up destroyed/dead enemies. Taking damage does not disable this
        // combat component, so a living plant keeps firing while being eaten.
        zombiesInRange.RemoveWhere(z =>
        {
            if (z == null || !z.activeInHierarchy) return true;
            ZombieHealth health = z.GetComponent<ZombieHealth>();
            return health == null || health.currentHealth <= 0;
        });

        // Select the closest valid enemy anywhere inside the aggro trigger.
        // Projectiles already aim at the selected target, so restricting the
        // target to a narrow lane made plants stop shooting when enemies were
        // pushed or navigated slightly sideways.
        currentTarget = null;
        float closestDistance = float.MaxValue;
        foreach (var z in zombiesInRange)
        {
            if (z == null) continue;
            Vector3 toZombie = z.transform.position - transform.position;
            toZombie.y = 0f;
            if (toZombie.sqrMagnitude < closestDistance)
            {
                closestDistance = toZombie.sqrMagnitude;
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

        GameObject pea = ObjectPoolManager.Instance.GetPea();
        pea.transform.position = spawnPoint.position;
        pea.transform.rotation = spawnPoint.rotation;

        Rigidbody rb = pea.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = currentTarget != null
                ? currentTarget.transform.position + Vector3.up * 0.9f - spawnPoint.position
                : GetCurrentAimDirection();
            direction.Normalize();
            rb.linearVelocity = direction * projectileSpeed;
        }

        PeaProjectile pp = pea.GetComponent<PeaProjectile>();
        if (pp == null)
        {
            pp = pea.AddComponent<PeaProjectile>();
        }
        pp.Initialize(gameObject);

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
        ZombieHealth health = other != null ? other.GetComponentInParent<ZombieHealth>() : null;
        if (health != null) zombiesInRange.Remove(health.gameObject);
    }
}
