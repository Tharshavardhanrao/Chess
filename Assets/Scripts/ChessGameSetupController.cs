using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChessGameSetupController : MonoBehaviour
{
    private enum MenuStep { Mode, Color, Difficulty, Playing }

    [Header("Loading Screen")]
    [Tooltip("Leave empty to skip straight to the Mode panel with no loading screen.")]
    public GameObject loadingPanel;
    public Slider loadingSlider;
    public Text loadingText;
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
    public Button colorBackButton;
    public Button difficultyBackButton;

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

    private void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

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
            ShowStep(MenuStep.Difficulty);
        }
        else
        {
            StartTheGame(2);
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
            return;
        }

        ShowStep(MenuStep.Playing); 
        SetActive(loadingPanel, false); 

        if (selectedVsAI)
        {
            ChessPieceColor aiSide = selectedColor == ChessPieceColor.White
                ? ChessPieceColor.Black
                : ChessPieceColor.White;

            gameManager.StartGame(true, depth, aiSide);
        }
        else
        {
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

    public void ShowMainMenu()
    {
        ShowStep(MenuStep.Mode);
    }
}