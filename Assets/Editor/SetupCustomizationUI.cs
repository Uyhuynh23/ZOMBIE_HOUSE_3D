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
        if (GUILayout.Button("Update Hierarchy"))
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

        if (ui.weaponsGridContainer != null)
        {
            // Fix WeaponsGrid RectTransform to take top half of RightPanel
            RectTransform wGridRect = ui.weaponsGridContainer.GetComponent<RectTransform>();
            wGridRect.anchorMin = new Vector2(0.15f, 0.5f);
            wGridRect.anchorMax = new Vector2(0.85f, 0.8f);
            wGridRect.anchoredPosition = Vector2.zero;
            wGridRect.sizeDelta = Vector2.zero;

            CreateCarouselControls("Weapon", ui.weaponsGridContainer, out Button wPrev, out Button wNext, 0.65f);
            ui.weaponPrevBtn = wPrev;
            ui.weaponNextBtn = wNext;

            GridLayoutGroup grid = ui.weaponsGridContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.cellSize = new Vector2(80, 80);
                grid.spacing = new Vector2(10, 10);
            }
            
            // Try to find missing title for Weapons
            Transform wTitle = ui.weaponsGridContainer.parent.Find("W_Title");
            if (wTitle != null)
            {
                RectTransform tRect = wTitle.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 0.85f);
                tRect.anchorMax = new Vector2(1, 0.95f);
                tRect.anchoredPosition = Vector2.zero;
                tRect.sizeDelta = Vector2.zero;
                
                Text tTxt = wTitle.GetComponent<Text>();
                if (tTxt != null) tTxt.text = "WEAPONS";
            }
        }

        if (ui.shieldsGridContainer != null)
        {
            // Fix ShieldsGrid RectTransform to take bottom half
            RectTransform sGridRect = ui.shieldsGridContainer.GetComponent<RectTransform>();
            sGridRect.anchorMin = new Vector2(0.15f, 0.1f);
            sGridRect.anchorMax = new Vector2(0.85f, 0.4f);
            sGridRect.anchoredPosition = Vector2.zero;
            sGridRect.sizeDelta = Vector2.zero;

            CreateCarouselControls("Shield", ui.shieldsGridContainer, out Button sPrev, out Button sNext, 0.25f);
            ui.shieldPrevBtn = sPrev;
            ui.shieldNextBtn = sNext;

            GridLayoutGroup grid = ui.shieldsGridContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.cellSize = new Vector2(80, 80);
                grid.spacing = new Vector2(10, 10);
            }
            
            // Title for Shields
            Transform sTitle = ui.shieldsGridContainer.parent.Find("S_Title");
            if (sTitle != null)
            {
                RectTransform tRect = sTitle.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0, 0.45f);
                tRect.anchorMax = new Vector2(1, 0.55f);
                tRect.anchoredPosition = Vector2.zero;
                tRect.sizeDelta = Vector2.zero;
            }
        }

        // Left Panel Setup
        if (ui.characterCardTemplate != null)
        {
            if (ui.characterCardTemplate.GetComponent<Image>() == null)
            {
                Undo.AddComponent<Image>(ui.characterCardTemplate);
            }
            
            Transform portrait = ui.characterCardTemplate.transform.Find("Portrait");
            if (portrait != null)
            {
                portrait.gameObject.SetActive(false);
            }
        }

        if (ui.equipmentButtonTemplate != null)
        {
            if (ui.equipmentButtonTemplate.GetComponent<Image>() == null)
            {
                Undo.AddComponent<Image>(ui.equipmentButtonTemplate);
            }
        }

        EditorUtility.SetDirty(ui);
        Debug.Log("Customization UI Hierarchy updated successfully. Layout has been fixed for weapons and arrows.");
    }

    private void CreateCarouselControls(string prefix, Transform container, out Button prevBtn, out Button nextBtn, float yAnchor)
    {
        Transform parent = container.parent;

        Transform prev = parent.Find(prefix + "PrevBtn");
        if (prev == null)
        {
            GameObject prevObj = new GameObject(prefix + "PrevBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            prevObj.transform.SetParent(parent, false);
            prevBtn = prevObj.GetComponent<Button>();
            
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(prevObj.transform, false);
            Text txt = txtObj.GetComponent<Text>();
            txt.text = "<";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            
            Undo.RegisterCreatedObjectUndo(prevObj, "Create Prev Btn");
        }
        else
        {
            prevBtn = prev.GetComponent<Button>();
        }

        Transform next = parent.Find(prefix + "NextBtn");
        if (next == null)
        {
            GameObject nextObj = new GameObject(prefix + "NextBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            nextObj.transform.SetParent(parent, false);
            nextBtn = nextObj.GetComponent<Button>();
            
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(nextObj.transform, false);
            Text txt = txtObj.GetComponent<Text>();
            txt.text = ">";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;

            Undo.RegisterCreatedObjectUndo(nextObj, "Create Next Btn");
        }
        else
        {
            nextBtn = next.GetComponent<Button>();
        }

        // Anchor the buttons relative to the RightPanel, properly positioned to the left and right of the grid
        RectTransform prevRect = prevBtn.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0.02f, yAnchor);
        prevRect.anchorMax = new Vector2(0.12f, yAnchor);
        prevRect.anchoredPosition = Vector2.zero;
        prevRect.sizeDelta = new Vector2(0, 40);

        RectTransform nextRect = nextBtn.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.88f, yAnchor);
        nextRect.anchorMax = new Vector2(0.98f, yAnchor);
        nextRect.anchoredPosition = Vector2.zero;
        nextRect.sizeDelta = new Vector2(0, 40);
    }
}
