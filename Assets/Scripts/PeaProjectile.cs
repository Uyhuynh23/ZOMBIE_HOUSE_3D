using UnityEngine;

public class PeaProjectile : MonoBehaviour
{
    public float lifetime = 3f;
    public int damage = 20;
    private float timer;

    public void Initialize()
    {
        timer = lifetime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            // Deal damage to the zombie
            ZombieHealth zh = other.GetComponentInParent<ZombieHealth>();
            if (zh != null)
            {
                zh.TakeDamage(damage);
            }

            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnPea(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
