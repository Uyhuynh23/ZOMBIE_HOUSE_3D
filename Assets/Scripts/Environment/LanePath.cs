using UnityEngine;

/// <summary>
/// Describes one plant lane inside the fence.
/// Place one LanePath MonoBehaviour per lane in the scene.
/// Wire Entry and End transforms in the Inspector.
///
///   LaneEntry  — world-space point just inside the fence gate,
///                centred on the plant-row column.
///   LaneEnd    — world-space point at the house-side of the lane
///                (just in front of Baker_house).
///
/// Scene setup:
///   1. Create an empty GameObject per lane, e.g.  "Lane0_Path".
///   2. Add this component.
///   3. Add two child empties:  "Entry"  and  "End".
///   4. Position Entry  at the fence-inside edge of the row, centred on the column.
///   5. Position End    directly in front of the house on the same column.
///   6. Drag those children into the Entry / End fields below.
///   7. Repeat for each lane (0, 1, 2 for the three plant rows).
/// </summary>
public class LanePath : MonoBehaviour
{
    [Tooltip("World-space point just inside the fence where the zombie enters the lane.")]
    public Transform laneEntry;

    [Tooltip("World-space point in front of the house at the end of the lane.")]
    public Transform laneEnd;

    public bool IsValid => laneEntry != null && laneEnd != null;

    private void OnDrawGizmos()
    {
        if (laneEntry == null || laneEnd == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Gizmos.DrawLine(laneEntry.position, laneEnd.position);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireSphere(laneEntry.position, 0.25f);

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(laneEnd.position, 0.25f);
    }
}

