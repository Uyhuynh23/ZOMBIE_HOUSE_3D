using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the Character button.
/// Bypasses all onClick persistent listener issues.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Button))]
public class CharacterButtonHandler : MonoBehaviour
{
    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick = new Button.ButtonClickedEvent();
        _btn.onClick.AddListener(OnClick);
        Debug.Log($"[CharacterButtonHandler] Awake: {gameObject.name} wired.");
    }

    private void OnClick()
    {
        Debug.Log($"[CharacterButtonHandler] CLICKED '{gameObject.name}' -> ShowCharacterSetting()");

        MainMenuManager mmm = Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm != null)
        {
            mmm.ShowCharacterSetting();
        }
        else
        {
            Debug.LogError("[CharacterButtonHandler] MainMenuManager not found!");
        }
    }
}
