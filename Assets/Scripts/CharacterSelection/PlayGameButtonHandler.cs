using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the "Play game" button on MainPanel.
/// Opens the Map Selection Panel.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Button))]
public class PlayGameButtonHandler : MonoBehaviour
{
    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick = new Button.ButtonClickedEvent();
        _btn.onClick.AddListener(OnClick);
        Debug.Log($"[PlayGameButtonHandler] Awake: {gameObject.name} wired to ShowMapSelection.");
    }

    private void OnClick()
    {
        Debug.Log($"[PlayGameButtonHandler] CLICKED '{gameObject.name}' -> ShowMapSelection()");

        MainMenuManager mmm = Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm != null)
        {
            mmm.ShowMapSelection();
        }
        else
        {
            Debug.LogError("[PlayGameButtonHandler] MainMenuManager not found in scene!");
        }
    }
}
