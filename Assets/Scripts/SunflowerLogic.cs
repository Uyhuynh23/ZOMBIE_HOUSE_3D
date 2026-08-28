using UnityEngine;

public class SunflowerLogic : MonoBehaviour
{
    public GameObject sunPrefab;
    public float productionInterval = 10f;
    
    private float timer;
    private Animator animator;

    void Start()
    {
        timer = productionInterval;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ProduceSun();
            timer = productionInterval;
        }
    }

    void ProduceSun()
    {
        if (animator != null)
        {
            // Fallback to Shoot if Produce doesn't exist yet
            animator.SetTrigger("Shoot"); 
        }

        if (sunPrefab != null)
        {
            // Spawn sun slightly in front and above
            Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 1f;
            Instantiate(sunPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Sunflower: sunPrefab is not assigned! Ensure the Sun prefab is linked.");
        }
    }
}
