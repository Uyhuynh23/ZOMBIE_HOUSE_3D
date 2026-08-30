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

    protected override void Awake()
    {
        base.Awake();
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
        // Clean up destroyed zombies
        zombiesInRange.RemoveWhere(z => z == null || !z.activeInHierarchy);

        // Check if any zombie is in the forward cone
        currentTarget = null;
        float closestDistance = float.MaxValue;
        foreach (var z in zombiesInRange)
        {
            if (z == null) continue;
            Vector3 toZombie = z.transform.position - transform.position;
            float laneDistance = Mathf.Abs(toZombie.z);
            bool isAhead = toZombie.x > 0.15f;
            if (isAhead && laneDistance <= 0.85f && toZombie.sqrMagnitude < closestDistance)
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
                : transform.right;
            direction.Normalize();
            rb.linearVelocity = direction * projectileSpeed;
        }

        PeaProjectile pp = pea.GetComponent<PeaProjectile>();
        if (pp == null)
        {
            pp = pea.AddComponent<PeaProjectile>();
        }
        pp.Initialize();

        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            // Always use root to avoid multi-collider duplication
            zombiesInRange.Add(other.transform.root.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            zombiesInRange.Remove(other.transform.root.gameObject);
        }
    }
}
