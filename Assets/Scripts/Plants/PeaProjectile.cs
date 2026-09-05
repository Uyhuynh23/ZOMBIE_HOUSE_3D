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
        // Imported enemies can expose a child collider whose tag is Untagged.
        // Resolve health from the hierarchy so both Zombie and Spider take
        // damage regardless of which collider the projectile reaches first.
        ZombieHealth zh = other.GetComponentInParent<ZombieHealth>();
        if (zh != null)
        {
            zh.TakeDamage(damage);
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
