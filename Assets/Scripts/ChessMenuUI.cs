using UnityEngine;
using UnityEngine.UI;

public class ChessMenuUI : MonoBehaviour
{
    [Header("References")]
    public ChessGameManager gameManager;

    [Header("Panels")]
    public GameObject mainMenuPanel;      // Start / Quit
    public GameObject modeSelectPanel;    // Play with Friend / Play with AI
    public GameObject difficultyPanel;    // Easy / Medium / Hard

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Mode Select Buttons")]
    public Button playFriendButton;
    public Button playAIButton;
    public Button backToMainButton;

    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button backToModeButton;

    void Start()
    {
        ShowMainMenu();

        startButton.onClick.AddListener(OnStartClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        playFriendButton.onClick.AddListener(OnPlayFriendClicked);
        playAIButton.onClick.AddListener(OnPlayAIClicked);
        backToMainButton.onClick.AddListener(ShowMainMenu);

        easyButton.onClick.AddListener(() => StartAIGame(1));
        mediumButton.onClick.AddListener(() => StartAIGame(2));
        hardButton.onClick.AddListener(() => StartAIGame(3));
        backToModeButton.onClick.AddListener(ShowModeSelect);
    }

    private void OnStartClicked()
    {
        ShowModeSelect();
    }

    private void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnPlayFriendClicked()
    {
        gameManager.StartGame(false, 1);
        HideAllPanels();
    }

    private void OnPlayAIClicked()
    {
        ShowDifficultySelect();
    }

    private void StartAIGame(int depth)
    {
        gameManager.StartGame(true, depth);
        HideAllPanels();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
        difficultyPanel.SetActive(false);
    }

    private void ShowModeSelect()
    {
        mainMenuPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
        difficultyPanel.SetActive(false);
    }

    private void ShowDifficultySelect()
    {
        mainMenuPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
        difficultyPanel.SetActive(true);
    }

    private void HideAllPanels()
    {
        mainMenuPanel.SetActive(false);
        modeSelectPanel.SetActive(false);
        difficultyPanel.SetActive(false);
    }
}