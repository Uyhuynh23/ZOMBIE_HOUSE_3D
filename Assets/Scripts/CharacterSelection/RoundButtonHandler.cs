using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script directly to each Round button.
/// Set roundNumber (1, 2, or 3) in the Inspector.
/// Runs after MainMenuManager due to Script Execution Order.
/// </summary>
[DefaultExecutionOrder(100)]   // Run AFTER default (0) scripts like MainMenuManager
[RequireComponent(typeof(Button))]
public class RoundButtonHandler : MonoBehaviour
{
    [Tooltip("1 = Map_Day, 2 = Map_Cloudy, 3 = Map_Night")]
    public int roundNumber = 1;

    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
        // Replace onClick completely to nuke any persistent or runtime listeners
        _btn.onClick = new Button.ButtonClickedEvent();
        _btn.onClick.AddListener(OnClick);
        Debug.Log($"[RoundButtonHandler] Awake: {gameObject.name} (round={roundNumber}) wired.");
    }

    private void OnClick()
    {
        Debug.Log($"[RoundButtonHandler] CLICKED '{gameObject.name}' -> OnRoundSelected({roundNumber})");

        MainMenuManager mmm = Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm != null)
        {
            mmm.OnRoundSelected(roundNumber);
        }
        else
        {
            Debug.LogError("[RoundButtonHandler] MainMenuManager not found!");
        }
    }
}
