#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility: Tools -> Capture Map Thumbnails
/// Automatically opens each map scene, captures a high-angle screenshot from the camera,
/// and saves the resulting sprite to Assets/UI/MapThumbnails/.
/// </summary>
public static class MapThumbnailCapture
{
    private const int CaptureWidth = 960;
    private const int CaptureHeight = 540;
    private const string OutputDirectory = "Assets/UI/MapThumbnails";

    private static readonly (string scenePath, string fileName)[] MapScenes = new[]
    {
        ("Assets/Scenes/GameScenes/Map_Day.unity", "Map_Day.png"),
        ("Assets/Scenes/GameScenes/Map_Cloudy.unity", "Map_Cloudy.png"),
        ("Assets/Scenes/GameScenes/Map_Night.unity", "Map_Night.png")
    };

    [MenuItem("Zombie House/Capture Map Thumbnails")]
    public static void CaptureAllMapThumbnails()
    {
        string currentScenePath = SceneManager.GetActiveScene().path;

        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        foreach (var (scenePath, fileName) in MapScenes)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[MapThumbnailCapture] Scene not found: {scenePath}");
                continue;
            }

            Debug.Log($"[MapThumbnailCapture] Opening {scenePath} to capture thumbnail...");
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            CaptureActiveSceneView(Path.Combine(OutputDirectory, fileName));
        }

        AssetDatabase.Refresh();

        // Restore original scene
        if (!string.IsNullOrEmpty(currentScenePath) && File.Exists(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }

        Debug.Log("[MapThumbnailCapture] All map thumbnails captured successfully!");
    }

    private static void CaptureActiveSceneView(string outputPath)
    {
        Camera targetCam = Camera.main;
        bool createdTempCam = false;

        if (targetCam == null)
        {
            targetCam = Object.FindFirstObjectByType<Camera>();
        }

        // If no camera in scene or camera is disabled, create a temporary camera looking at the scene center
        if (targetCam == null)
        {
            GameObject camObj = new GameObject("_CaptureCamera");
            targetCam = camObj.AddComponent<Camera>();
            targetCam.transform.position = new Vector3(0f, 18f, -22f);
            targetCam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            targetCam.fieldOfView = 50f;
            targetCam.clearFlags = CameraClearFlags.Skybox;
            createdTempCam = true;
        }

        RenderTexture rt = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;

        RenderTexture prevRT = targetCam.targetTexture;
        targetCam.targetTexture = rt;
        targetCam.Render();

        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
        screenshot.Apply();

        RenderTexture.active = null;
        targetCam.targetTexture = prevRT;

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(screenshot);

        if (createdTempCam)
        {
            Object.DestroyImmediate(targetCam.gameObject);
        }

        Debug.Log($"[MapThumbnailCapture] Saved map thumbnail: {outputPath}");
    }
}
#endif
