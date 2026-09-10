using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PlantHealthBarVerification
{
    private static readonly string[] PrefabPaths = new string[]
    {
        "Assets/Prefabs/PeaShooter.prefab",
        "Assets/Prefabs/PeaShooterFroze.prefab",
        "Assets/Prefabs/Sunflower.prefab"
    };

    private static readonly string ResultFile = "Temp/health_bar_verification.log";

    static PlantHealthBarVerification()
    {
        EditorApplication.delayCall += () => Verify();
    }

    [MenuItem("Zombie House/Verify Plant Health Bars")]
    public static void Verify()
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine($"=== Plant Health Bar Verification at {DateTime.Now} ===");
        int passed = 0;
        int total = 0;

        foreach (string path in PrefabPaths)
        {
            total++;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                output.AppendLine($"[Verification FAIL] Could not load prefab: {path}");
                continue;
            }

            // 1. Check PlantBase
            PlantBase plantBase = prefab.GetComponent<PlantBase>();
            if (plantBase == null)
            {
                output.AppendLine($"[Verification FAIL] {path} missing PlantBase component");
                continue;
            }

            if (plantBase.maxHealth != 100 || plantBase.currentHealth != 100)
            {
                output.AppendLine($"[Verification FAIL] {path} health values incorrect: max={plantBase.maxHealth}, current={plantBase.currentHealth}");
                continue;
            }

            // 2. Check PlantHealthBar
            PlantHealthBar healthBar = prefab.GetComponent<PlantHealthBar>();
            if (healthBar == null)
            {
                output.AppendLine($"[Verification FAIL] {path} missing PlantHealthBar component");
                continue;
            }

            // 3. Check child hierarchy
            Transform barTransform = prefab.transform.Find("Plant Health Bar");
            if (barTransform == null)
            {
                output.AppendLine($"[Verification FAIL] {path} missing 'Plant Health Bar' child");
                continue;
            }

            Canvas canvas = barTransform.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                output.AppendLine($"[Verification FAIL] {path} canvas missing or not WorldSpace");
                continue;
            }

            Transform bgTransform = barTransform.Find("Background");
            if (bgTransform == null)
            {
                output.AppendLine($"[Verification FAIL] {path} missing 'Background' child");
                continue;
            }

            Transform fillTransform = bgTransform.Find("Fill");
            if (fillTransform == null)
            {
                output.AppendLine($"[Verification FAIL] {path} missing 'Fill' child");
                continue;
            }

            RectTransform fillRect = fillTransform as RectTransform;
            if (fillRect == null || Mathf.Abs(fillRect.pivot.x) > 0.001f)
            {
                output.AppendLine($"[Verification FAIL] {path} Fill RectTransform missing or pivot.x != 0 (pivot={fillRect?.pivot})");
                continue;
            }

            Image fillImage = fillTransform.GetComponent<Image>();
            if (fillImage == null)
            {
                output.AppendLine($"[Verification FAIL] {path} Fill Image missing");
                continue;
            }

            // 4. Runtime simulation test
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                PlantBase instPlant = instance.GetComponent<PlantBase>();
                Transform instFill = instance.transform.Find("Plant Health Bar/Background/Fill");
                RectTransform instFillRect = instFill as RectTransform;

                // Initial full health check
                if (Mathf.Abs(instFillRect.localScale.x - 1f) > 0.01f)
                {
                    output.AppendLine($"[Verification FAIL] {path} instance initial fill scale is not 1.0 (actual: {instFillRect.localScale.x})");
                    continue;
                }

                // Simulate 1 zombie attack tick (10 damage)
                instPlant.TakeDamage(10);
                if (instPlant.currentHealth != 90)
                {
                    output.AppendLine($"[Verification FAIL] {path} currentHealth after 10 dmg is {instPlant.currentHealth}, expected 90");
                    continue;
                }

                if (Mathf.Abs(instFillRect.localScale.x - 0.9f) > 0.01f)
                {
                    output.AppendLine($"[Verification FAIL] {path} fill scale after 10 dmg is {instFillRect.localScale.x}, expected 0.9");
                    continue;
                }

                // Simulate another 30 damage (e.g. 3 more attacks = 60 health left)
                instPlant.TakeDamage(30);
                if (instPlant.currentHealth != 60)
                {
                    output.AppendLine($"[Verification FAIL] {path} currentHealth after 40 dmg is {instPlant.currentHealth}, expected 60");
                    continue;
                }

                if (Mathf.Abs(instFillRect.localScale.x - 0.6f) > 0.01f)
                {
                    output.AppendLine($"[Verification FAIL] {path} fill scale after 40 dmg is {instFillRect.localScale.x}, expected 0.6");
                    continue;
                }

                passed++;
                output.AppendLine($"[Verification PASS] {prefab.name}: Structure, components, and damage reaction verified successfully.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        output.AppendLine($"=== Plant Health Bar Verification Result: {passed}/{total} Passed ===");
        string log = output.ToString();
        Debug.Log(log);
        try
        {
            File.WriteAllText(ResultFile, log);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to write verification result file: " + ex.Message);
        }
    }
}
