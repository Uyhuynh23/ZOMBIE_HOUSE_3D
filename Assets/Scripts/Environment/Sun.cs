using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Sun : MonoBehaviour
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
        // Float animation using offset from base position (no drift)
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        timer -= Time.deltaTime;
        if (timer <= 0f && !collected)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.GetComponent<PlayerController>() != null || other.CompareTag("Player"))
        {
            collected = true;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddSun(sunValue);
            }
            AudioManager.PlaySfx(AudioCue.SunCollect);
            Destroy(gameObject);
        }
    }
}
