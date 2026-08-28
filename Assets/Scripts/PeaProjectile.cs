using UnityEngine;

public class PeaProjectile : MonoBehaviour
{
    public float lifetime = 3f;
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
            // Here you would typically call a TakeDamage method on the zombie
            // other.GetComponent<ZombieHealth>()?.TakeDamage(damageAmount);
            
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
