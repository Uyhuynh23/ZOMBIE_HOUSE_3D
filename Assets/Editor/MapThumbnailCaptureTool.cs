using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Captures real map imagery into the transparent thumbnail wells used by the
/// multiplayer lobby cards. All-map capture uses each scene's Main Camera;
/// active-scene capture uses the current Scene view composition.
/// </summary>
public static class MapThumbnailCaptureTool
{
    private const int Width = 1024;
    private const int Height = 768;
    private const string OutputRoot = "Assets/UI/MultiplayerLobby/Generated";

    private static readonly (string Scene, string Output)[] Maps =
    {
        ("Assets/Scenes/GameScenes/Map_Cloudy.unity", "Map_Cloudy.png"),
        ("Assets/Scenes/GameScenes/Map_Day.unity", "Map_Day.png"),
        ("Assets/Scenes/GameScenes/Map_Night.unity", "Map_Night.png")
    };

    [MenuItem("Tools/Multiplayer/Map Thumbnails/Capture All From Map Cameras")]
    public static void CaptureAll()
    {
        Directory.CreateDirectory(OutputRoot);
        Scene original = SceneManager.GetActiveScene();
        string originalPath = original.path;
        if (original.isDirty) EditorSceneManager.SaveScene(original);

        foreach ((string scenePath, string outputName) in Maps)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera camera = FindCaptureCamera();
            if (camera == null)
                throw new MissingReferenceException($"{scene.name} has no camera available for thumbnail capture.");
            CaptureEstablishingView(camera, $"{OutputRoot}/{outputName}");
        }

        if (!string.IsNullOrWhiteSpace(originalPath))
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        AssetDatabase.Refresh();
        foreach ((_, string outputName) in Maps) ConfigureSprite($"{OutputRoot}/{outputName}");
        AssetDatabase.SaveAssets();
        Debug.Log($"[LobbyUI] Captured {Maps.Length} real map thumbnails at {Width}x{Height} into {OutputRoot}.");
    }

    [MenuItem("Tools/Multiplayer/Map Thumbnails/Capture Active Scene From Scene View")]
    public static void CaptureActiveFromSceneView()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrWhiteSpace(scene.path))
            throw new System.InvalidOperationException("Save the active map scene before capturing it.");
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null || view.camera == null)
            throw new MissingReferenceException("Open a Scene view and compose the desired thumbnail first.");

        Directory.CreateDirectory(OutputRoot);
        string output = $"{OutputRoot}/{scene.name}.png";
        GameObject temporary = new GameObject("ThumbnailCaptureCamera");
        try
        {
            Camera camera = temporary.AddComponent<Camera>();
            camera.CopyFrom(view.camera);
            camera.transform.SetPositionAndRotation(view.camera.transform.position, view.camera.transform.rotation);
            Capture(camera, output);
        }
        finally
        {
            Object.DestroyImmediate(temporary);
        }
        AssetDatabase.Refresh();
        ConfigureSprite(output);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LobbyUI] Captured {scene.name} from Scene view to {output}.");
    }

    private static Camera FindCaptureCamera()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera camera in cameras)
            if (camera.CompareTag("MainCamera")) return camera;
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static void CaptureEstablishingView(Camera camera, string assetPath)
    {
        Vector3 previousPosition = camera.transform.position;
        Quaternion previousRotation = camera.transform.rotation;
        MapIntroFlythrough intro = Object.FindFirstObjectByType<MapIntroFlythrough>();
        try
        {
            if (intro != null && intro.waypoints != null && intro.waypoints.Count > 0 &&
                intro.waypoints[0] != null && intro.waypoints[0].point != null)
            {
                camera.transform.SetPositionAndRotation(
                    intro.waypoints[0].point.position,
                    intro.waypoints[0].point.rotation);
            }
            Capture(camera, assetPath);
        }
        finally
        {
            camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
        }
    }

    private static void Capture(Camera camera, string assetPath)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        float previousAspect = camera.aspect;
        RenderTexture renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        try
        {
            camera.aspect = (float)Width / Height;
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply(false, false);
            File.WriteAllBytes(assetPath, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(image);
        }
    }

    private static void ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }
}
