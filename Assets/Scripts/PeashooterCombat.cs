using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(CapsuleCollider))]
public class PeashooterCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float fireRate = 1f;
    public float projectileSpeed = 10f;
    public float aggroRadius = 5f;

    [Header("Body Collider Settings")]
    public float bodyHeight = 1.0f;
    public float bodyRadius = 0.35f;
    public Vector3 bodyCenter = new Vector3(0f, 0.5f, 0f);

    [Header("References")]
    public Transform spawnPoint;

    private SphereCollider aggroCollider;
    private CapsuleCollider bodyCollider;
    private List<GameObject> zombiesInRange = new List<GameObject>();
    private float fireTimer = 0f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        // ── Aggro trigger (SphereCollider) ──
        aggroCollider = GetComponent<SphereCollider>();
        aggroCollider.isTrigger = true;
        aggroCollider.radius = aggroRadius;

        // ── Physical body (CapsuleCollider) — blocks character movement ──
        bodyCollider = GetComponent<CapsuleCollider>();
        bodyCollider.isTrigger = false;
        bodyCollider.height = bodyHeight;
        bodyCollider.radius = bodyRadius;
        bodyCollider.center = bodyCenter;
        bodyCollider.direction = 1; // Y-axis (upright capsule)

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
        // Clean up list in case zombies were destroyed
        zombiesInRange.RemoveAll(z => z == null || !z.activeInHierarchy);

        if (zombiesInRange.Count > 0)
        {
            // Do NOT rotate toward the zombie. The plant's direction is fixed by the ground it was planted on!
            
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
            // Shoot straight forward relative to the plant's current rotation (which matches the ground's forward)
            Vector3 direction = transform.forward;
            
            // Unity 6 uses linearVelocity, older versions use velocity. Fallback to velocity if linearVelocity is a compile error, but linearVelocity is fine here.
            rb.linearVelocity = direction * projectileSpeed;
        }

        // Ensure the projectile has the return script
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
            if (!zombiesInRange.Contains(other.gameObject))
            {
                zombiesInRange.Add(other.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            zombiesInRange.Remove(other.gameObject);
        }
    }
}
