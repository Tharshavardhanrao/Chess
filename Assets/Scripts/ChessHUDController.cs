using UnityEngine;
using UnityEngine.UI;

public class ChessHUDController : MonoBehaviour
{
    [Header("References")]
    public ChessGameManager gameManager;
    public ChessGameSetupController setupController;
    public ChessCameraController cameraController;

    [Header("HUD (Settings + Resign buttons only)")]
    public GameObject hudPanel;
    public Button settingsButton;
    public Button resignButton;

    [Header("Resign Confirm Panel")]
    public GameObject resignPanel;
    public Button resignYesButton;
    public Button resignNoButton;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Button cameraButton;
    [Tooltip("Lock image on the camera button. Visible = camera locked, hidden = camera free.")]
    public GameObject lockImage;

    [Header("Game Over Popup")]
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Button gameOverMainMenuButton;

    void Awake()
    {
        if (gameManager != null) gameManager.OnGameOver.AddListener(HandleGameOver);
        if (cameraController != null) cameraController.OnLockChanged.AddListener(RefreshLockImage);

        Bind(settingsButton, ToggleSettings);
        Bind(resignButton, () => SetActive(resignPanel, true));
        Bind(resignYesButton, ConfirmResign);
        Bind(resignNoButton, () => SetActive(resignPanel, false));
        Bind(cameraButton, () => { if (cameraController != null) cameraController.ToggleLock(); });
        Bind(gameOverMainMenuButton, HandleMainMenu);

        CloseAllPanels();
    }

    void Start()
    {
        RefreshLockImage(cameraController != null && cameraController.IsLocked);
    }

    public void ShowHUD()
    {
        CloseAllPanels();
        SetActive(hudPanel, true);

        if (cameraController != null) cameraController.SetLocked(false);
        RefreshLockImage(false);
    }

    public void HideHUD()
    {
        CloseAllPanels();
        SetActive(hudPanel, false);
    }

    private void ToggleSettings()
    {
        if (settingsPanel != null) SetActive(settingsPanel, !settingsPanel.activeSelf);
    }

    private void ConfirmResign()
    {
        SetActive(resignPanel, false);
        if (gameManager != null) gameManager.Resign(gameManager.CurrentTurn);
    }

    private void RefreshLockImage(bool locked)
    {
        SetActive(lockImage, locked);
    }

    private void HandleGameOver(string message)
    {
        SetActive(resignPanel, false);
        SetActive(settingsPanel, false);
        SetActive(gameOverPanel, true);
        if (gameOverText != null) gameOverText.text = message;
    }

    private void HandleMainMenu()
    {
        if (cameraController != null) cameraController.SetLocked(false);
        HideHUD();
        if (setupController != null) setupController.ShowMainMenu();
    }

    private void CloseAllPanels()
    {
        SetActive(resignPanel, false);
        SetActive(settingsPanel, false);
        SetActive(gameOverPanel, false);
    }

    private void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}