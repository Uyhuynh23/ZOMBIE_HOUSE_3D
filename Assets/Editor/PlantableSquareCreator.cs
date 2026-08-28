using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class PlantableSquareCreator
{
    static PlantableSquareCreator()
    {
        EditorApplication.delayCall += CreatePrefab;
    }

    static void CreatePrefab()
    {
        string path = "Assets/Prefabs/PlantableSquare.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        // Determine correct shader for the project (URP vs Standard)
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        string matPath = "Assets/Materials/BrownGround.mat";
        if (!Directory.Exists("Assets/Materials"))
        {
            Directory.CreateDirectory("Assets/Materials");
        }
        
        Material brownMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (brownMat == null)
        {
            brownMat = new Material(shader);
            brownMat.color = new Color(0.4f, 0.25f, 0.13f); // Brown
            
            // URP uses _BaseColor instead of _Color
            if (shader.name.Contains("Universal"))
            {
                brownMat.SetColor("_BaseColor", new Color(0.4f, 0.25f, 0.13f));
            }
            AssetDatabase.CreateAsset(brownMat, matPath);
        }

        // Create the object
        GameObject square = GameObject.CreatePrimitive(PrimitiveType.Cube);
        square.name = "PlantableSquare";
        square.tag = "PlantableNode";
        square.transform.localScale = new Vector3(2f, 0.1f, 2f);

        // Add custom component
        square.AddComponent<PlantableSquare>();
        
        // Assign material
        square.GetComponent<MeshRenderer>().sharedMaterial = brownMat;

        // Save as prefab
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }
        
        PrefabUtility.SaveAsPrefabAsset(square, path);
        GameObject.DestroyImmediate(square);
        
        Debug.Log("Created PlantableSquare.prefab successfully with shader: " + shader.name);
    }
}
