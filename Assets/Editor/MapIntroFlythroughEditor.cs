#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapIntroFlythrough))]
public class MapIntroFlythroughEditor : Editor
{
    private float previewScrub = 0f;
    private bool syncGameView = false;
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private bool cachedCamOriginal = false;

    private void OnDisable()
    {
        RestoreMainCamera();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapIntroFlythrough flythrough = (MapIntroFlythrough)target;
        if (flythrough == null) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎬 Flight Pacing & Motion Comfort", EditorStyles.boldLabel);
        float totalTime = flythrough.GetTotalDuration();
        EditorGUILayout.HelpBox($"Total Flight Duration: {totalTime:F1}s  (Speed Multiplier: {flythrough.durationMultiplier:F2}x)\nChoose a preset below to eliminate nausea or adjust flight speed with 1 click:", MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⚡ Fast (0.75x)"))
        {
            Undo.RecordObject(flythrough, "Set Pacing Fast");
            flythrough.durationMultiplier = 0.75f;
            EditorUtility.SetDirty(flythrough);
        }
        if (GUILayout.Button("🎬 Smooth (1.0x)"))
        {
            Undo.RecordObject(flythrough, "Set Pacing Normal");
            flythrough.durationMultiplier = 1.0f;
            EditorUtility.SetDirty(flythrough);
        }
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("🧘 Calm / Gentle (1.35x)"))
        {
            Undo.RecordObject(flythrough, "Set Pacing Calm");
            flythrough.durationMultiplier = 1.35f;
            EditorUtility.SetDirty(flythrough);
        }
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("🐌 Slow Drone (1.75x)"))
        {
            Undo.RecordObject(flythrough, "Set Pacing Slow");
            flythrough.durationMultiplier = 1.75f;
            EditorUtility.SetDirty(flythrough);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Interactive Shot Director & Preview", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "HOW TO PREVIEW IN EDIT MODE:\n" +
            "• Click [👁️ Scene View] on any shot below to jump your 3D Scene View directly to that camera angle.\n" +
            "• Click [🎥 Game View] to position the Main Camera there so the Game View tab shows the exact 1:1 in-game look (lighting, UI, aspect ratio)!\n" +
            "• Enable 'Live Game View Preview' and drag the Flight Scrubber to watch the intro tour animate right inside the Game View window.",
            MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🔄 Quick Threat Order Presets", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click any preset below to instantly reorder the 4 threat approaches (no dragging needed!):", MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CW: East → South → West → North", GUILayout.Height(22)))
        {
            ReorderThreats(flythrough, new string[] { "East", "South", "West", "North" });
        }
        if (GUILayout.Button("CW: West → North → East → South", GUILayout.Height(22)))
        {
            ReorderThreats(flythrough, new string[] { "West", "North", "East", "South" });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CCW: East → North → West → South", GUILayout.Height(22)))
        {
            ReorderThreats(flythrough, new string[] { "East", "North", "West", "South" });
        }
        if (GUILayout.Button("CCW: South → East → North → West", GUILayout.Height(22)))
        {
            ReorderThreats(flythrough, new string[] { "South", "East", "North", "West" });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Shots & Waypoints Sequence", EditorStyles.boldLabel);

        if (flythrough.waypoints != null && flythrough.waypoints.Count > 0)
        {
            for (int i = 0; i < flythrough.waypoints.Count; i++)
            {
                var wp = flythrough.waypoints[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                string label = $"Shot {i + 1}: {(wp.point != null ? wp.point.name : "Missing Transform")}";
                if (!string.IsNullOrEmpty(wp.title)) label += $" ({wp.title})";
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                // Up / Down reorder buttons (trackpad friendly!)
                GUI.enabled = (i > 0);
                if (GUILayout.Button("▲ Up", EditorStyles.miniButtonLeft, GUILayout.Width(46)))
                {
                    Undo.RecordObject(flythrough, "Move Shot Up");
                    var temp = flythrough.waypoints[i];
                    flythrough.waypoints[i] = flythrough.waypoints[i - 1];
                    flythrough.waypoints[i - 1] = temp;
                    EditorUtility.SetDirty(flythrough);
                }
                GUI.enabled = (i < flythrough.waypoints.Count - 1);
                if (GUILayout.Button("▼ Down", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                {
                    Undo.RecordObject(flythrough, "Move Shot Down");
                    var temp = flythrough.waypoints[i];
                    flythrough.waypoints[i] = flythrough.waypoints[i + 1];
                    flythrough.waypoints[i + 1] = temp;
                    EditorUtility.SetDirty(flythrough);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                if (i > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    bool isStraight = wp.straightTransition;
                    string modeLabel = isStraight ? "📏 Motion: Straight Linear Dolly (Smooth Easing)" : "〰️ Motion: Curved Spline Arc";
                    if (isStraight) GUI.contentColor = new Color(0.3f, 1f, 0.4f);
                    bool newStraight = EditorGUILayout.ToggleLeft(modeLabel, isStraight, EditorStyles.boldLabel);
                    GUI.contentColor = Color.white;

                    if (newStraight != isStraight)
                    {
                        Undo.RecordObject(flythrough, "Toggle Straight Transition");
                        wp.straightTransition = newStraight;
                        EditorUtility.SetDirty(flythrough);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();

                // 1. Look Through in Scene View
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("👁️ Scene View", GUILayout.Height(24)))
                {
                    if (wp.point != null)
                    {
                        LookThroughInSceneView(wp.point);
                        Selection.activeGameObject = wp.point.gameObject;
                    }
                }

                // 2. Preview in Game View (Move Main Camera)
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                if (GUILayout.Button("🎥 Game View", GUILayout.Height(24)))
                {
                    if (wp.point != null)
                    {
                        SnapMainCameraTo(wp.point.position, wp.point.rotation);
                    }
                }

                // 3. Snap Waypoint from Current Scene View
                GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
                if (GUILayout.Button("📌 Align from Scene", GUILayout.Height(24)))
                {
                    if (wp.point != null)
                    {
                        AlignWaypointFromSceneView(wp.point);
                    }
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                // No-Mouse Quick Aim & Nudge Bar
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🎯 Aim: House", EditorStyles.miniButtonLeft))
                {
                    if (wp.point != null) AimWaypointAt(wp.point, new Vector3(0f, 1.5f, 0f));
                }
                if (GUILayout.Button("🎯 Aim: East Road", EditorStyles.miniButtonMid))
                {
                    if (wp.point != null) AimWaypointAt(wp.point, new Vector3(34f, 1f, 0f));
                }
                if (GUILayout.Button("🎯 Aim: Player", EditorStyles.miniButtonRight))
                {
                    if (wp.point != null) AimWaypointAtPlayer(wp.point);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("↰ Left 15°", EditorStyles.miniButtonLeft))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, Vector3.zero, 0f, -15f);
                }
                if (GUILayout.Button("Right 15° ↱", EditorStyles.miniButtonMid))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, Vector3.zero, 0f, 15f);
                }
                if (GUILayout.Button("⇧ Tilt Up", EditorStyles.miniButtonMid))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, Vector3.zero, -10f, 0f);
                }
                if (GUILayout.Button("Tilt Down ⇩", EditorStyles.miniButtonRight))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, Vector3.zero, 10f, 0f);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("⬆ Height +2m", EditorStyles.miniButtonLeft))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, new Vector3(0f, 2f, 0f), 0f, 0f);
                }
                if (GUILayout.Button("⬇ Height -2m", EditorStyles.miniButtonMid))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, new Vector3(0f, -2f, 0f), 0f, 0f);
                }
                if (GUILayout.Button("🔍 Closer +3m", EditorStyles.miniButtonMid))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, wp.point.forward * 3f, 0f, 0f, isLocalMove: false);
                }
                if (GUILayout.Button("Back -3m 🔎", EditorStyles.miniButtonRight))
                {
                    if (wp.point != null) NudgeWaypoint(wp.point, -wp.point.forward * 3f, 0f, 0f, isLocalMove: false);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.Space(10);

        // Add new waypoint button
        GUI.backgroundColor = new Color(0.85f, 0.85f, 1f);
        if (GUILayout.Button("[+] Add New Waypoint from Current Scene View", GUILayout.Height(30)))
        {
            AddWaypointFromSceneView(flythrough);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Flight Scrubber (Real-time Timeline)", EditorStyles.boldLabel);

        // Toggle for live Game View sync
        syncGameView = EditorGUILayout.ToggleLeft("Live Game View Preview (Move Main Camera with Scrubber)", syncGameView);

        EditorGUI.BeginChangeCheck();
        previewScrub = EditorGUILayout.Slider("Flight Scrubber (0 -> 1)", previewScrub, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            ScrubFlight(flythrough, previewScrub);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Restore Main Camera", GUILayout.Height(24)))
        {
            RestoreMainCamera();
        }

        if (GUILayout.Button("Reset Scene View", GUILayout.Height(24)))
        {
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI()
    {
        MapIntroFlythrough flythrough = (MapIntroFlythrough)target;
        if (flythrough.waypoints == null || flythrough.waypoints.Count < 2) return;

        // 1. Draw smooth spline path
        int samples = 60;
        Vector3[] linePoints = new Vector3[samples + 1];
        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            linePoints[i] = flythrough.SampleSpline(t);
        }

        Handles.color = new Color(1f, 0.85f, 0.2f, 0.9f); // bright gold path
        Handles.DrawAAPolyLine(4f, linePoints);

        // 2. Draw waypoint markers and camera cones
        for (int i = 0; i < flythrough.waypoints.Count; i++)
        {
            var wp = flythrough.waypoints[i];
            if (wp.point == null) continue;

            Vector3 pos = wp.point.position;
            Quaternion rot = wp.point.rotation;

            // Waypoint node sphere
            Handles.color = (i == 0) ? Color.green : (i == flythrough.waypoints.Count - 1 ? Color.red : Color.cyan);
            Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.8f, EventType.Repaint);

            // Viewing direction arrow
            Handles.color = Color.yellow;
            Handles.ArrowHandleCap(0, pos, rot, 2.5f, EventType.Repaint);

            // Label
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;

            string titleStr = !string.IsNullOrEmpty(wp.title) ? $" - {wp.title}" : "";
            Handles.Label(pos + Vector3.up * 1.6f, $"Shot {i + 1}: {wp.shotName}{titleStr}", labelStyle);
        }
    }

    private void LookThroughInSceneView(Transform targetTransform)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return;

        view.in2DMode = false;
        view.rotation = targetTransform.rotation;
        view.pivot = targetTransform.position + targetTransform.forward * 0.001f;
        view.size = 0.001f;
        view.Repaint();
    }

    private void SnapMainCameraTo(Vector3 pos, Quaternion rot)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[MapIntroFlythroughEditor] No Camera tagged 'MainCamera' found.");
            return;
        }

        CacheOriginalCamera(mainCam);

        Undo.RecordObject(mainCam.transform, "Preview Intro Waypoint");
        mainCam.transform.position = pos;
        mainCam.transform.rotation = rot;

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void CacheOriginalCamera(Camera mainCam)
    {
        if (!cachedCamOriginal && mainCam != null)
        {
            originalCamPos = mainCam.transform.position;
            originalCamRot = mainCam.transform.rotation;
            cachedCamOriginal = true;
        }
    }

    private void RestoreMainCamera()
    {
        if (!cachedCamOriginal) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Undo.RecordObject(mainCam.transform, "Restore Camera Position");
            mainCam.transform.position = originalCamPos;
            mainCam.transform.rotation = originalCamRot;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        cachedCamOriginal = false;
    }

    private void AlignWaypointFromSceneView(Transform wpTransform)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null) return;

        Undo.RecordObject(wpTransform, "Align Waypoint from Scene");
        wpTransform.position = view.camera.transform.position;
        wpTransform.rotation = view.camera.transform.rotation;
        EditorUtility.SetDirty(wpTransform);
        Debug.Log($"[MapIntroFlythroughEditor] Updated {wpTransform.name} from Scene View camera.");
    }

    private void AddWaypointFromSceneView(MapIntroFlythrough flythrough)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
        {
            Debug.LogWarning("[MapIntroFlythroughEditor] No active SceneView found!");
            return;
        }

        Undo.RecordObject(flythrough, "Add Waypoint");

        GameObject wpObj = new GameObject($"Waypoint_{flythrough.waypoints.Count + 1}");
        wpObj.transform.SetParent(flythrough.transform, false);
        wpObj.transform.position = view.camera.transform.position;
        wpObj.transform.rotation = view.camera.transform.rotation;

        Undo.RegisterCreatedObjectUndo(wpObj, "Create Waypoint GameObject");

        var newWp = new MapIntroFlythrough.WaypointData
        {
            shotName = $"Shot {flythrough.waypoints.Count + 1}",
            point = wpObj.transform,
            title = $"SHOT {flythrough.waypoints.Count + 1}",
            subtitle = "Cinematic View",
            duration = 2.5f
        };

        flythrough.waypoints.Add(newWp);
        EditorUtility.SetDirty(flythrough);

        Selection.activeGameObject = wpObj;
        Debug.Log($"[MapIntroFlythroughEditor] Added {wpObj.name} at current scene camera view.");
    }

    private void ScrubFlight(MapIntroFlythrough flythrough, float t)
    {
        Vector3 pos = flythrough.SampleSpline(t);
        Quaternion rot = flythrough.SampleSplineRotation(t);

        // Update Scene View camera
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.rotation = rot;
            view.pivot = pos + (rot * Vector3.forward * 0.001f);
            view.size = 0.001f;
            view.Repaint();
        }

        // Optionally update Main Camera for real-time Game View preview
        if (syncGameView)
        {
            SnapMainCameraTo(pos, rot);
        }
    }

    private void AimWaypointAt(Transform wp, Vector3 targetPos)
    {
        Undo.RecordObject(wp, "Aim Waypoint");
        wp.LookAt(targetPos);
        EditorUtility.SetDirty(wp);
        LookThroughInSceneView(wp);
        Debug.Log($"[MapIntroFlythroughEditor] Aimed {wp.name} at {targetPos}.");
    }

    private void AimWaypointAtPlayer(Transform wp)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        Vector3 target = player != null ? player.transform.position + Vector3.up * 1.5f : new Vector3(-4.5f, 1.5f, -4.5f);
        AimWaypointAt(wp, target);
    }

    private void NudgeWaypoint(Transform wp, Vector3 deltaPos, float deltaPitch, float deltaYaw, bool isLocalMove = true)
    {
        Undo.RecordObject(wp, "Nudge Waypoint");
        if (deltaPos != Vector3.zero)
        {
            if (isLocalMove) wp.position += wp.TransformDirection(deltaPos);
            else wp.position += deltaPos;
        }

        if (deltaPitch != 0f || deltaYaw != 0f)
        {
            Vector3 euler = wp.eulerAngles;
            euler.x += deltaPitch;
            euler.y += deltaYaw;
            wp.eulerAngles = euler;
        }

        EditorUtility.SetDirty(wp);
        LookThroughInSceneView(wp);
    }

    private void ReorderThreats(MapIntroFlythrough flythrough, string[] directions)
    {
        if (flythrough == null || flythrough.waypoints == null) return;
        Undo.RecordObject(flythrough, "Reorder Threat Routes");

        // Find existing threat indices in the sequence
        var threatIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < flythrough.waypoints.Count; i++)
        {
            var wp = flythrough.waypoints[i];
            string name = (wp.shotName + " " + (wp.point != null ? wp.point.name : "")).ToLower();
            if (name.Contains("threat"))
            {
                threatIndices.Add(i);
            }
        }

        if (threatIndices.Count != directions.Length)
        {
            Debug.LogWarning($"[MapIntroFlythroughEditor] Found {threatIndices.Count} threat shots, expected {directions.Length}.");
            return;
        }

        // Collect existing threat waypoints
        var threatWps = new System.Collections.Generic.List<MapIntroFlythrough.WaypointData>();
        foreach (int idx in threatIndices)
        {
            threatWps.Add(flythrough.waypoints[idx]);
        }

        // Reassign into the sequence matching the requested directions
        for (int d = 0; d < directions.Length; d++)
        {
            string targetDir = directions[d].ToLower();
            var match = threatWps.Find(w => {
                string str = (w.shotName + " " + (w.point != null ? w.point.name : "")).ToLower();
                return str.Contains(targetDir);
            });

            if (match != null)
            {
                int targetSlot = threatIndices[d];
                string dirUpper = directions[d].ToUpper();
                match.shotName = $"Threat: {directions[d]} Road";
                match.title = $"THREAT ROUTE {d + 1}: {dirUpper} ROAD";
                flythrough.waypoints[targetSlot] = match;
            }
        }

        EditorUtility.SetDirty(flythrough);
        Debug.Log($"[MapIntroFlythroughEditor] Reordered threats to: {string.Join(" -> ", directions)}");
    }
}
#endif
