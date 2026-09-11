using UnityEngine;
using Unity.Netcode;

public class PeaProjectile : NetworkBehaviour
{
    public float lifetime = 3f;
    [Min(0.1f)] public float maxTravelDistance = 35f;
    public int damage = 20;
    private float timer;
    private Vector3 launchPosition;
    private Transform ownerRoot;
    private bool hasHit;

    public override void OnNetworkSpawn()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !IsServer) rb.isKinematic = true;
    }

    public void Initialize()
    {
        Initialize(null);
    }

    public void Initialize(GameObject owner)
    {
        timer = lifetime;
        launchPosition = transform.position;
        ownerRoot = owner != null ? owner.transform.root : null;
        hasHit = false;
    }

    void Update()
    {
        if (NetworkBootstrap.IsNetworkSession && !IsServer) return;
        timer -= Time.deltaTime;
        if (timer <= 0f || (transform.position - launchPosition).sqrMagnitude >= maxTravelDistance * maxTravelDistance)
        {
            ReturnToPool();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!NetworkGameplayAuthority.IsServer || !NetworkGameplayAuthority.CanMutate) return;
        if (hasHit || other == null || (ownerRoot != null && other.transform.root == ownerRoot)) return;
        // Imported enemies can expose a child collider whose tag is Untagged.
        // Resolve health from the hierarchy so both Zombie and Spider take
        // damage regardless of which collider the projectile reaches first.
        ZombieHealth zh = other.GetComponentInParent<ZombieHealth>();
        if (zh != null)
        {
            hasHit = true;
            zh.TakeDamage(damage);
            AudioManager.PlaySfx(AudioCue.ProjectileHit);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        NetworkObject no = GetComponent<NetworkObject>();
        if (NetworkBootstrap.IsNetworkSession && no != null && no.IsSpawned)
        {
            if (IsServer) no.Despawn(true);
        }
        else if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnPea(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
