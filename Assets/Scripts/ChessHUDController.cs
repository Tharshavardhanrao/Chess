using UnityEngine;
using UnityEngine.UI;

// The in-game HUD, shown once the game actually starts (after the menu
// panels close). Displays whose turn it is, shows a check/checkmate/
// stalemate message, and offers Resign and Main Menu buttons.
//
// INSPECTOR SETUP:
// 1. Drag your ChessGameManager into Game Manager.
// 2. Drag your ChessGameSetupController into Setup Controller (so "Main Menu"
//    can bring the menu panels back).
// 3. Drag the HUD Panel itself into Hud Panel. It can start ACTIVE or
//    INACTIVE in the scene — this script hides it until ShowHUD() is called.
// 4. Drag a Text for "White's Turn" / "Black's Turn" into Turn Text.
// 5. (Optional) Drag a Text for check/checkmate/stalemate/resign messages
//    into Status Text.
// 6. (Optional) Drag Resign / Main Menu buttons into their fields.
// 7. (Optional) For a separate game-over popup instead of just using Status
//    Text, drag a panel into Game Over Panel and a Text into Game Over Text.
// No Button OnClick() entries are needed; everything is bound here in code.
public class ChessHUDController : MonoBehaviour
{
    [Header("References")]
    public ChessGameManager gameManager;
    public ChessGameSetupController setupController;

    [Header("HUD Panel")]
    public GameObject hudPanel;

    [Header("Turn / Status Text")]
    public Text turnText;
    public Text statusText; // optional — check / checkmate / stalemate / resign messages

    [Header("HUD Buttons")]
    public Button resignButton;
    public Button mainMenuButton;

    [Header("Game Over Popup (optional)")]
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Button gameOverMainMenuButton;

    void Awake()
    {
        if (gameManager != null)
        {
            gameManager.OnTurnChanged.AddListener(HandleTurnChanged);
            gameManager.OnCheck.AddListener(HandleCheck);
            gameManager.OnGameOver.AddListener(HandleGameOver);
        }

        Bind(resignButton, HandleResign);
        Bind(mainMenuButton, HandleMainMenu);
        Bind(gameOverMainMenuButton, HandleMainMenu);

        SetActive(hudPanel, false);
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

    // Call this right when a new game starts.
    public void ShowHUD()
    {
        SetActive(hudPanel, true);
        SetActive(gameOverPanel, false);

        if (statusText != null) statusText.text = "";

        if (gameManager != null)
        {
            HandleTurnChanged(gameManager.CurrentTurn);
        }
    }

    public void HideHUD()
    {
        SetActive(hudPanel, false);
        SetActive(gameOverPanel, false);
    }

    private void HandleTurnChanged(ChessPieceColor color)
    {
        if (turnText != null)
        {
            turnText.text = color == ChessPieceColor.White ? "White's Turn" : "Black's Turn";
        }
        if (statusText != null) statusText.text = "";
    }

    private void HandleCheck(ChessPieceColor colorInCheck)
    {
        if (statusText != null)
        {
            string who = colorInCheck == ChessPieceColor.White ? "White" : "Black";
            statusText.text = $"{who} is in Check!";
        }
    }

    private void HandleGameOver(string message)
    {
        if (statusText != null) statusText.text = message;

        if (gameOverPanel != null)
        {
            SetActive(gameOverPanel, true);
            if (gameOverText != null) gameOverText.text = message;
        }
    }

    private void HandleResign()
    {
        if (gameManager == null) return;
        gameManager.Resign(gameManager.CurrentTurn);
    }

    private void HandleMainMenu()
    {
        HideHUD();
        if (setupController != null) setupController.ShowMainMenu();
    }
}