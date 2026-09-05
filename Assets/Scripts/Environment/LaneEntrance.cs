using UnityEngine;

/// <summary>
/// Placed at each fence gate opening.
/// When an enemy with EnemyNavAgent enters this trigger, it is assigned
/// to a specific direction and lane, constraining its movement inside the fence.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class LaneEntrance : MonoBehaviour
{
    public enum Direction { North, South, East, West }

    [Tooltip("Which direction enemies approach from (their spawn side).")]
    public Direction direction;

    [Tooltip("Lane index: 0=Left, 1=Center, 2=Right (relative to approach direction).")]
    [Range(0, 2)]
    public int laneIndex;

    [Tooltip("World-space coordinate on the perpendicular axis for this lane.\n" +
             "North/South: this is the X position. East/West: this is the Z position.")]
    public float laneWorldCoord;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        var navAgent = other.GetComponentInParent<EnemyNavAgent>();
        if (navAgent == null) return;

        // Only assign lane once (prevent re-assignment if enemy re-enters)
        if (navAgent.HasLaneAssigned) return;

        navAgent.AssignLane(direction, laneIndex, laneWorldCoord);
        Debug.Log($"[LaneEntrance] {other.name} -> {direction} Lane {laneIndex} (coord={laneWorldCoord:F1})");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}
