using UnityEngine;

public class ChessGameSetup : MonoBehaviour
{
    public ChessBoardGenerator board;
    public ChessPieceFactory pieceFactory;

    private static readonly ChessPieceType[] BackRow = new ChessPieceType[]
    {
        ChessPieceType.Rook, ChessPieceType.Knight, ChessPieceType.Bishop, ChessPieceType.Queen,
        ChessPieceType.King, ChessPieceType.Bishop, ChessPieceType.Knight, ChessPieceType.Rook
    };

    void Start()
    {
        if (board == null) board = GetComponent<ChessBoardGenerator>();
        if (pieceFactory == null) pieceFactory = GetComponent<ChessPieceFactory>();

        SetupStandardPosition();
    }

    [ContextMenu("Setup Standard Position")]
    public void SetupStandardPosition()
    {
        float squareSize = board.squareSize;
        int boardSize = board.boardSize;
        float halfBoard = (boardSize * squareSize) / 2f;

        for (int col = 0; col < boardSize; col++)
        {
            SpawnAt(BackRow[col], ChessPieceColor.White, 0, col, halfBoard, squareSize);
            SpawnAt(ChessPieceType.Pawn, ChessPieceColor.White, 1, col, halfBoard, squareSize);
        }

        for (int col = 0; col < boardSize; col++)
        {
            SpawnAt(BackRow[col], ChessPieceColor.Black, boardSize - 1, col, halfBoard, squareSize);
            SpawnAt(ChessPieceType.Pawn, ChessPieceColor.Black, boardSize - 2, col, halfBoard, squareSize);
        }
    }

    private void SpawnAt(ChessPieceType type, ChessPieceColor color, int row, int col, float halfBoard, float squareSize)
    {
        float x = col * squareSize - halfBoard + squareSize / 2f;
        float z = row * squareSize - halfBoard + squareSize / 2f;

        // This is the EXACT same Y value the old models used. 
        // Change 0.2f to 0f if your custom models sink into the board.
        // Change 0.2f to 0.3f if they float.
        float y = 0.2f;

        Vector3 pos = new Vector3(x, y, z);
        pieceFactory.CreatePiece(type, color, pos, row, col, board.transform);
    }
}