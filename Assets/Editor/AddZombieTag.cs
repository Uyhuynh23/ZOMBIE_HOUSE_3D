using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AddZombieTag
{
    static AddZombieTag()
    {
        EditorApplication.delayCall += AddTag;
    }

    static void AddTag()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) return;

        var so = new SerializedObject(asset[0]);
        var tags = so.FindProperty("tags");
        
        bool found = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "Zombie")
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Zombie";
            so.ApplyModifiedProperties();
            so.Update();
            Debug.Log("Automatically added 'Zombie' tag to TagManager.");
        }
    }
}
