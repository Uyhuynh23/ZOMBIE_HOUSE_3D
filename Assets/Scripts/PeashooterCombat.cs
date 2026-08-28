using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PeashooterCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float fireRate = 1f;
    public float projectileSpeed = 10f;
    public float aggroRadius = 5f;

    [Header("References")]
    public Transform spawnPoint;

    private SphereCollider aggroCollider;
    private List<GameObject> zombiesInRange = new List<GameObject>();
    private float fireTimer = 0f;

    void Start()
    {
        // Setup aggro collider if not already set
        aggroCollider = GetComponent<SphereCollider>();
        aggroCollider.isTrigger = true;
        aggroCollider.radius = aggroRadius;

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
            // Optional: rotate to face the closest zombie
            Vector3 direction = (zombiesInRange[0].transform.position - transform.position).normalized;
            direction.y = 0; // Keep rotation horizontal
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            }

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                FireProjectile(zombiesInRange[0].transform);
                fireTimer = 1f / fireRate;
            }
        }
        else
        {
            fireTimer = 0f; // Ready to fire immediately when a zombie enters
        }
    }

    void FireProjectile(Transform target)
    {
        if (ObjectPoolManager.Instance == null || spawnPoint == null) return;

        GameObject pea = ObjectPoolManager.Instance.GetPea();
        pea.transform.position = spawnPoint.position;
        pea.transform.rotation = spawnPoint.rotation;

        Rigidbody rb = pea.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Aim slightly above the target's base if they don't have a center point defined
            Vector3 targetPos = target.position + Vector3.up * 1f; 
            Vector3 direction = (targetPos - spawnPoint.position).normalized;
            rb.linearVelocity = direction * projectileSpeed;
        }

        // Ensure the projectile has the return script
        PeaProjectile pp = pea.GetComponent<PeaProjectile>();
        if (pp == null)
        {
            pp = pea.AddComponent<PeaProjectile>();
        }
        pp.Initialize();
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
