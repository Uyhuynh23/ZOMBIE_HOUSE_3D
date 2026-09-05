using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Watches for a trigger file to automatically rebuild Map_Tutorial and InstructionBoard3D
/// within the active Unity Editor session.
/// </summary>
[InitializeOnLoad]
public static class TutorialMapRebuilder
{
    private const string TriggerFile = "rebuild_tutorial.trigger";
    private const string ResultFile = "rebuild_tutorial.log";

    static TutorialMapRebuilder()
    {
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (!File.Exists(TriggerFile)) return;

        try
        {
            File.Delete(TriggerFile);
            Debug.Log("[TutorialMapRebuilder] Trigger detected. Rebuilding Tutorial Map and 3D Instruction Board...");

            // 1. Rebuild 3D Instruction Board Prefab with large fonts
            InstructionBoardBuilder.BuildPrefab();

            // 2. Rebuild Tutorial Map with large fonts, enlarged easel, and HUD banner
            TutorialSceneBuilder.BuildTutorialMap();

            File.WriteAllText(ResultFile, "SUCCESS: Tutorial Map rebuilt.");
            Debug.Log("[TutorialMapRebuilder] 🎉 Tutorial Map successfully rebuilt!");
        }
        catch (System.Exception ex)
        {
            File.WriteAllText(ResultFile, "ERROR: " + ex.ToString());
            Debug.LogError("[TutorialMapRebuilder] Rebuild failed: " + ex);
        }
    }
}
