using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the grid of PlantableSquares on the map.
/// Supports four directional zones (North, South, East, West),
/// each with 3 lanes and multiple row depths.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("Number of lanes per zone (perpendicular to enemy approach direction).")]
    public int lanesPerZone = 3;
    [Tooltip("Number of row depths per zone (along enemy approach direction).")]
    public int rowsPerZone = 5;
    [Tooltip("Distance from house center beyond which a square belongs to a zone (inner boundary).")]
    public float innerBoundary = 8f;

    // Backward-compatible shims for legacy editor scripts
    [System.Obsolete("Use lanesPerZone instead.")]
    public int rows    { get => lanesPerZone;  set => lanesPerZone  = value; }
    [System.Obsolete("Use rowsPerZone instead.")]
    public int columns { get => rowsPerZone;   set => rowsPerZone   = value; }


    // ── Data structure: zone -> laneIndex -> list of squares (sorted depth-first, row 0 = closest to house) ──
    private Dictionary<LaneEntrance.Direction, Dictionary<int, List<PlantableSquare>>> zoneData
        = new Dictionary<LaneEntrance.Direction, Dictionary<int, List<PlantableSquare>>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            AutoDiscoverSquares();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Auto-discovers all PlantableSquare objects in the scene and organises them by zone and lane.
    /// A square's zone is determined by which axis it's furthest from the house on.
    /// Lane is determined by offset on the perpendicular axis.
    /// </summary>
    public void AutoDiscoverSquares()
    {
        zoneData.Clear();
        foreach (LaneEntrance.Direction dir in System.Enum.GetValues(typeof(LaneEntrance.Direction)))
            zoneData[dir] = new Dictionary<int, List<PlantableSquare>>();

        PlantableSquare[] allSquares = FindObjectsByType<PlantableSquare>(FindObjectsSortMode.None);

        if (allSquares.Length == 0)
        {
            Debug.LogWarning("[GridManager] No PlantableSquares found in scene.");
            return;
        }

        int assigned = 0;
        foreach (var sq in allSquares)
        {
            Vector3 pos = sq.transform.position;
            float absX = Mathf.Abs(pos.x);
            float absZ = Mathf.Abs(pos.z);

            // Determine direction by dominant axis and sign
            LaneEntrance.Direction dir;
            float perpCoord; // the coordinate on the perpendicular axis (for lane assignment)

            if (absZ >= absX && absZ >= innerBoundary)
            {
                dir = pos.z >= 0 ? LaneEntrance.Direction.North : LaneEntrance.Direction.South;
                perpCoord = pos.x;
            }
            else if (absX > absZ && absX >= innerBoundary)
            {
                dir = pos.x >= 0 ? LaneEntrance.Direction.East : LaneEntrance.Direction.West;
                perpCoord = pos.z;
            }
            else
            {
                // Too close to center — skip
                continue;
            }

            // Assign lane index (0=Left, 1=Center, 2=Right relative to approach)
            // We bucket by rounding to nearest lane using perpCoord
            int laneIndex = GetLaneIndexFromCoord(dir, perpCoord);
            if (laneIndex < 0) continue;

            if (!zoneData[dir].ContainsKey(laneIndex))
                zoneData[dir][laneIndex] = new List<PlantableSquare>();

            zoneData[dir][laneIndex].Add(sq);
            assigned++;
        }

        // Sort each lane: row 0 = closest to house, last row = farthest
        foreach (var dir in zoneData.Keys)
        {
            foreach (var lane in zoneData[dir].Keys)
            {
                zoneData[dir][lane].Sort((a, b) =>
                {
                    float da = GetApproachCoord(dir, a.transform.position);
                    float db = GetApproachCoord(dir, b.transform.position);
                    return da.CompareTo(db); // ascending: closest first
                });
            }
        }

        int totalLanes = 0;
        foreach (var dir in zoneData.Keys) totalLanes += zoneData[dir].Count;
        Debug.Log($"[GridManager] Discovered {assigned} squares across {totalLanes} lanes in 4 zones.");
    }

    // ── Public API ──────────────────────────────────────────────

    /// <summary>Returns all squares in a specific zone and lane, sorted closest-to-house first.</summary>
    public List<PlantableSquare> GetSquaresInLane(LaneEntrance.Direction dir, int laneIndex)
    {
        if (!zoneData.ContainsKey(dir) || !zoneData[dir].ContainsKey(laneIndex))
            return new List<PlantableSquare>();
        return new List<PlantableSquare>(zoneData[dir][laneIndex]);
    }

    /// <summary>
    /// Returns the first occupied plant in the enemy's lane that is in front of the enemy.
    /// enemyApproachCoord is the enemy's coordinate along the approach axis.
    /// </summary>
    public PlantBase FindFirstPlantInLane(LaneEntrance.Direction dir, int laneIndex, float enemyApproachCoord)
    {
        var squares = GetSquaresInLane(dir, laneIndex);
        bool fromPositive = dir == LaneEntrance.Direction.North || dir == LaneEntrance.Direction.East;

        foreach (var sq in squares)
        {
            if (!sq.isOccupied || sq.currentPlant == null) continue;
            float sqCoord = GetApproachCoord(dir, sq.transform.position);

            // Plant is "in front" of enemy if it's between enemy and house
            bool inFront = fromPositive
                ? sqCoord < enemyApproachCoord   // North/East: enemy approaches decreasing coord
                : sqCoord > enemyApproachCoord;  // South/West: enemy approaches increasing coord

            if (inFront) return sq.currentPlant;
        }
        return null;
    }

    /// <summary>Returns the world-space lane coordinate (X for N/S, Z for E/W) for the given lane index.</summary>
    public float GetLaneWorldCoord(LaneEntrance.Direction dir, int laneIndex)
    {
        if (!zoneData.ContainsKey(dir) || !zoneData[dir].ContainsKey(laneIndex)) return 0f;
        var squares = zoneData[dir][laneIndex];
        if (squares.Count == 0) return 0f;
        bool isNS = dir == LaneEntrance.Direction.North || dir == LaneEntrance.Direction.South;
        return isNS ? squares[0].transform.position.x : squares[0].transform.position.z;
    }

    public int LaneCountForZone(LaneEntrance.Direction dir) =>
        zoneData.ContainsKey(dir) ? zoneData[dir].Count : 0;

    // ── Private helpers ──────────────────────────────────────────

    /// <summary>Gets the approach-axis coordinate (Z for N/S, X for E/W).</summary>
    private float GetApproachCoord(LaneEntrance.Direction dir, Vector3 pos)
    {
        return (dir == LaneEntrance.Direction.North || dir == LaneEntrance.Direction.South)
            ? pos.z : pos.x;
    }

    /// <summary>Maps a perpendicular coordinate to a lane index 0-2. Returns -1 if out of range.</summary>
    private int GetLaneIndexFromCoord(LaneEntrance.Direction dir, float perpCoord)
    {
        // Lane centers are at -4, 0, +4 (adjusted for direction)
        // For South and West the left/right are mirrored
        bool mirrored = dir == LaneEntrance.Direction.South || dir == LaneEntrance.Direction.West;
        float[] centers = mirrored
            ? new[] { 4f, 0f, -4f }   // left relative to enemy = positive coord
            : new[] { -4f, 0f, 4f };  // left relative to enemy = negative coord

        float minDist = float.MaxValue;
        int bestLane = -1;
        for (int i = 0; i < centers.Length; i++)
        {
            float d = Mathf.Abs(perpCoord - centers[i]);
            if (d < minDist && d < 2.5f) // max 2.5m from lane center
            {
                minDist = d;
                bestLane = i;
            }
        }
        return bestLane;
    }
}
