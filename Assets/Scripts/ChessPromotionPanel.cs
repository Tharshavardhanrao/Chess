using System;
using UnityEngine;
using UnityEngine.UI;

public class ChessPromotionPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Buttons")]
    public Button queenButton;
    public Button rookButton;
    public Button bishopButton;
    public Button knightButton;

    private Action<ChessPieceType> onChosen;

    void Awake()
    {
        Bind(queenButton, ChessPieceType.Queen);
        Bind(rookButton, ChessPieceType.Rook);
        Bind(bishopButton, ChessPieceType.Bishop);
        Bind(knightButton, ChessPieceType.Knight);
    }

    void Start()
    {
        if (onChosen == null) SetPanelActive(false);
    }

    private void Bind(Button button, ChessPieceType type)
    {
        if (button != null) button.onClick.AddListener(() => Choose(type));
    }

    public void Show(Action<ChessPieceType> onPicked)
    {
        onChosen = onPicked;
        SetPanelActive(true);
    }

    public void Hide()
    {
        onChosen = null;
        SetPanelActive(false);
    }

    private void Choose(ChessPieceType type)
    {
        Action<ChessPieceType> callback = onChosen;
        Hide();
        callback?.Invoke(type);
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}