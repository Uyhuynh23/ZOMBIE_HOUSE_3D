using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor tool: Tools > Capture Plant Portraits
/// Spawns each plant prefab in the scene, renders it with a temporary orthographic camera,
/// and saves a 128x128 PNG sprite to Assets/UI/PlantPortraits/.
/// </summary>
public class PlantPortraitCapture
{
    private const int TEX_SIZE = 128;
    private const string OUTPUT_DIR = "Assets/UI/PlantPortraits";

    [MenuItem("Tools/Capture Plant Portraits")]
    public static void CaptureAll()
    {
        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);

        // Find all plant prefabs in Assets/Prefabs that have PeashooterCombat or SunflowerLogic
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Only process plant prefabs (those that have PeashooterCombat or SunflowerLogic)
            bool isPlant = prefab.GetComponentInChildren<PeashooterCombat>() != null
                        || prefab.GetComponentInChildren<SunflowerLogic>() != null;
            if (!isPlant) continue;

            CapturePlant(prefab, prefab.name);
        }

        AssetDatabase.Refresh();
        Debug.Log("[PlantPortraitCapture] Done! Portraits saved to " + OUTPUT_DIR);
    }

    static void CapturePlant(GameObject prefab, string plantName)
    {
        // Spawn prefab off-screen
        Vector3 capturePos = new Vector3(1000f, 0f, 1000f);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = capturePos;

        // Calculate bounds of the object
        Bounds bounds = GetRenderBounds(instance);

        // Create render texture
        RenderTexture rt = new RenderTexture(TEX_SIZE, TEX_SIZE, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;

        // Setup camera
        GameObject camGo = new GameObject("_PortraitCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent

        // Position camera to look at the plant from a front-angled view (like PvZ card angle)
        float size = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.4f;
        cam.orthographicSize = size;

        // Slight top-front angle (15 degrees down, like PvZ 2D art perspective)
        Vector3 camDir = Quaternion.Euler(20f, 0f, 0f) * Vector3.back;
        camGo.transform.position = bounds.center - camDir * (size * 3f);
        camGo.transform.LookAt(bounds.center);
        cam.targetTexture = rt;

        cam.Render();

        // Read pixels
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, TEX_SIZE, TEX_SIZE), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // Save PNG
        byte[] bytes = tex.EncodeToPNG();
        string filePath = Path.Combine(OUTPUT_DIR, plantName + ".png");
        File.WriteAllBytes(filePath, bytes);

        // Cleanup
        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);

        Debug.Log("[PlantPortraitCapture] Captured: " + filePath);
    }

    static Bounds GetRenderBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
