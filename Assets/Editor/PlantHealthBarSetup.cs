using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility to configure health bars on plant prefabs (PeaShooter, PeaShooterFroze, Sunflower),
/// mirroring the architecture and visual style of the Spider prefab's health bar.
/// </summary>
[InitializeOnLoad]
public static class PlantHealthBarSetup
{
    private const string PeaShooterPath      = "Assets/Prefabs/PeaShooter.prefab";
    private const string PeaShooterFrozePath = "Assets/Prefabs/PeaShooterFroze.prefab";
    private const string SunflowerPath       = "Assets/Prefabs/Sunflower.prefab";
    private const string WallnutPath         = "Assets/Prefabs/Wallnut.prefab";

    static PlantHealthBarSetup()
    {
        EditorApplication.delayCall += EnsureAllPlantHealthBars;
    }

    [MenuItem("Zombie House/Setup Plant Health Bars")]
    public static void SetupFromMenu()
    {
        SetupPrefab(PeaShooterPath, new Vector3(0f, 0f, 0f), 0.5f);
        SetupPrefab(PeaShooterFrozePath, new Vector3(0f, 0f, 0f), 0.5f);
        SetupPrefab(SunflowerPath, new Vector3(0f, 0f, 0f), 0.5f);
        SetupPrefab(WallnutPath, new Vector3(0f, 0f, 0f), 0.5f);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PlantHealthBarSetup] All plant health bars successfully set up.");
    }

    public static void EnsureAllPlantHealthBars()
    {
        bool changed = false;
        changed |= EnsurePrefab(PeaShooterPath, new Vector3(0f, 0f, 0f), 0.5f);
        changed |= EnsurePrefab(PeaShooterFrozePath, new Vector3(0f, 0f, 0f), 0.5f);
        changed |= EnsurePrefab(SunflowerPath, new Vector3(0f, 0f, 0f), 0.5f);
        changed |= EnsurePrefab(WallnutPath, new Vector3(0f, 0f, 0f), 0.5f);

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlantHealthBarSetup] Plant prefabs updated with health bars.");
        }
    }

    private static bool EnsurePrefab(string path, Vector3 offset, float heightAbovePlant)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) return false;

        PlantHealthBar bar = asset.GetComponent<PlantHealthBar>();
        Transform barChild = asset.transform.Find("Plant Health Bar");
        PlantBase plantBase = asset.GetComponent<PlantBase>();

        int expectedHealth = path.Contains("Wallnut") ? 400 : 100;
        bool needsSetup = bar == null || barChild == null ||
                          (plantBase != null && (plantBase.maxHealth != expectedHealth || plantBase.currentHealth != expectedHealth));

        if (needsSetup)
        {
            SetupPrefab(path, offset, heightAbovePlant);
            return true;
        }

        return false;
    }

    public static void SetupPrefab(string path, Vector3 worldOffset, float heightAbovePlant = 0.5f)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"[PlantHealthBarSetup] Failed to load prefab contents at {path}");
            return;
        }

        try
        {
            // 1. Configure PlantBase health fields
            PlantBase plant = root.GetComponent<PlantBase>();
            int expectedHealth = path.Contains("Wallnut") ? 400 : 100;
            if (plant != null)
            {
                plant.maxHealth = expectedHealth;
                plant.currentHealth = expectedHealth;
            }

            // 2. Attach and configure PlantHealthBar component on root
            PlantHealthBar healthBar = root.GetComponent<PlantHealthBar>();
            if (healthBar == null)
                healthBar = root.AddComponent<PlantHealthBar>();

            SerializedObject soBar = new SerializedObject(healthBar);
            SerializedProperty propHeight = soBar.FindProperty("heightAbovePlant");
            if (propHeight != null) propHeight.floatValue = heightAbovePlant;
            SerializedProperty propOffset = soBar.FindProperty("worldOffset");
            if (propOffset != null) propOffset.vector3Value = worldOffset;
            SerializedProperty propScale = soBar.FindProperty("worldSpaceScale");
            if (propScale != null) propScale.floatValue = 0.012f;
            SerializedProperty propHideFull = soBar.FindProperty("hideAtFullHealth");
            if (propHideFull != null) propHideFull.boolValue = true;
            SerializedProperty propHideDelay = soBar.FindProperty("hideDelayAfterAttack");
            if (propHideDelay != null) propHideDelay.floatValue = 4.0f;
            soBar.ApplyModifiedPropertiesWithoutUndo();

            // 3. Setup or find the "Plant Health Bar" child GameObject
            Transform barTransform = root.transform.Find("Plant Health Bar");
            GameObject barObject = barTransform != null ? barTransform.gameObject : new GameObject("Plant Health Bar");
            barObject.transform.SetParent(root.transform, false);
            barObject.SetActive(true);

            RectTransform canvasRect = barObject.GetComponent<RectTransform>();
            if (canvasRect == null) canvasRect = barObject.AddComponent<RectTransform>();

            float parentScale = Mathf.Max(0.001f, root.transform.lossyScale.x, root.transform.lossyScale.y, root.transform.lossyScale.z);
            canvasRect.localPosition = new Vector3(0f, 1.25f, 0f);
            canvasRect.localScale = Vector3.one * (0.012f / parentScale);
            canvasRect.sizeDelta = new Vector2(100f, 12f);
            canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);

            Canvas canvas = barObject.GetComponent<Canvas>();
            if (canvas == null) canvas = barObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;

            // 4. Setup "Background" child
            Transform bgTransform = barObject.transform.Find("Background");
            GameObject bgObject = bgTransform != null ? bgTransform.gameObject : new GameObject("Background");
            bgObject.transform.SetParent(barObject.transform, false);

            RectTransform bgRect = bgObject.GetComponent<RectTransform>();
            if (bgRect == null) bgRect = bgObject.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgRect.pivot = new Vector2(0.5f, 0.5f);

            CanvasRenderer bgRenderer = bgObject.GetComponent<CanvasRenderer>();
            if (bgRenderer == null) bgObject.AddComponent<CanvasRenderer>();

            Image bgImage = bgObject.GetComponent<Image>();
            if (bgImage == null) bgImage = bgObject.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.03f, 0.03f, 0.95f);
            bgImage.type = Image.Type.Simple;

            // 5. Setup "Fill" child under Background
            Transform fillTransform = bgObject.transform.Find("Fill");
            GameObject fillObject = fillTransform != null ? fillTransform.gameObject : new GameObject("Fill");
            fillObject.transform.SetParent(bgObject.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            if (fillRect == null) fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            fillRect.sizeDelta = new Vector2(-4f, -4f);
            fillRect.localScale = Vector3.one;

            CanvasRenderer fillRenderer = fillObject.GetComponent<CanvasRenderer>();
            if (fillRenderer == null) fillObject.AddComponent<CanvasRenderer>();

            Image fillImage = fillObject.GetComponent<Image>();
            if (fillImage == null) fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.25f, 0.95f, 0.22f, 1f);
            fillImage.type = Image.Type.Simple;

            barObject.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success)
            {
                Debug.LogError($"[PlantHealthBarSetup] Failed to save prefab at {path}");
            }
            else
            {
                Debug.Log($"[PlantHealthBarSetup] Successfully updated {path}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
