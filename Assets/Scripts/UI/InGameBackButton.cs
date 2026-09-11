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
    [Header("Confirmation Modal")]
    [Tooltip("If true, shows the confirmation modal dialog. If false (e.g. tutorial), directly leaves.")]
    public bool requireConfirmation = true;
    public LeaveRoundConfirmModal confirmModal;

    private Button button;
    private bool isLeaving = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickBack);
        }

        if (confirmModal == null && requireConfirmation)
        {
            confirmModal = Object.FindFirstObjectByType<LeaveRoundConfirmModal>(FindObjectsInactive.Include);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClickBack();
    }

    public void OnClickBack()
    {
        if (isLeaving) return;

        if (requireConfirmation)
        {
            if (confirmModal == null)
            {
                confirmModal = Object.FindFirstObjectByType<LeaveRoundConfirmModal>(FindObjectsInactive.Include);
            }

            if (confirmModal != null)
            {
                confirmModal.Open();
                return;
            }
        }

        DirectLeave();
    }

    public async void DirectLeave()
    {
        if (isLeaving) return;
        isLeaving = true;

        Debug.Log("[InGameBackButton] Returning to Main Menu...");
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
