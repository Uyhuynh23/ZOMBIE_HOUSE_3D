using UnityEngine;

/// <summary>
/// Ordered world-space path for one map entrance.
/// The scene builder samples smooth entry/exit curves into child waypoints while
/// keeping the combat section perfectly straight for Plants-vs-Zombies gameplay.
/// </summary>
public sealed class ZombieRoute : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0)] private int combatStartIndex;
    [SerializeField, Min(0)] private int combatEndIndex;

    public int WaypointCount => waypoints == null ? 0 : waypoints.Length;
    public int CombatStartIndex => combatStartIndex;
    public int CombatEndIndex => combatEndIndex;
    public Transform SpawnPoint => WaypointCount == 0 ? null : waypoints[0];

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Length)
            return null;
        return waypoints[index];
    }

    public void Configure(Transform[] orderedWaypoints, int straightStartIndex, int straightEndIndex)
    {
        waypoints = orderedWaypoints;
        combatStartIndex = Mathf.Clamp(straightStartIndex, 0, Mathf.Max(0, WaypointCount - 1));
        combatEndIndex = Mathf.Clamp(straightEndIndex, combatStartIndex, Mathf.Max(0, WaypointCount - 1));
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i - 1] == null || waypoints[i] == null)
                continue;

            bool combatSegment = i - 1 >= combatStartIndex && i <= combatEndIndex;
            Gizmos.color = combatSegment
                ? new Color(1f, 0.25f, 0.15f, 0.9f)
                : new Color(1f, 0.72f, 0.12f, 0.9f);
            Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            Gizmos.DrawSphere(waypoints[i].position, combatSegment ? 0.16f : 0.11f);
        }
    }
}
