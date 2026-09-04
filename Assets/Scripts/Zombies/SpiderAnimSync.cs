using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Syncs the Spider's walk animation speed with actual NavMeshAgent velocity.
/// Prevents the sliding/floating look when NavMesh changes speed dynamically.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SpiderAnimSync : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation")]
    [Tooltip("Animator float parameter name driving the spider walk cycle.")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField, Min(0.01f)] private float dampTime = 0.08f;
    [Tooltip("Normalizing divisor — set to spider's max speed so param goes 0..1.")]
    [SerializeField, Min(0.1f)] private float maxSpeed = 2.5f;

    private NavMeshAgent agent;
    private int moveSpeedHash;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        moveSpeedHash = Animator.StringToHash(moveSpeedParam);
    }

    private void Update()
    {
        if (animator == null || agent == null) return;

        float speed = agent.isActiveAndEnabled && agent.isOnNavMesh
            ? agent.velocity.magnitude / Mathf.Max(0.01f, maxSpeed)
            : 0f;

        animator.SetFloat(moveSpeedHash, speed, dampTime, Time.deltaTime);
    }
}
