using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(SphereCollider))]
public class Sun : NetworkBehaviour
{
    public int sunValue = 25;
    public float lifetime = 15f;
    public float floatAmplitude = 0.15f;
    public float floatSpeed = 4f;

    private float timer;
    private bool collected = false;
    private Vector3 basePosition;

    void Start()
    {
        timer = lifetime;
        basePosition = transform.position;

        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;
    }

    void Update()
    {
        if (NetworkBootstrap.IsNetworkSession && !IsServer) return;
        // Float animation using offset from base position (no drift)
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        timer -= Time.deltaTime;
        if (timer <= 0f && !collected)
        {
            DespawnOrDestroy();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!NetworkGameplayAuthority.IsServer || !NetworkGameplayAuthority.CanMutate) return;
        if (collected) return;

        if (other.GetComponent<PlayerController>() != null || other.CompareTag("Player"))
        {
            collected = true;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddSun(sunValue);
            }
            AudioManager.PlaySfx(AudioCue.SunCollect);
            Debug.Log($"[NET][ECON] Sun collected value={sunValue} by={other.name}.");
            DespawnOrDestroy();
        }
    }

    private void DespawnOrDestroy()
    {
        NetworkObject no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned && IsServer) no.Despawn(true);
        else Destroy(gameObject);
    }
}
