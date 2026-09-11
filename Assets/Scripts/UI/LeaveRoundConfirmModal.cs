using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Confirmation modal popup when the player clicks the InGameBackButton.
/// Shows a dark overlay, the Leave Round dialog banner, and Cancel / Leave buttons.
/// </summary>
public class LeaveRoundConfirmModal : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnCancel;
    public Button btnLeave;

    [Header("Optional Animation Target")]
    public Transform dialogFrame;

    private bool isLeaving = false;

    private void Awake()
    {
        if (btnCancel != null)
        {
            btnCancel.onClick.AddListener(Close);
        }

        if (btnLeave != null)
        {
            btnLeave.onClick.AddListener(ConfirmLeave);
        }
    }

    private void OnEnable()
    {
        // Ensure cursor is freed and visible when modal opens
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (dialogFrame != null)
        {
            dialogFrame.localScale = Vector3.one * 0.92f;
        }
    }

    private void Update()
    {
        // Smooth subtle pop-in
        if (dialogFrame != null && dialogFrame.localScale.x < 0.999f)
        {
            dialogFrame.localScale = Vector3.Lerp(dialogFrame.localScale, Vector3.one, Time.unscaledDeltaTime * 18f);
        }

        // Press Escape to cancel/close
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
        AudioManager.PlaySfx(AudioCue.UiClick);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        AudioManager.PlaySfx(AudioCue.UiClick);
        gameObject.SetActive(false);
    }

    public async void ConfirmLeave()
    {
        if (isLeaving) return;
        isLeaving = true;

        Debug.Log("[LeaveRoundConfirmModal] Confirmed leave! Returning to Main Menu...");
        AudioManager.PlaySfx(AudioCue.UiClick);
        Time.timeScale = 1f;

        if (NetworkBootstrap.IsNetworkSession && NetworkBootstrap.Instance != null)
        {
            await NetworkBootstrap.Instance.LeaveToMenuAsync("Player confirmed leave from in-game modal.");
            return;
        }

        string menuScene = GameDataCarrier.MainMenuSceneName;
        if (string.IsNullOrEmpty(menuScene)) menuScene = "MainMenu";
        SceneManager.LoadScene(menuScene);
    }
}
