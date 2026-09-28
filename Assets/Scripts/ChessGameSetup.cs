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

        // Fail loudly instead of throwing a silent NullReferenceException that
        // only kills this method — this way it's obvious in the Console
        // exactly what's missing, instead of just "no pieces spawned".
        if (board == null)
        {
            Debug.LogError("ChessGameSetup: 'board' is not assigned and no " +
                "ChessBoardGenerator was found on this GameObject. Assign it " +
                "in the Inspector, or make sure this script sits on the same " +
                "GameObject as ChessBoardGenerator.", this);
            return;
        }

        if (pieceFactory == null)
        {
            Debug.LogError("ChessGameSetup: 'pieceFactory' is not assigned and " +
                "no ChessPieceFactory was found on this GameObject. Assign it " +
                "in the Inspector, or make sure this script sits on the same " +
                "GameObject as ChessPieceFactory.", this);
            return;
        }

        SetupStandardPosition();
    }

    [ContextMenu("Setup Standard Position")]
    public void SetupStandardPosition()
    {
        if (board == null || pieceFactory == null)
        {
            Debug.LogError("ChessGameSetup: cannot set up pieces, 'board' or " +
                "'pieceFactory' is missing. Check the Inspector references.", this);
            return;
        }

        ClearExistingPieces();

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

    // Prevents duplicate pieces piling up if SetupStandardPosition() is ever
    // called more than once (e.g. via the context menu, or if you later wire
    // up a "restart game" button that re-runs this).
    private void ClearExistingPieces()
    {
        ChessPieceInfo[] existingPieces = GetComponentsInChildren<ChessPieceInfo>();
        foreach (var p in existingPieces)
        {
            if (Application.isPlaying) Destroy(p.gameObject);
            else DestroyImmediate(p.gameObject);
        }

        // Also catch pieces parented directly under the board, in case this
        // script's own GameObject isn't the board's parent.
        if (board != null)
        {
            ChessPieceInfo[] boardPieces = board.GetComponentsInChildren<ChessPieceInfo>();
            foreach (var p in boardPieces)
            {
                if (Application.isPlaying) Destroy(p.gameObject);
                else DestroyImmediate(p.gameObject);
            }
        }
    }

    private void SpawnAt(ChessPieceType type, ChessPieceColor color, int row, int col, float halfBoard, float squareSize)
    {
        float x = col * squareSize - halfBoard + squareSize / 2f;
        float z = row * squareSize - halfBoard + squareSize / 2f;

        // Consistent with ChessGameManager.ComputeWorldPos() — places the
        // piece's base exactly on top of the tile, using the board's own
        // tileThickness rather than a hardcoded magic number.
        float y = board.tileThickness / 2f;

        Vector3 pos = new Vector3(x, y, z);
        pieceFactory.CreatePiece(type, color, pos, row, col, board.transform);
    }
}