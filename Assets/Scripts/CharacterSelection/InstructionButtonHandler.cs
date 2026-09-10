using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the "Instruction" button on MainPanel.
/// Directs user to Map_Tutorial.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Button))]
public class InstructionButtonHandler : MonoBehaviour
{
    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick = new Button.ButtonClickedEvent();
        _btn.onClick.AddListener(OnClick);
        Debug.Log($"[InstructionButtonHandler] Awake: {gameObject.name} wired to StartTutorial.");
    }

    private void OnClick()
    {
        Debug.Log($"[InstructionButtonHandler] CLICKED '{gameObject.name}' -> StartTutorial()");

        MainMenuManager mmm = Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm != null)
        {
            mmm.StartTutorial();
        }
        else
        {
            Debug.LogError("[InstructionButtonHandler] MainMenuManager not found in scene!");
        }
    }
}
