using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class SunflowerLogic : PlantBase
{
    public GameObject sunPrefab;
    public float productionInterval = 10f;

    [Header("Body Collider Settings")]
    public float bodyHeight = 0.8f;
    public float bodyRadius = 0.3f;
    public Vector3 bodyCenter = new Vector3(0f, 0.4f, 0f);

    private float timer;
    private Animator animator;
    private CapsuleCollider bodyCollider;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        timer = productionInterval;
        animator = GetComponent<Animator>();

        // Physical body collider
        bodyCollider = GetComponent<CapsuleCollider>();
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
            bodyCollider.height = bodyHeight;
            bodyCollider.radius = bodyRadius;
            bodyCollider.center = bodyCenter;
            bodyCollider.direction = 1; // Y-axis
        }
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
            animator.SetTrigger("Shoot");
        }

        if (sunPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 1f;
            Instantiate(sunPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Sunflower: sunPrefab is not assigned!");
        }
    }
}
