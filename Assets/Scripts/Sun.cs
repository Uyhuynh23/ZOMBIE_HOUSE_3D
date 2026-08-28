using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Sun : MonoBehaviour
{
    public int sunValue = 25;
    public float lifetime = 15f;
    private float timer;
    private bool collected = false;

    void Start()
    {
        timer = lifetime;
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f; // Generous pickup radius
    }

    void Update()
    {
        // Simple floating animation
        transform.position += new Vector3(0, Mathf.Sin(Time.time * 4f) * 0.002f, 0);

        timer -= Time.deltaTime;
        if (timer <= 0f && !collected)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Player can collect it
        if (other.GetComponent<PlayerController>() != null || other.CompareTag("Player"))
        {
            collected = true;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddSun(sunValue);
            }
            Destroy(gameObject);
        }
    }
}
