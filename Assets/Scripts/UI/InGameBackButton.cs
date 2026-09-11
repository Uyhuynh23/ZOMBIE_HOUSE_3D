using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Top-right Back button component for in-game HUDs.
/// Gracefully leaves the network session if connected, or loads MainMenu for solo play.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class InGameBackButton : MonoBehaviour, IPointerClickHandler
{
    private Button button;
    private bool isLeaving = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickBack);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickBack();
    }

    public async void OnClickBack()
    {
        if (isLeaving) return;
        isLeaving = true;

        Debug.Log("[InGameBackButton] Clicked! Returning to Main Menu...");
        AudioManager.PlaySfx(AudioCue.UiClick);
        Time.timeScale = 1f;

        if (NetworkBootstrap.IsNetworkSession && NetworkBootstrap.Instance != null)
        {
            await NetworkBootstrap.Instance.LeaveToMenuAsync("Player returned to menu from HUD.");
            return;
        }

        string menuScene = GameDataCarrier.MainMenuSceneName;
        if (string.IsNullOrEmpty(menuScene)) menuScene = "MainMenu";
        SceneManager.LoadScene(menuScene);
    }
}
