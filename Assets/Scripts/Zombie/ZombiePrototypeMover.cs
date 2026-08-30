using UnityEngine;

public sealed class ZombiePrototypeMover : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

    [Header("Prototype Patrol")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField, Min(0.1f)] private float moveSpeed = 0.7f;
    [SerializeField, Min(1f)] private float turnSpeed = 220f;
    [SerializeField, Min(0f)] private float pauseDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.04f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float animationDampTime = 0.12f;
    [Tooltip("The imported cartoon zombie faces local -Z instead of Unity's +Z.")]
    [SerializeField] private bool visualFacesBackward = true;

    private Transform currentTarget;
    private float pauseTimer;

    public void Configure(Animator targetAnimator, Transform pointA, Transform pointB)
    {
        animator = targetAnimator;
        patrolPointA = pointA;
        patrolPointB = pointB;
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        currentTarget = patrolPointB != null ? patrolPointB : patrolPointA;
        SetAnimationSpeed(0f);
    }

    private void Update()
    {
        if (currentTarget == null)
        {
            SetAnimationSpeed(0f);
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            SetAnimationSpeed(0f);
            return;
        }

        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            transform.position = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
            currentTarget = currentTarget == patrolPointA ? patrolPointB : patrolPointA;
            pauseTimer = pauseDuration;
            SetAnimationSpeed(0f);
            return;
        }

        Vector3 direction = toTarget.normalized;
        Vector3 facingDirection = visualFacesBackward ? -direction : direction;
        Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        float alignment = Vector3.Dot(transform.forward, facingDirection);
        if (alignment > 0.75f)
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);

        SetAnimationSpeed(1f);
    }

    private void SetAnimationSpeed(float value)
    {
        if (animator != null)
            animator.SetFloat(MoveSpeedHash, value, animationDampTime, Time.deltaTime);
    }
}
