using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Handles the menu flow in code. No Button OnClick() entries are needed in
// the Inspector, and no panel needs to be enabled/disabled by hand.
//
//   Mode Panel       : [vs Friend] [vs AI]
//   Color Panel      : [Play White] [Play Black]        (both modes)
//   Difficulty Panel : [Easy] [Medium] [Hard]            (AI mode only)
//
//   Mode -> Friend -> Color -> game
//   Mode -> AI     -> Color -> Difficulty -> game
//
// There is no Start panel and no separate loading script — this component
// plays the loading animation itself (if loadingPanel is assigned), then
// opens the Mode panel directly once it finishes.
//
// INSPECTOR SETUP: drag the panels, buttons and ChessGameManager into the
// fields below. Panels can be in any active/inactive state in the scene;
// this script sets the correct state as soon as Play starts.
public class ChessGameSetupController : MonoBehaviour
{
    private enum MenuStep { Mode, Color, Difficulty, Playing }

    [Header("Loading Screen")]
    [Tooltip("Leave empty to skip straight to the Mode panel with no loading screen.")]
    public GameObject loadingPanel;
    public Slider loadingSlider;
    public Text loadingText; // optional — leave empty if you don't have one
    [Tooltip("How long the loading slider takes to fill, in seconds.")]
    public float loadingDuration = 2.5f;
    [Tooltip("Shapes how the fill speeds up/slows down.")]
    public AnimationCurve loadingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Small pause at 100% before the loading panel hides.")]
    public float loadingDelayBeforeHide = 0.3f;

    [Header("Game")]
    public ChessGameManager gameManager;
    [Tooltip("Optional — shown automatically once a game starts.")]
    public ChessHUDController hudController;

    [Header("Panels")]
    public GameObject modePanel;
    public GameObject colorPanel;
    public GameObject difficultyPanel;

    [Header("Mode Panel Buttons")]
    public Button vsFriendButton;
    public Button vsAIButton;
    [Tooltip("Optional — wire this up if you still want a Quit option on the mode panel.")]
    public Button quitButton;

    [Header("Color Panel Buttons")]
    public Button playWhiteButton;
    public Button playBlackButton;

    [Header("Difficulty Panel Buttons (AI mode only)")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("Optional Back Buttons (leave empty if you don't have them)")]
    public Button colorBackButton;      // Color -> Mode
    public Button difficultyBackButton; // Difficulty -> Color

    [Header("Pause Menu")]
    [Tooltip("The gear/settings button that opens the Pause panel during play.")]
    public Button settingsButton;
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;   // opens the Yes/No confirm panel below
    public Button pauseMainMenuButton; // leaves immediately, no confirmation

    [Header("Pause Menu — Restart Confirmation")]
    public GameObject confirmPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("AI Difficulty (search depth)")]
    [Tooltip("Higher depth = stronger but slower AI.")]
    [Range(1, 4)] public int easyDepth = 1;
    [Range(1, 4)] public int mediumDepth = 2;
    [Range(1, 4)] public int hardDepth = 3;

    private bool selectedVsAI = false;
    private ChessPieceColor selectedColor = ChessPieceColor.White;

    void Start()
    {
        Bind(vsFriendButton, () => { selectedVsAI = false; ShowStep(MenuStep.Color); });
        Bind(vsAIButton, () => { selectedVsAI = true; ShowStep(MenuStep.Color); });
        Bind(quitButton, QuitGame);

        Bind(playWhiteButton, () => PickColor(ChessPieceColor.White));
        Bind(playBlackButton, () => PickColor(ChessPieceColor.Black));

        Bind(easyButton, () => PickDifficulty(easyDepth));
        Bind(mediumButton, () => PickDifficulty(mediumDepth));
        Bind(hardButton, () => PickDifficulty(hardDepth));

        Bind(colorBackButton, () => ShowStep(MenuStep.Mode));
        Bind(difficultyBackButton, () => ShowStep(MenuStep.Color));

        Bind(settingsButton, OpenPauseMenu);
        Bind(resumeButton, ResumeGame);
        Bind(restartButton, OpenRestartConfirm);
        Bind(pauseMainMenuButton, LeaveToMainMenu);
        Bind(confirmYesButton, LeaveToMainMenu);
        Bind(confirmNoButton, CancelRestartConfirm);

        SetActive(pausePanel, false);
        SetActive(confirmPanel, false);

        if (loadingPanel != null)
        {
            SetActive(loadingPanel, true);
            SetActive(modePanel, false);
            SetActive(colorPanel, false);
            SetActive(difficultyPanel, false);
            StartCoroutine(AnimateLoading());
        }
        else
        {
            ShowStep(MenuStep.Mode);
        }
    }

    private IEnumerator AnimateLoading()
    {
        SetLoadingProgress(0f);
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / loadingDuration);
            SetLoadingProgress(loadingCurve.Evaluate(t));
            yield return null;
        }

        SetLoadingProgress(1f);

        if (loadingDelayBeforeHide > 0f)
        {
            yield return new WaitForSeconds(loadingDelayBeforeHide);
        }

        SetActive(loadingPanel, false);
        ShowMainMenu();
    }

    private void SetLoadingProgress(float t)
    {
        if (loadingSlider != null) loadingSlider.value = t;
        if (loadingText != null) loadingText.text = Mathf.RoundToInt(t * 100f) + "%";
    }

    // Adds a click listener only if the button is assigned, so optional
    // buttons (like Back / Quit) can simply be left empty.
    private void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    // Shows exactly one panel and hides the rest.
    private void ShowStep(MenuStep step)
    {
        SetActive(modePanel, step == MenuStep.Mode);
        SetActive(colorPanel, step == MenuStep.Color);
        SetActive(difficultyPanel, step == MenuStep.Difficulty);
    }

    private void SetActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void PickColor(ChessPieceColor humanColor)
    {
        selectedColor = humanColor;

        if (selectedVsAI)
        {
            // AI mode needs a difficulty pick next.
            ShowStep(MenuStep.Difficulty);
        }
        else
        {
            // Friend mode: nothing left to pick, start right away.
            StartTheGame(2); // depth is irrelevant in friend mode
        }
    }

    private void PickDifficulty(int depth)
    {
        StartTheGame(Mathf.Clamp(depth, 1, 4));
    }

    private void StartTheGame(int depth)
    {
        if (gameManager == null)
        {
            Debug.LogError("ChessGameSetupController: Game Manager is not assigned.", this);
            return;
        }

        ShowStep(MenuStep.Playing); // hides every panel
        SetActive(loadingPanel, false); // in case a game is restarted while it's still around

        if (selectedVsAI)
        {
            // The AI plays whichever color the human did not pick.
            ChessPieceColor aiSide = selectedColor == ChessPieceColor.White
                ? ChessPieceColor.Black
                : ChessPieceColor.White;

            gameManager.StartGame(true, depth, aiSide);
        }
        else
        {
            // Friend mode: White always moves first, and the camera flips every turn.
            gameManager.StartGame(false, depth);
        }

        if (hudController != null) hudController.ShowHUD();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------
    // Pause Menu — Settings button opens it; Restart asks Yes/No first.
    // ------------------------------------------------------------------
    private void OpenPauseMenu()
    {
        SetActive(pausePanel, true);
        SetActive(confirmPanel, false);
        if (gameManager != null) gameManager.SetPaused(true);
    }

    private void ResumeGame()
    {
        SetActive(pausePanel, false);
        SetActive(confirmPanel, false);
        if (gameManager != null) gameManager.SetPaused(false);
    }

    private void OpenRestartConfirm()
    {
        SetActive(confirmPanel, true);
    }

    private void CancelRestartConfirm()
    {
        // "No" just closes the confirm popup — still paused, pause panel still open.
        SetActive(confirmPanel, false);
    }

    // Used by both "Yes" on the restart confirm, and the direct Main Menu
    // button — leaves to the Mode panel. Picking a mode/color again starts a
    // brand new game (ChessGameManager.StartGame() resets the board).
    private void LeaveToMainMenu()
    {
        SetActive(pausePanel, false);
        SetActive(confirmPanel, false);

        if (gameManager != null) gameManager.SetPaused(false);
        if (hudController != null) hudController.HideHUD();
        ShowMainMenu();
    }

    // Called by the loading screen once it finishes — opens the Mode panel,
    // since there's no separate Start panel anymore.
    public void ShowMainMenu()
    {
        ShowStep(MenuStep.Mode);
    }
}