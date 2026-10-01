using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public partial class ChessGameManager : MonoBehaviour
{
    public ChessBoardGenerator board;
    public ChessPieceFactory pieceFactory;
    public ChessGameSetup gameSetup;
    public Camera cam;
    public ChessCameraController cameraController;
    public ChessPromotionPanel promotionPanel;

    public Color selectedColor = new Color(0.3f, 0.6f, 1f);
    public Color legalMoveColor = new Color(0.4f, 0.9f, 0.4f);
    public Color captureColor = new Color(0.95f, 0.3f, 0.3f);
    [Tooltip("How strongly an enemy piece that can be captured is tinted with the capture color.")]

    public bool aiEnabled = false;
    public ChessPieceColor aiColor = ChessPieceColor.Black;
    [Range(1, 4)]
    public int aiSearchDepth = 3;
    public float aiMoveDelay = 0.4f;

    [Header("Sound")]
    [Tooltip("Leave empty and one will be added automatically.")]
    public AudioSource audioSource;
    [Tooltip("Played whenever a piece moves to an empty square.")]
    public AudioClip moveSound;
    [Tooltip("Played whenever a move captures an enemy piece. Falls back to moveSound if left empty.")]
    public AudioClip captureSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Game Start / Background Music")]
    [Tooltip("Leave empty and one will be added automatically.")]
    public AudioSource musicSource;
    [Tooltip("Played once, right when the game actually starts (after the menu panels close).")]
    public AudioClip gameStartSound;
    [Tooltip("Optional looping track that starts playing alongside gameStartSound.")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("Move Animation")]
    public float moveAnimDuration = 0.28f;
    public float moveJumpHeight = 0.5f;
    private bool isAnimating = false;
    private bool isAwaitingPromotion = false;
    private bool isPaused = false;

    [Header("Captured Pieces")]
    [Tooltip("Size of a captured piece relative to its size on the board.")]
    public float capturedPieceScale = 0.6f;
    [Tooltip("Distance between captured pieces, as a fraction of one square.")]
    public float capturedSpacing = 0.6f;
    [Tooltip("Columns of captured pieces beside the board (rows run along the board's length).")]
    public int capturedColumns = 2;
    [Tooltip("Gap between the board's border and the first captured piece, in squares.")]
    public float capturedSideGap = 0.5f;
    [Tooltip("Swap which side of the board each color's captured pieces go on.")]
    public bool swapCapturedSides = false;
    [Tooltip("Nudge the height captured pieces stand at (added to the detected table height). " +
             "Raise it if they sink into the table, lower it if they float.")]
    public float capturedHeightOffset = 0f;
    public float capturedAnimDuration = 0.45f;
    [Tooltip("How high a captured piece arcs while flying to its spot, in squares.")]
    public float capturedArcHeight = 0.8f;

    // Pieces each side has captured so far (used to pick the next free spot).
    private int capturedByWhite = 0;
    private int capturedByBlack = 0;

    public UnityEvent<ChessPieceColor> OnTurnChanged;
    public UnityEvent<string> OnGameOver;
    public UnityEvent<ChessPieceColor> OnCheck;

    [HideInInspector] public bool gameStarted = false;

    private int boardSize;
    private float squareSize;
    private float halfBoard;

    private ChessPieceType?[,] pieceType;
    private ChessPieceColor?[,] pieceColor;
    private GameObject[,] pieceObjects;
    private GameObject[,] tileObjects;
    private Material[,] tileOriginalMaterial;

    private ChessPieceColor currentTurn = ChessPieceColor.White;
    private int selectedRow = -1;
    private int selectedCol = -1;
    private List<Vector2Int> currentLegalMoves = new List<Vector2Int>();
    private List<Vector2Int> highlightedTiles = new List<Vector2Int>();
    private List<GameObject> moveIndicators = new List<GameObject>();
    private bool gameOver = false;

    // --- Tap-vs-drag tracking (lets camera orbit/zoom coexist with piece taps) ---
    private bool pointerDown = false;
    private Vector2 pointerDownPos;
    private const float TapMoveThreshold = 25f; // pixels; tweak for touch feel

    public ChessPieceColor CurrentTurn => currentTurn;
    public bool IsGameOver => gameOver;

    // Ends the game immediately as a resignation. The OTHER color wins.
    public void Resign(ChessPieceColor resigningColor)
    {
        if (gameOver || !gameStarted) return;

        gameOver = true;
        StopAllCoroutines();
        isAnimating = false;

        string winner = Opposite(resigningColor) == ChessPieceColor.White ? "White" : "Black";
        string message = $"{winner} wins — opponent resigned.";
        OnGameOver?.Invoke(message);
    }

    // Blocks board taps (piece selection/movement) while true. Doesn't touch
    // AI thinking or in-progress animations — it only gates player input.
    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    void Start()
    {
        if (board == null) board = GetComponent<ChessBoardGenerator>();
        if (pieceFactory == null) pieceFactory = GetComponent<ChessPieceFactory>();
        if (gameSetup == null) gameSetup = GetComponent<ChessGameSetup>();
        if (cam == null) cam = Camera.main;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(InitializeAfterSceneReady());
    }

    private IEnumerator InitializeAfterSceneReady()
    {
        yield return null;
        BuildStateFromScene();
    }

    private void BuildStateFromScene()
    {
        boardSize = board.boardSize;
        squareSize = board.squareSize;
        halfBoard = (boardSize * squareSize) / 2f;

        pieceType = new ChessPieceType?[boardSize, boardSize];
        pieceColor = new ChessPieceColor?[boardSize, boardSize];
        pieceObjects = new GameObject[boardSize, boardSize];
        tileObjects = new GameObject[boardSize, boardSize];
        tileOriginalMaterial = new Material[boardSize, boardSize];

        ChessSquareInfo[] tiles = board.GetComponentsInChildren<ChessSquareInfo>();
        foreach (ChessSquareInfo tile in tiles)
        {
            tileObjects[tile.row, tile.col] = tile.gameObject;
            tileOriginalMaterial[tile.row, tile.col] = tile.GetComponent<Renderer>().sharedMaterial;
        }

        ChessPieceInfo[] pieces = board.GetComponentsInChildren<ChessPieceInfo>();
        foreach (ChessPieceInfo piece in pieces)
        {
            if (piece.row < 0 || piece.col < 0) continue; // captured piece sitting in a tray
            pieceObjects[piece.row, piece.col] = piece.gameObject;
            pieceType[piece.row, piece.col] = piece.type;
            pieceColor[piece.row, piece.col] = piece.color;
        }

        capturedByWhite = 0;
        capturedByBlack = 0;

        currentTurn = ChessPieceColor.White;

        // Always face White as soon as the board is ready, even before
        // StartGame() is called from a menu — so testing directly in the
        // scene (Play button) also shows the correct starting orientation.
        cameraController?.SnapViewForTurn(ChessPieceColor.White);

        if (gameStarted && aiEnabled && currentTurn == aiColor)
        {
            TriggerAIMove();
        }
    }

    public void StartGame(bool vsAI, int depth, ChessPieceColor aiSide = ChessPieceColor.Black)
    {
        // Cancel anything left over from a previous game (pending AI move,
        // running move/capture animations) so it can't act on the new board.
        StopAllCoroutines();
        isAnimating = false;
        isAwaitingPromotion = false;
        if (promotionPanel != null) promotionPanel.Hide();

        aiEnabled = vsAI;
        aiSearchDepth = Mathf.Clamp(depth, 1, 4);
        aiColor = aiSide;

        gameOver = false;
        gameStarted = false; // stays false during the rebuild below

        // Clear highlights BEFORE rebuilding, so tinted tile materials are
        // never captured as the "original" tile materials.
        if (tileObjects != null) DeselectAll();

        // Reset the board to the standard starting position for every new game.
        if (gameSetup != null)
        {
            gameSetup.SetupStandardPosition();
        }
        BuildStateFromScene();

        // Only now mark the game as started. BuildStateFromScene() checks this
        // flag to trigger the AI, so setting it earlier would make the AI's
        // opening move fire twice (once there, once below).
        gameStarted = true;

        PlayGameStartAudio();

        OnTurnChanged?.Invoke(currentTurn);

        // The camera should show whichever side the HUMAN is actually playing.
        // - AI mode: the human plays whatever color the AI is NOT, and since
        //   there's no per-turn rotation in AI mode, this is the one and only
        //   view for the whole game — so it must be correct from the start.
        // - Friend mode: always starts on White (White moves first, by the
        //   rules), and the camera continues flipping every turn as normal.
        ChessPieceColor humanFacingColor = aiEnabled ? Opposite(aiColor) : ChessPieceColor.White;
        cameraController?.SnapViewForTurn(humanFacingColor);

        if (aiEnabled && currentTurn == aiColor)
        {
            TriggerAIMove();
        }
    }

    void Update()
    {
        if (!gameStarted) return;
        if (gameOver || tileObjects == null) return;
        if (isAnimating) return;
        if (isAwaitingPromotion) return;
        if (isPaused) return;
        if (aiEnabled && currentTurn == aiColor) return;

        // ---- Mouse ----
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            pointerDown = true;
            pointerDownPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (pointerDown)
            {
                pointerDown = false;
                Vector2 upPos = Input.mousePosition;
                if (Vector2.Distance(pointerDownPos, upPos) <= TapMoveThreshold)
                {
                    HandleTapAt(upPos);
                }
            }
        }

        // ---- Touch ----
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

                pointerDown = true;
                pointerDownPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (pointerDown)
                {
                    pointerDown = false;
                    if (Vector2.Distance(pointerDownPos, touch.position) <= TapMoveThreshold)
                    {
                        HandleTapAt(touch.position);
                    }
                }
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                pointerDown = false;
            }
        }
        else if (Input.touchCount >= 2)
        {
            // Two-finger input is reserved for camera pinch-zoom — cancel any
            // pending single-finger tap so it doesn't fire on release.
            pointerDown = false;
        }
    }

    private void HandleTapAt(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int? rc = GetRowColFromHit(hit);
            if (rc.HasValue) OnSquareTapped(rc.Value.x, rc.Value.y);
        }
    }

    private Vector2Int? GetRowColFromHit(RaycastHit hit)
    {
        ChessPieceInfo pieceInfo = hit.collider.GetComponentInParent<ChessPieceInfo>();
        if (pieceInfo != null)
        {
            if (pieceInfo.row < 0 || pieceInfo.col < 0) return null; // captured piece
            return new Vector2Int(pieceInfo.row, pieceInfo.col);
        }

        ChessSquareInfo tileInfo = hit.collider.GetComponentInParent<ChessSquareInfo>();
        if (tileInfo != null) return new Vector2Int(tileInfo.row, tileInfo.col);

        return null;
    }

    private void OnSquareTapped(int row, int col)
    {
        if (selectedRow >= 0)
        {
            if (currentLegalMoves.Contains(new Vector2Int(row, col)))
            {
                int fromR = selectedRow;
                int fromC = selectedCol;
                DeselectAll();
                ExecuteMove(fromR, fromC, row, col, () => AdvanceTurn());
                return;
            }

            if (pieceColor[row, col].HasValue && pieceColor[row, col].Value == currentTurn)
            {
                SelectSquare(row, col);
                return;
            }

            DeselectAll();
            return;
        }

        if (pieceColor[row, col].HasValue && pieceColor[row, col].Value == currentTurn)
        {
            SelectSquare(row, col);
        }
    }

    private void SelectSquare(int row, int col)
    {
        ClearHighlights();
        selectedRow = row;
        selectedCol = col;
        currentLegalMoves = GetLegalMoves(row, col);

        HighlightTile(row, col, selectedColor);
        foreach (Vector2Int move in currentLegalMoves)
        {
            bool isCapture = pieceColor[move.x, move.y].HasValue;

            if (isCapture)
            {
                // A small dot would be hidden under the enemy piece, so color
                // the whole tile instead — the piece itself is left untouched.
                HighlightTile(move.x, move.y, captureColor);
            }
            else
            {
                CreateMoveIndicator(move.x, move.y, legalMoveColor);
            }
        }
    }

    private void DeselectAll()
    {
        ClearHighlights();
        selectedRow = -1;
        selectedCol = -1;
        currentLegalMoves.Clear();
    }

    private void HighlightTile(int row, int col, Color color)
    {
        Renderer rend = tileObjects[row, col].GetComponent<Renderer>();
        rend.material.color = color;
        highlightedTiles.Add(new Vector2Int(row, col));
    }

    private void ClearHighlights()
    {
        foreach (Vector2Int t in highlightedTiles)
        {
            tileObjects[t.x, t.y].GetComponent<Renderer>().sharedMaterial = tileOriginalMaterial[t.x, t.y];
        }
        highlightedTiles.Clear();

        foreach (GameObject indicator in moveIndicators)
        {
            Destroy(indicator);
        }
        moveIndicators.Clear();
    }

    private void CreateMoveIndicator(int row, int col, Color color)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = $"MoveIndicator_{row}_{col}";
        indicator.transform.SetParent(board.transform, false);

        Vector3 pos = ComputeWorldPos(row, col);
        pos.y = board.tileThickness / 2f + 0.02f;
        indicator.transform.localPosition = pos;

        float diameter = 0.3f * squareSize;
        indicator.transform.localScale = new Vector3(diameter, 0.015f * squareSize, diameter);

        Destroy(indicator.GetComponent<Collider>());

        indicator.GetComponent<Renderer>().sharedMaterial = CreateIndicatorMaterial(color);
        moveIndicators.Add(indicator);
    }

    private void PlayGameStartAudio()
    {
        if (musicSource == null) return;

        if (gameStartSound != null)
        {
            musicSource.PlayOneShot(gameStartSound, musicVolume);
        }

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    private void PlayMoveSound(bool isCapture)
    {
        AudioClip clip = isCapture && captureSound != null ? captureSound : moveSound;
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private Material CreateIndicatorMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private void ExecuteMove(int fromRow, int fromCol, int toRow, int toCol, System.Action onComplete = null)
    {
        bool isCapture = pieceObjects[toRow, toCol] != null;
        PlayMoveSound(isCapture);

        if (isCapture)
        {
            GameObject captured = pieceObjects[toRow, toCol];
            pieceObjects[toRow, toCol] = null;
            // The mover's color is the capturing side (state arrays aren't updated yet).
            SendToCapturedArea(captured, pieceColor[fromRow, fromCol].Value);
        }

        GameObject movingGO = pieceObjects[fromRow, fromCol];
        ChessPieceColor movedColor = pieceColor[fromRow, fromCol].Value;
        ChessPieceType movedType = pieceType[fromRow, fromCol].Value;

        pieceType[toRow, toCol] = movedType;
        pieceColor[toRow, toCol] = movedColor;
        pieceType[fromRow, fromCol] = null;
        pieceColor[fromRow, fromCol] = null;
        pieceObjects[toRow, toCol] = movingGO;
        pieceObjects[fromRow, fromCol] = null;

        ChessPieceInfo info = movingGO.GetComponent<ChessPieceInfo>();
        info.row = toRow;
        info.col = toCol;

        Vector3 fromPos = movingGO.transform.localPosition;
        Vector3 promotionPos = ComputeWorldPos(toRow, toCol);
        // Keep the piece's own resting height (models with a centered pivot sit
        // higher than the tile top), so it doesn't sink after its first move.
        Vector3 toPos = promotionPos;
        toPos.y = fromPos.y;

        bool reachedLastRank = (movedColor == ChessPieceColor.White && toRow == boardSize - 1) ||
                                (movedColor == ChessPieceColor.Black && toRow == 0);
        bool willPromote = movedType == ChessPieceType.Pawn && reachedLastRank;

        StartCoroutine(AnimateMove(movingGO, fromPos, toPos, moveAnimDuration, moveJumpHeight * squareSize, () =>
        {
            if (willPromote)
            {
                bool humanMover = !(aiEnabled && movedColor == aiColor);

                if (humanMover && promotionPanel != null)
                {
                    // Let the player choose. The turn only advances after they pick.
                    isAwaitingPromotion = true;
                    promotionPanel.Show(chosenType =>
                    {
                        isAwaitingPromotion = false;
                        PromotePawn(toRow, toCol, movedColor, chosenType, promotionPos);
                        onComplete?.Invoke();
                    });
                    return;
                }

                // The AI (or a scene with no promotion panel) always takes a Queen.
                PromotePawn(toRow, toCol, movedColor, ChessPieceType.Queen, promotionPos);
            }

            onComplete?.Invoke();
        }));
    }

    // Replaces the pawn on (row, col) with a new piece of the chosen type.
    private void PromotePawn(int row, int col, ChessPieceColor color, ChessPieceType newType, Vector3 basePos)
    {
        GameObject oldPawn = pieceObjects[row, col];
        if (oldPawn != null)
        {
            oldPawn.SetActive(false);
            Destroy(oldPawn);
        }

        GameObject promoted = pieceFactory.CreatePiece(newType, color, basePos, row, col, board.transform);
        pieceObjects[row, col] = promoted;
        pieceType[row, col] = newType;
    }

    private IEnumerator AnimateMove(GameObject go, Vector3 fromPos, Vector3 toPos, float duration, float jumpHeight, System.Action onComplete)
    {
        isAnimating = true;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            go.transform.localPosition = toPos;
            isAnimating = false;
            onComplete?.Invoke();
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(fromPos, toPos, smoothT);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            go.transform.localPosition = pos;

            yield return null;
        }

        go.transform.localPosition = toPos;
        isAnimating = false;
        onComplete?.Invoke();
    }

    // ------------------------------------------------------------------
    // Captured pieces are lined up on the table beside the board. Each side's
    // captures start at that side's own end of the board, on opposite sides of
    // the board, so both collections stay visible from either camera view.
    // ------------------------------------------------------------------

    // Local position (in the board's space) where the Nth captured piece of a
    // side should stand, with its base on the table surface.
    private Vector3 GetCapturedSlotPosition(ChessPieceColor capturer, int index)
    {
        int columns = Mathf.Max(1, capturedColumns);
        int rowIdx = index / columns;   // moves along the board's length
        int colIdx = index % columns;   // moves away from the board

        float spacing = capturedSpacing * squareSize;
        float border = board.addBorder ? board.borderThickness : 0f;

        float sideDistance = halfBoard + border + capturedSideGap * squareSize + colIdx * spacing;
        float alongBoard = spacing * (rowIdx + 0.5f);

        float whiteSideSign = swapCapturedSides ? -1f : 1f;
        float x, z;
        if (capturer == ChessPieceColor.White)
        {
            x = whiteSideSign * sideDistance;
            z = -halfBoard + alongBoard;   // starts at White's end, heads toward Black
        }
        else
        {
            x = -whiteSideSign * sideDistance;
            z = halfBoard - alongBoard;    // starts at Black's end, heads toward White
        }

        return new Vector3(x, FindSurfaceLocalY(x, z), z);
    }

    // Finds the height of the table under a spot beside the board by casting a
    // ray straight down. Falls back to the underside of the board if the table
    // has no collider.
    private float FindSurfaceLocalY(float localX, float localZ)
    {
        Vector3 origin = board.transform.TransformPoint(new Vector3(localX, 20f * squareSize, localZ));

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f * squareSize,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return board.transform.InverseTransformPoint(hit.point).y + capturedHeightOffset;
        }

        return -board.tileThickness / 2f + capturedHeightOffset;
    }

    private void SendToCapturedArea(GameObject captured, ChessPieceColor capturer)
    {
        int index = capturer == ChessPieceColor.White ? capturedByWhite++ : capturedByBlack++;

        // Mark as off-board so board scans and taps ignore it.
        ChessPieceInfo info = captured.GetComponent<ChessPieceInfo>();
        if (info != null)
        {
            info.row = -1;
            info.col = -1;
        }
        foreach (Collider c in captured.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        StartCoroutine(AnimateCaptureToSpot(captured, GetCapturedSlotPosition(capturer, index)));
    }

    private IEnumerator AnimateCaptureToSpot(GameObject go, Vector3 spotBasePos)
    {
        if (go == null) yield break;

        Transform t = go.transform;
        Vector3 startPos = t.localPosition;
        Vector3 startScale = t.localScale;
        Vector3 endScale = startScale * capturedPieceScale;

        // How far the model's pivot sits above its base. It shrinks with the
        // piece, so the base still ends up resting on the table.
        float pivotHeight = startPos.y - board.tileThickness / 2f;
        Vector3 endPos = new Vector3(
            spotBasePos.x,
            spotBasePos.y + pivotHeight * capturedPieceScale,
            spotBasePos.z);

        float duration = Mathf.Max(0.01f, capturedAnimDuration);
        float arc = capturedArcHeight * squareSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;

            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float smooth = p * p * (3f - 2f * p);

            Vector3 pos = Vector3.Lerp(startPos, endPos, smooth);
            pos.y += Mathf.Sin(p * Mathf.PI) * arc;
            t.localPosition = pos;
            t.localScale = Vector3.Lerp(startScale, endScale, smooth);

            yield return null;
        }

        if (go != null)
        {
            t.localPosition = endPos;
            t.localScale = endScale;
        }
    }

    private Vector3 ComputeWorldPos(int row, int col)
    {
        float x = col * squareSize - halfBoard + squareSize / 2f;
        float z = row * squareSize - halfBoard + squareSize / 2f;
        // FIXED: Align with the top of the tiles
        return new Vector3(x, board.tileThickness / 2f, z);
    }

    private void AdvanceTurn()
    {
        currentTurn = Opposite(currentTurn);
        OnTurnChanged?.Invoke(currentTurn);

        // Only flip the camera in friend (pass-and-play) mode. In AI mode the
        // human plays from a fixed seat, so the camera always stays on White's side.
        if (!aiEnabled)
        {
            cameraController?.SetViewForTurn(currentTurn);
        }

        bool inCheck = IsInCheck(pieceType, pieceColor, currentTurn);
        List<(Vector2Int from, Vector2Int to)> legalMoves = GetAllLegalMoves(currentTurn);

        if (legalMoves.Count == 0)
        {
            gameOver = true;
            string message = inCheck
                ? $"Checkmate! {Opposite(currentTurn)} wins."
                : "Stalemate! The game is a draw.";
            Debug.Log(message);
            OnGameOver?.Invoke(message);
        }
        else if (inCheck)
        {
            Debug.Log($"{currentTurn} is in check!");
            OnCheck?.Invoke(currentTurn);
        }

        if (!gameOver && aiEnabled && currentTurn == aiColor)
        {
            TriggerAIMove();
        }
    }

    private ChessPieceColor Opposite(ChessPieceColor c) =>
        c == ChessPieceColor.White ? ChessPieceColor.Black : ChessPieceColor.White;

    private List<Vector2Int> GetLegalMoves(int row, int col)
    {
        List<Vector2Int> pseudo = GetPseudoLegalMoves(row, col, pieceType, pieceColor);
        List<Vector2Int> legal = new List<Vector2Int>();
        ChessPieceColor movingColor = pieceColor[row, col].Value;

        foreach (Vector2Int move in pseudo)
        {
            ChessPieceType?[,] tClone = (ChessPieceType?[,])pieceType.Clone();
            ChessPieceColor?[,] cClone = (ChessPieceColor?[,])pieceColor.Clone();

            tClone[move.x, move.y] = tClone[row, col];
            cClone[move.x, move.y] = cClone[row, col];
            tClone[row, col] = null;
            cClone[row, col] = null;

            if (!IsInCheck(tClone, cClone, movingColor))
            {
                legal.Add(move);
            }
        }

        return legal;
    }

    private List<(Vector2Int from, Vector2Int to)> GetAllLegalMoves(ChessPieceColor color)
    {
        List<(Vector2Int, Vector2Int)> all = new List<(Vector2Int, Vector2Int)>();
        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                if (pieceColor[r, c].HasValue && pieceColor[r, c].Value == color)
                {
                    foreach (Vector2Int move in GetLegalMoves(r, c))
                    {
                        all.Add((new Vector2Int(r, c), move));
                    }
                }
            }
        }
        return all;
    }

    private bool IsInCheck(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, ChessPieceColor color)
    {
        Vector2Int kingPos = FindKing(typeGrid, colorGrid, color);
        if (kingPos.x < 0) return false;
        return IsSquareAttacked(typeGrid, colorGrid, kingPos.x, kingPos.y, Opposite(color));
    }

    private Vector2Int FindKing(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, ChessPieceColor color)
    {
        for (int r = 0; r < boardSize; r++)
            for (int c = 0; c < boardSize; c++)
                if (typeGrid[r, c] == ChessPieceType.King && colorGrid[r, c] == color)
                    return new Vector2Int(r, c);
        return new Vector2Int(-1, -1);
    }

    private bool IsSquareAttacked(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, int row, int col, ChessPieceColor attacker)
    {
        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                if (!colorGrid[r, c].HasValue || colorGrid[r, c].Value != attacker) continue;

                if (typeGrid[r, c] == ChessPieceType.Pawn)
                {
                    int dir = attacker == ChessPieceColor.White ? 1 : -1;
                    if (row == r + dir && (col == c - 1 || col == c + 1)) return true;
                }
                else
                {
                    List<Vector2Int> moves = GetPseudoLegalMoves(r, c, typeGrid, colorGrid);
                    if (moves.Contains(new Vector2Int(row, col))) return true;
                }
            }
        }
        return false;
    }

    private List<Vector2Int> GetPseudoLegalMoves(int row, int col, ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        ChessPieceType type = typeGrid[row, col].Value;
        ChessPieceColor color = colorGrid[row, col].Value;

        switch (type)
        {
            case ChessPieceType.Pawn:
                AddPawnMoves(row, col, color, typeGrid, colorGrid, moves);
                break;
            case ChessPieceType.Knight:
                AddKnightMoves(row, col, color, colorGrid, moves);
                break;
            case ChessPieceType.Bishop:
                AddSlidingMoves(row, col, color, colorGrid, moves, DiagonalDirs);
                break;
            case ChessPieceType.Rook:
                AddSlidingMoves(row, col, color, colorGrid, moves, StraightDirs);
                break;
            case ChessPieceType.Queen:
                AddSlidingMoves(row, col, color, colorGrid, moves, DiagonalDirs);
                AddSlidingMoves(row, col, color, colorGrid, moves, StraightDirs);
                break;
            case ChessPieceType.King:
                AddKingMoves(row, col, color, colorGrid, moves);
                break;
        }

        return moves;
    }

    private static readonly Vector2Int[] DiagonalDirs = {
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };
    private static readonly Vector2Int[] StraightDirs = {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
    };
    private static readonly Vector2Int[] KnightOffsets = {
        new Vector2Int(2,1), new Vector2Int(2,-1), new Vector2Int(-2,1), new Vector2Int(-2,-1),
        new Vector2Int(1,2), new Vector2Int(1,-2), new Vector2Int(-1,2), new Vector2Int(-1,-2)
    };

    private bool InBounds(int r, int c) => r >= 0 && r < boardSize && c >= 0 && c < boardSize;

    private void AddPawnMoves(int row, int col, ChessPieceColor color, ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, List<Vector2Int> moves)
    {
        int dir = color == ChessPieceColor.White ? 1 : -1;
        int startRow = color == ChessPieceColor.White ? 1 : boardSize - 2;

        int oneRow = row + dir;
        if (InBounds(oneRow, col) && !colorGrid[oneRow, col].HasValue)
        {
            moves.Add(new Vector2Int(oneRow, col));

            int twoRow = row + dir * 2;
            if (row == startRow && InBounds(twoRow, col) && !colorGrid[twoRow, col].HasValue)
            {
                moves.Add(new Vector2Int(twoRow, col));
            }
        }

        foreach (int dc in new int[] { -1, 1 })
        {
            int r = row + dir;
            int c = col + dc;
            if (InBounds(r, c) && colorGrid[r, c].HasValue && colorGrid[r, c].Value != color)
            {
                moves.Add(new Vector2Int(r, c));
            }
        }
    }

    private void AddKnightMoves(int row, int col, ChessPieceColor color, ChessPieceColor?[,] colorGrid, List<Vector2Int> moves)
    {
        foreach (Vector2Int offset in KnightOffsets)
        {
            int r = row + offset.x;
            int c = col + offset.y;
            if (InBounds(r, c) && (!colorGrid[r, c].HasValue || colorGrid[r, c].Value != color))
            {
                moves.Add(new Vector2Int(r, c));
            }
        }
    }

    private void AddKingMoves(int row, int col, ChessPieceColor color, ChessPieceColor?[,] colorGrid, List<Vector2Int> moves)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int r = row + dr;
                int c = col + dc;
                if (InBounds(r, c) && (!colorGrid[r, c].HasValue || colorGrid[r, c].Value != color))
                {
                    moves.Add(new Vector2Int(r, c));
                }
            }
        }
    }

    private void AddSlidingMoves(int row, int col, ChessPieceColor color, ChessPieceColor?[,] colorGrid, List<Vector2Int> moves, Vector2Int[] directions)
    {
        foreach (Vector2Int dir in directions)
        {
            int r = row + dir.x;
            int c = col + dir.y;
            while (InBounds(r, c))
            {
                if (!colorGrid[r, c].HasValue)
                {
                    moves.Add(new Vector2Int(r, c));
                }
                else
                {
                    if (colorGrid[r, c].Value != color) moves.Add(new Vector2Int(r, c));
                    break;
                }
                r += dir.x;
                c += dir.y;
            }
        }
    }
}