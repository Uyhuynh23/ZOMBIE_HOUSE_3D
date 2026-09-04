using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Adds procedural sway/limp to enemies to make movement feel heavy and zombie-like.
/// Uses NavMeshAgent velocity (or Animator MoveSpeed as fallback) to drive sway intensity.
/// </summary>
public class ZombieSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("Max side-to-side lean angle (degrees Z rotation).")]
    public float sideSwayAngle = 5f;
    [Tooltip("Max limp drop (Y position offset).")]
    public float limpAmount = 0.05f;
    [Tooltip("Base sway frequency multiplier.")]
    public float swaySpeed = 4f;

    [Header("References")]
    [Tooltip("The Animator used to check move speed (fallback if no NavMeshAgent).")]
    public Animator animator;
    [Tooltip("The visual root transform to apply sway to. Auto-detected from first child if null.")]
    public Transform visualRoot;

    private float timer;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    private NavMeshAgent agent;
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0);

        if (visualRoot != null)
        {
            initialLocalPos = visualRoot.localPosition;
            initialLocalRot = visualRoot.localRotation;
        }
    }

    private void LateUpdate()
    {
        if (visualRoot == null) return;

        // Prefer NavMesh agent velocity; fall back to Animator float
        float speed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            speed = agent.velocity.magnitude;
        else if (animator != null)
            speed = animator.GetFloat(MoveSpeedHash);

        if (speed > 0.1f)
        {
            float normalizedSpeed = Mathf.Clamp01(speed / 2.0f);
            timer += Time.deltaTime * swaySpeed * (0.7f + normalizedSpeed);

            // Limp: dip down every half-step
            float limp = Mathf.Abs(Mathf.Sin(timer)) * limpAmount * normalizedSpeed;

            // Side sway: asymmetric wobble
            float sway = Mathf.Sin(timer) * sideSwayAngle * normalizedSpeed;

            // Forward pitch: slight stumble forward
            float pitch = Mathf.Abs(Mathf.Sin(timer * 0.5f)) * sideSwayAngle * 0.4f * normalizedSpeed;

            Vector3 targetPos = initialLocalPos - new Vector3(0, limp, 0);
            Quaternion targetRot = initialLocalRot * Quaternion.Euler(pitch, 0f, sway);

            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPos, Time.deltaTime * 12f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRot, Time.deltaTime * 12f);
        }
        else
        {
            // Return to idle posture
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, initialLocalPos, Time.deltaTime * 6f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, initialLocalRot, Time.deltaTime * 6f);
        }
    }
}
