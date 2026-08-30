using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the grid of PlantableSquares on the map.
/// Provides lane-based queries for the zombie spawn system.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("Number of lane rows (Z-axis)")]
    public int rows = 3;
    [Tooltip("Number of plant columns (X-axis)")]
    public int columns = 5;

    /// <summary>All PlantableSquares registered in the scene, keyed by lane row index.</summary>
    private Dictionary<int, List<PlantableSquare>> laneSquares = new Dictionary<int, List<PlantableSquare>>();

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
    /// Auto-discovers all PlantableSquare objects in the scene and organises them by lane row.
    /// Called once on Awake; can be called again if squares are dynamically added.
    /// </summary>
    public void AutoDiscoverSquares()
    {
        laneSquares.Clear();

        PlantableSquare[] allSquares = FindObjectsByType<PlantableSquare>(FindObjectsSortMode.None);

        if (allSquares.Length == 0)
        {
            Debug.LogWarning("[GridManager] No PlantableSquares found in scene.");
            return;
        }

        // Find Z bounds so we can bucket by row
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        foreach (var sq in allSquares)
        {
            float z = sq.transform.position.z;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
        }

        float range = maxZ - minZ;

        foreach (var sq in allSquares)
        {
            float z = sq.transform.position.z;
            int laneIndex;
            if (range < 0.001f)
                laneIndex = 0;
            else
                laneIndex = Mathf.RoundToInt((z - minZ) / range * (rows - 1));

            laneIndex = Mathf.Clamp(laneIndex, 0, rows - 1);
            if (!laneSquares.ContainsKey(laneIndex))
                laneSquares[laneIndex] = new List<PlantableSquare>();

            laneSquares[laneIndex].Add(sq);
        }

        Debug.Log($"[GridManager] Discovered {allSquares.Length} squares across {laneSquares.Count} lanes.");
    }

    /// <summary>Returns the number of lanes discovered.</summary>
    public int LaneCount => laneSquares.Count;

    /// <summary>
    /// Returns the world-space Z position of the given lane.
    /// Returns 0 if lane is not found.
    /// </summary>
    public float GetLaneZ(int laneIndex)
    {
        if (!laneSquares.ContainsKey(laneIndex) || laneSquares[laneIndex].Count == 0)
            return 0f;

        return laneSquares[laneIndex][0].transform.position.z;
    }

    /// <summary>
    /// Returns all squares in a lane sorted by X (nearest to zombie spawn first).
    /// </summary>
    public List<PlantableSquare> GetSquaresInLane(int laneIndex)
    {
        if (!laneSquares.ContainsKey(laneIndex))
            return new List<PlantableSquare>();

        var list = new List<PlantableSquare>(laneSquares[laneIndex]);
        list.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x)); // right-to-left
        return list;
    }

    /// <summary>
    /// Returns the first occupied plant in the zombie's lane that is directly in front of it (higher X).
    /// </summary>
    public PlantBase FindFirstPlantInLane(int laneIndex, float zombieX)
    {
        var squares = GetSquaresInLane(laneIndex);
        // Squares sorted right-to-left (high X first); zombie walks toward lower X
        foreach (var sq in squares)
        {
            if (sq.isOccupied && sq.currentPlant != null && sq.transform.position.x <= zombieX)
                return sq.currentPlant;
        }
        return null;
    }
}
