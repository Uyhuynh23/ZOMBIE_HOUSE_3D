using UnityEngine;

/// <summary>
/// Placed at each fence gate opening (one trigger per approach direction).
/// When an enemy crosses this trigger it is randomly assigned to one LanePath,
/// NavMesh is disabled, and manual lane movement begins.
///
/// Scene Setup
/// ────────────────────────────────────────────────────────────
/// 1. Create an empty GameObject at the fence gate, e.g. "LaneEntrance_North".
/// 2. Add this component + a BoxCollider (auto-set to trigger in Awake).
/// 3. Size the BoxCollider to span the FULL width of the gate opening
///    and at least 1–2 m deep (in the approach direction).
/// 4. Set 'direction' to match the side zombies come from.
/// 5. Drag your LanePath GameObjects (one per plant row) into the 'lanes' array.
///    Make sure every LanePath.IsValid (laneEntry + laneEnd are assigned).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class LaneEntrance : MonoBehaviour
{
    public enum Direction { North, South, East, West }

    [Tooltip("Which side enemies approach from.")]
    public Direction direction;

    [Tooltip("One LanePath per plant row for this gate.\n" +
             "The zombie will randomly pick one when it crosses this trigger.")]
    public LanePath[] lanes;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        EnemyNavAgent navAgent = other.GetComponentInParent<EnemyNavAgent>();
        if (navAgent == null || navAgent.HasLaneAssigned) return;

        LanePath chosen = PickRandomLane();
        if (chosen == null)
        {
            Debug.LogWarning($"[LaneEntrance] '{name}' ({direction}): no valid LanePaths assigned! " +
                             "Drag LanePath objects into the 'lanes' array in the Inspector.");
            return;
        }

        navAgent.AssignLane(chosen);
        Debug.Log($"[LaneEntrance] {other.name} → {direction} lane '{chosen.name}'");
    }

    private LanePath PickRandomLane()
    {
        if (lanes == null || lanes.Length == 0) return null;

        // Collect valid lanes
        var valid = new System.Collections.Generic.List<LanePath>(lanes.Length);
        foreach (var lp in lanes)
            if (lp != null && lp.IsValid) valid.Add(lp);

        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;
        Gizmos.color  = new Color(0f, 1f, 0.5f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color  = new Color(0f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
}
