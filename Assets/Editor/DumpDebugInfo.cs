using UnityEngine;
using UnityEditor;

public class DumpDebugInfo {
    public static void Dump() {
        var spawner = Object.FindObjectOfType<ZombieSpawner>();
        if (spawner != null) {
            Debug.Log("--- SPAWN POINTS ---");
            foreach(var sp in spawner.spawnPoints) {
                if(sp) Debug.Log($"Spawn {sp.name}: {sp.position}");
            }
        }
        var gates = Object.FindObjectsOfType<LaneEntrance>();
        Debug.Log("--- GATES ---");
        foreach(var g in gates) {
            var col = g.GetComponent<BoxCollider>();
            Debug.Log($"Gate {g.direction} at {g.transform.position}. Collider center: {col.center}, size: {col.size}");
            foreach(var lane in g.lanes) {
                if(lane) Debug.Log($"  Lane {lane.name}: Entry {lane.laneEntry.position}, End {lane.laneEnd.position}");
            }
        }
    }
}
