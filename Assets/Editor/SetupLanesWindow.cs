using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupLanesWindow : EditorWindow
{
    [MenuItem("Tools/Setup Zombie Lanes")]
    public static void ShowWindow()
    {
        SetupLanes();
    }

    private static void SetupLanes()
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

        // FENCE GATE POSITIONS (Approximate from ZombieSpawner)
        CreateGate(container, LaneEntrance.Direction.North, new Vector3(0, 0, 18f), true, gm);
        CreateGate(container, LaneEntrance.Direction.South, new Vector3(0, 0, -18f), true, gm);
        CreateGate(container, LaneEntrance.Direction.East, new Vector3(18f, 0, 0), false, gm);
        CreateGate(container, LaneEntrance.Direction.West, new Vector3(-18f, 0, 0), false, gm);

        Debug.Log("Successfully generated 4 LaneEntrances with EXACTLY 3 lanes each!");
    }

    private static void CreateGate(GameObject parent, LaneEntrance.Direction dir, Vector3 gateApproxPos, bool isNorthSouth, GridManager gm)
    {
        int laneCount = gm.LaneCountForZone(dir);
        if (laneCount <= 0) return;

        GameObject gateObj = new GameObject($"LaneEntrance_{dir}");
        gateObj.transform.SetParent(parent.transform);
        gateObj.transform.position = gateApproxPos;

        // Big BoxCollider trigger that spans the whole gate
        BoxCollider col = gateObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        if (isNorthSouth)
            col.size = new Vector3(40f, 5f, 4f);
        else
            col.size = new Vector3(4f, 5f, 40f);

        LaneEntrance entrance = gateObj.AddComponent<LaneEntrance>();
        entrance.direction = dir;
        entrance.lanes = new LanePath[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            float coord = gm.GetLaneWorldCoord(dir, i);
            
            GameObject pathObj = new GameObject($"Lane{i}_Path");
            pathObj.transform.SetParent(gateObj.transform);

            GameObject entry = new GameObject("Entry");
            entry.transform.SetParent(pathObj.transform);
            
            GameObject end = new GameObject("End");
            end.transform.SetParent(pathObj.transform);

            Vector3 entryPos = gateApproxPos;
            Vector3 endPos = Vector3.zero;

            if (isNorthSouth)
            {
                entryPos.x = coord;
                endPos.x = coord;
                endPos.z = (dir == LaneEntrance.Direction.North) ? 4f : -4f;
            }
            else
            {
                entryPos.z = coord;
                endPos.z = coord;
                endPos.x = (dir == LaneEntrance.Direction.East) ? 4f : -4f;
            }

            entry.transform.position = entryPos;
            end.transform.position = endPos;

            LanePath path = pathObj.AddComponent<LanePath>();
            path.laneEntry = entry.transform;
            path.laneEnd = end.transform;

            entrance.lanes[i] = path;
        }
    }
}

