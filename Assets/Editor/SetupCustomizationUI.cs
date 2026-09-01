using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SetupCustomizationUI : EditorWindow
{
    [MenuItem("Tools/Setup Customization UI")]
    public static void ShowWindow()
    {
        GetWindow<SetupCustomizationUI>("Setup Customization UI");
    }

    private void OnGUI()
    {
        GUILayout.Label("Update Character Setting UI Hierarchy", EditorStyles.boldLabel);
        GUILayout.Space(5);
        GUILayout.Label("Creates or updates the PortraitRenderer and UI templates.", EditorStyles.wordWrappedMiniLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Update Hierarchy & 3D Portrait Renderer", GUILayout.Height(30)))
        {
            UpdateHierarchy();
        }
    }

    private void UpdateHierarchy()
    {
        CharacterSettingUI ui = FindObjectOfType<CharacterSettingUI>(true);
        if (ui == null)
        {
            Debug.LogError("Could not find CharacterSettingUI in the scene.");
            return;
        }

        Undo.RecordObject(ui.gameObject, "Update UI Hierarchy");

        // 1. Setup PortraitRenderer in the scene if missing
        PortraitRenderer pr = FindObjectOfType<PortraitRenderer>(true);
        if (pr == null)
        {
            GameObject prObj = new GameObject("PortraitRenderer");
            prObj.transform.position = new Vector3(1000f, 1000f, 1000f);
            pr = prObj.AddComponent<PortraitRenderer>();
            pr.InitializeRenderer();
            Undo.RegisterCreatedObjectUndo(prObj, "Create PortraitRenderer");
        }
        else
        {
            pr.InitializeRenderer();
        }
        ui.portraitRenderer = pr;

        // 2. Configure Left Panel Character Template
        if (ui.characterCardTemplate != null)
        {
            if (ui.characterCardTemplate.GetComponent<Image>() == null)
            {
                Undo.AddComponent<Image>(ui.characterCardTemplate);
            }

            // Ensure Portrait child exists on characterCardTemplate
            Transform portraitTrans = ui.characterCardTemplate.transform.Find("Portrait");
            if (portraitTrans == null)
            {
                GameObject pObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                pObj.transform.SetParent(ui.characterCardTemplate.transform, false);
                pObj.transform.SetAsFirstSibling();
                
                RectTransform pRect = pObj.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0.05f, 0.1f);
                pRect.anchorMax = new Vector2(0.35f, 0.9f);
                pRect.anchoredPosition = Vector2.zero;
                pRect.sizeDelta = Vector2.zero;

                Image pImg = pObj.GetComponent<Image>();
                pImg.preserveAspect = true;
                
                Undo.RegisterCreatedObjectUndo(pObj, "Create Character Card Portrait");
            }
            else
            {
                portraitTrans.gameObject.SetActive(true);
            }
        }

        // 3. Configure Right Panel Equipment Template
        if (ui.equipmentButtonTemplate != null)
        {
            if (ui.equipmentButtonTemplate.GetComponent<Image>() == null)
            {
                Undo.AddComponent<Image>(ui.equipmentButtonTemplate);
            }

            Transform iconTrans = ui.equipmentButtonTemplate.transform.Find("Icon");
            if (iconTrans == null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(ui.equipmentButtonTemplate.transform, false);
                
                RectTransform iRect = iconObj.GetComponent<RectTransform>();
                iRect.anchorMin = new Vector2(0.1f, 0.1f);
                iRect.anchorMax = new Vector2(0.9f, 0.9f);
                iRect.anchoredPosition = Vector2.zero;
                iRect.sizeDelta = Vector2.zero;

                Image iImg = iconObj.GetComponent<Image>();
                iImg.preserveAspect = true;

                Undo.RegisterCreatedObjectUndo(iconObj, "Create Equipment Icon");
            }
        }

        EditorUtility.SetDirty(ui);
        if (pr != null) EditorUtility.SetDirty(pr);

        Debug.Log("Customization UI & 3D Portrait Renderer setup completed successfully!");
    }
}
