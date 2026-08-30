using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("The projectile prefab to pool.")]
    public GameObject peaPrefab;
    [Tooltip("Initial number of objects to spawn in the pool.")]
    public int initialPoolSize = 50;

    private Queue<GameObject> peaPool = new Queue<GameObject>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        if (peaPrefab == null)
        {
            Debug.LogError("ObjectPoolManager: Pea Prefab is not assigned!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject newPea = Instantiate(peaPrefab, transform);
            newPea.SetActive(false);
            peaPool.Enqueue(newPea);
        }
    }

    /// <summary>
    /// Retrieves a Pea projectile from the pool.
    /// </summary>
    public GameObject GetPea()
    {
        if (peaPool.Count > 0)
        {
            GameObject pea = peaPool.Dequeue();
            pea.SetActive(true);
            return pea;
        }
        else
        {
            // Expand pool if we run out (to prevent errors, though GC spikes could occur if this happens frequently)
            Debug.LogWarning("ObjectPoolManager: Pool exhausted. Instantiating a new object.");
            GameObject newPea = Instantiate(peaPrefab, transform);
            newPea.SetActive(true);
            return newPea;
        }
    }

    /// <summary>
    /// Returns a Pea projectile to the pool for reuse.
    /// </summary>
    public void ReturnPea(GameObject pea)
    {
        // Reset velocity just in case
        Rigidbody rb = pea.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        pea.SetActive(false);
        peaPool.Enqueue(pea);
    }
}
