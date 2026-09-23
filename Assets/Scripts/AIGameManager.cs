using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ChessGameManager
{
    private static readonly Dictionary<ChessPieceType, int> PieceValues = new Dictionary<ChessPieceType, int>
    {
        { ChessPieceType.Pawn, 100 },
        { ChessPieceType.Knight, 320 },
        { ChessPieceType.Bishop, 330 },
        { ChessPieceType.Rook, 500 },
        { ChessPieceType.Queen, 900 },
        { ChessPieceType.King, 0 }
    };

    private const int CheckmateScore = 1000000;

    private void TriggerAIMove()
    {
        StartCoroutine(AIMoveCoroutine());
    }

    private IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(aiMoveDelay);

        (Vector2Int from, Vector2Int to) best = ComputeBestMove(aiColor);

        if (best.from.x >= 0)
        {
            ExecuteMove(best.from.x, best.from.y, best.to.x, best.to.y, () => AdvanceTurn());
        }
    }

    private (Vector2Int from, Vector2Int to) ComputeBestMove(ChessPieceColor color)
    {
        List<(Vector2Int from, Vector2Int to)> rootMoves = GetAllLegalMovesOnGrid(pieceType, pieceColor, color);
        if (rootMoves.Count == 0) return (new Vector2Int(-1, -1), new Vector2Int(-1, -1));

        int bestScore = int.MinValue;
        List<(Vector2Int from, Vector2Int to)> bestMoves = new List<(Vector2Int, Vector2Int)>();

        foreach ((Vector2Int from, Vector2Int to) move in rootMoves)
        {
            (ChessPieceType?[,] t, ChessPieceColor?[,] c) = CloneAndApply(pieceType, pieceColor, move);
            int score = Minimax(t, c, aiSearchDepth - 1, int.MinValue, int.MaxValue, Opposite(color), color);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        return bestMoves[UnityEngine.Random.Range(0, bestMoves.Count)];
    }

    private int Minimax(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, int depth, int alpha, int beta, ChessPieceColor colorToMove, ChessPieceColor aiPerspective)
    {
        List<(Vector2Int from, Vector2Int to)> moves = GetAllLegalMovesOnGrid(typeGrid, colorGrid, colorToMove);

        if (moves.Count == 0)
        {
            bool inCheck = IsInCheck(typeGrid, colorGrid, colorToMove);
            if (!inCheck) return 0;

            return colorToMove == aiPerspective ? -CheckmateScore - depth : CheckmateScore + depth;
        }

        if (depth == 0)
        {
            return EvaluateBoard(typeGrid, colorGrid, aiPerspective);
        }

        bool maximizing = colorToMove == aiPerspective;

        if (maximizing)
        {
            int best = int.MinValue;
            foreach ((Vector2Int from, Vector2Int to) move in moves)
            {
                (ChessPieceType?[,] t, ChessPieceColor?[,] c) = CloneAndApply(typeGrid, colorGrid, move);
                int val = Minimax(t, c, depth - 1, alpha, beta, Opposite(colorToMove), aiPerspective);
                if (val > best) best = val;
                alpha = Math.Max(alpha, best);
                if (alpha >= beta) break;
            }
            return best;
        }
        else
        {
            int worst = int.MaxValue;
            foreach ((Vector2Int from, Vector2Int to) move in moves)
            {
                (ChessPieceType?[,] t, ChessPieceColor?[,] c) = CloneAndApply(typeGrid, colorGrid, move);
                int val = Minimax(t, c, depth - 1, alpha, beta, Opposite(colorToMove), aiPerspective);
                if (val < worst) worst = val;
                beta = Math.Min(beta, worst);
                if (alpha >= beta) break;
            }
            return worst;
        }
    }

    private int EvaluateBoard(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, ChessPieceColor aiPerspective)
    {
        int score = 0;
        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                if (!colorGrid[r, c].HasValue) continue;
                int value = PieceValues[typeGrid[r, c].Value];
                score += colorGrid[r, c].Value == aiPerspective ? value : -value;
            }
        }
        return score;
    }

    private List<(Vector2Int from, Vector2Int to)> GetAllLegalMovesOnGrid(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, ChessPieceColor color)
    {
        List<(Vector2Int, Vector2Int)> all = new List<(Vector2Int, Vector2Int)>();

        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                if (!colorGrid[r, c].HasValue || colorGrid[r, c].Value != color) continue;

                List<Vector2Int> pseudo = GetPseudoLegalMoves(r, c, typeGrid, colorGrid);
                foreach (Vector2Int move in pseudo)
                {
                    ChessPieceType?[,] tClone = (ChessPieceType?[,])typeGrid.Clone();
                    ChessPieceColor?[,] cClone = (ChessPieceColor?[,])colorGrid.Clone();
                    ApplyMoveOnGrids(tClone, cClone, r, c, move.x, move.y);

                    if (!IsInCheck(tClone, cClone, color))
                    {
                        all.Add((new Vector2Int(r, c), move));
                    }
                }
            }
        }

        return all;
    }

    private (ChessPieceType?[,], ChessPieceColor?[,]) CloneAndApply(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, (Vector2Int from, Vector2Int to) move)
    {
        ChessPieceType?[,] tClone = (ChessPieceType?[,])typeGrid.Clone();
        ChessPieceColor?[,] cClone = (ChessPieceColor?[,])colorGrid.Clone();
        ApplyMoveOnGrids(tClone, cClone, move.from.x, move.from.y, move.to.x, move.to.y);
        return (tClone, cClone);
    }

    private void ApplyMoveOnGrids(ChessPieceType?[,] typeGrid, ChessPieceColor?[,] colorGrid, int fromRow, int fromCol, int toRow, int toCol)
    {
        ChessPieceType movingType = typeGrid[fromRow, fromCol].Value;
        ChessPieceColor movingColor = colorGrid[fromRow, fromCol].Value;

        typeGrid[toRow, toCol] = movingType;
        colorGrid[toRow, toCol] = movingColor;
        typeGrid[fromRow, fromCol] = null;
        colorGrid[fromRow, fromCol] = null;

        bool reachedLastRank = (movingColor == ChessPieceColor.White && toRow == boardSize - 1) ||
                                (movingColor == ChessPieceColor.Black && toRow == 0);
        if (movingType == ChessPieceType.Pawn && reachedLastRank)
        {
            typeGrid[toRow, toCol] = ChessPieceType.Queen;
        }
    }
}