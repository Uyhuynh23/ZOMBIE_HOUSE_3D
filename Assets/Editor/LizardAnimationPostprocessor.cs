using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LizardAnimationPostprocessor : AssetPostprocessor
{
    private void OnPreprocessAnimation()
    {
        if (string.IsNullOrEmpty(assetPath)) return;
        if (!assetPath.Replace('\\', '/').Contains("Hatogame_new/Lizard/Animation")) return;

        ModelImporter modelImporter = assetImporter as ModelImporter;
        if (modelImporter == null) return;

        string lowerPath = assetPath.ToLowerInvariant();
        bool shouldLoop = lowerPath.Contains("walk") || lowerPath.Contains("idle") || lowerPath.Contains("run");

        if (shouldLoop)
        {
            ModelImporterClipAnimation[] clips = modelImporter.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = true;
                }
                modelImporter.clipAnimations = clips;
            }
        }
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.EndsWith("Lizard_Controller.controller", StringComparison.OrdinalIgnoreCase))
            {
                SanitizeLizardController(path);
            }
        }
    }

    private static void SanitizeLizardController(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;
            string text = File.ReadAllText(filePath);

            string[] badBlocks = new string[]
            {
                "-7933603769539795471",
                "-5104857276024824291",
                "4455504528743998485",
                "8855778827961866949",
                "8955370479146098367"
            };

            bool needsClean = false;
            foreach (string badId in badBlocks)
            {
                if (text.Contains(badId))
                {
                    needsClean = true;
                    break;
                }
            }

            if (!needsClean) return;

            string[] docs = text.Split(new string[] { "--- !u!" }, StringSplitOptions.None);
            string header = docs[0];
            var cleanDocs = new System.Collections.Generic.List<string>();

            for (int i = 1; i < docs.Length; i++)
            {
                bool isBad = false;
                foreach (string badId in badBlocks)
                {
                    if (docs[i].Contains(badId))
                    {
                        isBad = true;
                        break;
                    }
                }
                if (!isBad)
                {
                    cleanDocs.Add(docs[i]);
                }
            }

            string cleaned = header + "--- !u!" + string.Join("--- !u!", cleanDocs);
            File.WriteAllText(filePath, cleaned);
            Debug.Log($"[LizardAnimationPostprocessor] Automatically sanitized broken transitions in {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LizardAnimationPostprocessor] Could not sanitize {filePath}: {ex.Message}");
        }
    }
}
