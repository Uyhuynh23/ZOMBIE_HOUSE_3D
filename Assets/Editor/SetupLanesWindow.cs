using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class SetupLanesWindow : EditorWindow
{
    [MenuItem("Tools/Setup Zombie Lanes")]
    public static void ShowWindow()
    {
        RebuildOpenScene();
    }

    [MenuItem("Tools/Setup Zombie Lanes/Rebuild Open Scene")]
    public static void RebuildOpenScene()
    {
        // 1. Find GridManager and ensure it has discovered squares
        GridManager gm = FindObjectOfType<GridManager>();
        if (gm == null)
        {
            Debug.LogError("No GridManager found in scene!");
            return;
        }
        // Force discovery just in case
        gm.AutoDiscoverSquares();

        // 2. Setup Container
        GameObject container = GameObject.Find("LaneEntrances");
        if (container != null) DestroyImmediate(container);
        container = new GameObject("LaneEntrances");

        // Map_Cloudy's plant grids extend from roughly 16 m to 30 m from the
        // house.  Put each trigger immediately OUTSIDE the outer row instead
        // of at 15 m (which previously skipped several plant rows).
        const float outerGate = 31.5f;
        CreateGate(container, LaneEntrance.Direction.North, new Vector3(0, 0, outerGate), true, gm);
        CreateGate(container, LaneEntrance.Direction.South, new Vector3(0, 0, -outerGate), true, gm);
        CreateGate(container, LaneEntrance.Direction.East, new Vector3(outerGate, 0, 0), false, gm);
        CreateGate(container, LaneEntrance.Direction.West, new Vector3(-outerGate, 0, 0), false, gm);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Setup Zombie Lanes] Rebuilt 4 lane entrances with exactly 3 paths each.");
    }

    /// <summary>Batch-safe entry point used to repair the committed Map_Cloudy scene.</summary>
    public static void RebuildMapCloudy()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScenes/Map_Cloudy.unity");
        RebuildOpenScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup Zombie Lanes] Saved repaired Map_Cloudy scene.");
    }

    private static void CreateGate(GameObject parent, LaneEntrance.Direction dir, Vector3 gateApproxPos, bool isNorthSouth, GridManager gm)
    {
        const int laneCount = 3;
        if (gm.LaneCountForZone(dir) != laneCount)
            Debug.LogWarning($"[Setup Zombie Lanes] {dir} grid exposes {gm.LaneCountForZone(dir)} lanes; " +
                             "using the standard three lane coordinates.");

        GameObject gateObj = new GameObject($"LaneEntrance_{dir}");
        gateObj.transform.SetParent(parent.transform);
        gateObj.transform.position = gateApproxPos;

        // Big BoxCollider trigger that spans the whole gate
        BoxCollider col = gateObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        if (isNorthSouth)
            col.size = new Vector3(40f, 5f, 2f);
        else
            col.size = new Vector3(2f, 5f, 40f);

        LaneEntrance entrance = gateObj.AddComponent<LaneEntrance>();
        entrance.direction = dir;
        entrance.lanes = new LanePath[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            // The farthest plantable square is the only valid lane entry.
            // Enemies may approach it from outside the fence, but once they
            // reach this point their movement is a locked straight line toward
            // the house, allowing plants in this exact lane to target them.
            var laneSquares = gm.GetSquaresInLane(dir, i);
            Vector3 entryPos;
            float coord;
            if (laneSquares.Count > 0)
            {
                entryPos = laneSquares[laneSquares.Count - 1].transform.position;
                coord = isNorthSouth ? entryPos.x : entryPos.z;
            }
            else
            {
                coord = GetFallbackLaneCoord(dir, i);
                entryPos = gateApproxPos;
                if (isNorthSouth) entryPos.x = coord;
                else entryPos.z = coord;
            }
            
            GameObject pathObj = new GameObject($"Lane{i}_Path");
            pathObj.transform.SetParent(gateObj.transform);

            GameObject entry = new GameObject("Entry");
            entry.transform.SetParent(pathObj.transform);
            
            GameObject end = new GameObject("End");
            end.transform.SetParent(pathObj.transform);

            // Continue through the house rather than stopping at a nearby
            // point.  EnemyNavAgent will stop only when its sensor contacts a
            // HouseHealth collider, never when it merely reaches this marker.
            Vector3 endPos = Vector3.zero;

            if (isNorthSouth)
            {
                endPos.x = coord;
                endPos.z = (dir == LaneEntrance.Direction.North) ? -8f : 8f;
            }
            else
            {
                endPos.z = coord;
                endPos.x = (dir == LaneEntrance.Direction.East) ? -8f : 8f;
            }

            endPos.y = entryPos.y;

            entry.transform.position = entryPos;
            end.transform.position = endPos;

            LanePath path = pathObj.AddComponent<LanePath>();
            path.laneEntry = entry.transform;
            path.laneEnd = end.transform;

            entrance.lanes[i] = path;
        }
    }

    private static float GetFallbackLaneCoord(LaneEntrance.Direction dir, int laneIndex)
    {
        bool mirrored = dir == LaneEntrance.Direction.South || dir == LaneEntrance.Direction.West;
        float[] centers = mirrored ? new[] { 4f, 0f, -4f } : new[] { -4f, 0f, 4f };
        return centers[laneIndex];
    }
}
