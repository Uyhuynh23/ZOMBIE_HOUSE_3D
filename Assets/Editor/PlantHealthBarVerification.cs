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
        "Assets/Prefabs/Sunflower.prefab",
        "Assets/Prefabs/Wallnut.prefab"
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

            int expectedHealth = path.Contains("Wallnut") ? 400 : 100;
            if (plantBase.maxHealth != expectedHealth || plantBase.currentHealth != expectedHealth)
            {
                output.AppendLine($"[Verification FAIL] {path} health values incorrect: max={plantBase.maxHealth}, current={plantBase.currentHealth} (expected {expectedHealth})");
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
                PlantHealthBar instHealthBar = instance.GetComponent<PlantHealthBar>();
                if (instHealthBar != null)
                {
                    instHealthBar.Initialize();
                }
                Transform instFill = instance.transform.Find("Plant Health Bar/Background/Fill");
                RectTransform instFillRect = instFill as RectTransform;

                // Initial full health check: UX requires health bar to be hidden when full health (only show when attacked)
                if (instHealthBar.IsVisible)
                {
                    output.AppendLine($"[Verification FAIL] {path} health bar should be hidden at full health (only show when attacked)");
                    continue;
                }

                // Simulate 1 zombie attack tick (10 damage)
                instPlant.TakeDamage(10);
                int expectedAfter10 = expectedHealth - 10;
                if (instPlant.currentHealth != expectedAfter10)
                {
                    output.AppendLine($"[Verification FAIL] {path} currentHealth after 10 dmg is {instPlant.currentHealth}, expected {expectedAfter10}");
                    continue;
                }

                // Health bar must now be visible after attack
                if (!instHealthBar.IsVisible)
                {
                    output.AppendLine($"[Verification FAIL] {path} health bar should be visible after being attacked");
                    continue;
                }

                float expectedRatio10 = (float)expectedAfter10 / expectedHealth;
                if (Mathf.Abs(instFillRect.localScale.x - expectedRatio10) > 0.01f)
                {
                    output.AppendLine($"[Verification FAIL] {path} fill scale after 10 dmg is {instFillRect.localScale.x}, expected {expectedRatio10:F2}");
                    continue;
                }

                // Simulate another 30 damage
                instPlant.TakeDamage(30);
                int expectedAfter40 = expectedHealth - 40;
                if (instPlant.currentHealth != expectedAfter40)
                {
                    output.AppendLine($"[Verification FAIL] {path} currentHealth after 40 dmg is {instPlant.currentHealth}, expected {expectedAfter40}");
                    continue;
                }

                float expectedRatio40 = (float)expectedAfter40 / expectedHealth;
                if (Mathf.Abs(instFillRect.localScale.x - expectedRatio40) > 0.01f)
                {
                    output.AppendLine($"[Verification FAIL] {path} fill scale after 40 dmg is {instFillRect.localScale.x}, expected {expectedRatio40:F2}");
                    continue;
                }

                passed++;
                output.AppendLine($"[Verification PASS] {prefab.name}: Structure, components, damage reaction, and UX visibility verified successfully.");
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
