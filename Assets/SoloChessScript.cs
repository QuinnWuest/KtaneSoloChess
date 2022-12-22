using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

public class SoloChessScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    public class SoloChessPuzzle
    {
        public ChessPiece[] Board;

        public SoloChessPuzzle(ChessPiece[] board)
        {
            Board = board;
        }

        // Somewhere here this is inf looping
        public SoloChessPuzzle GeneratePuzzle()
        {
            tryAgain:
            // Initial setup:
            // - Fill the Board with a ChessPiece array containing "None" pieces
            // - Place one random starting piece on the board that isn't a "None" piece
            Board = new ChessPiece[64].Select(i => new ChessPiece(Piece.None)).ToArray();
            int startPos = Rnd.Range(0, 64);
            Board[startPos] = new ChessPiece((Piece)Rnd.Range(1, Enum.GetValues(typeof(Piece)).Length));
            int pieceGoal = Rnd.Range(7, 12);
            while (GetPieceCount() <= pieceGoal)
            {
                // Through each iteration, what I'm trying to do is:
                // - Pick a random piece on the board
                // - Generate a new piece
                // - Move the original piece on the board to a new location
                // - Place the generated piece into the original position (as if to reverse capture)
                // - Return the board once the number of pieces on the board is equal to the pieceGoal.
                var placedPieces = Board.Where(i => i.PieceType != Piece.None).ToArray();
                if (placedPieces.Length == 0)
                    goto tryAgain;
                var randNewPiece = GetRandomNewPiece();
                var randCurrentPiece = placedPieces[Rnd.Range(0, placedPieces.Length)];
                var currentPieceIx = Array.IndexOf(Board, randCurrentPiece);
                var getPlacesToMove = randCurrentPiece.GetAvailableSquares(Board, randCurrentPiece.PieceType, currentPieceIx, true).ToArray();
                if (getPlacesToMove.Length == 0)
                    continue;
                var rndNewPosition = getPlacesToMove[Rnd.Range(0, getPlacesToMove.Length)];
                Board[rndNewPosition] = randCurrentPiece;
                Board[currentPieceIx] = randNewPiece;
            }
            return new SoloChessPuzzle(Board);
        }

        public ChessPiece GetRandomNewPiece()
        {
            return new ChessPiece((Piece)Rnd.Range(1, Enum.GetValues(typeof(Piece)).Length));
        }

        public int GetPieceCount()
        {
            return Board.Where(i => i.PieceType != Piece.None).Count();
        }

        public override string ToString()
        {
            return string.Format("{0}", Board.Select(i => i.ToString()).Join(""));
        }
    }

    public class ChessPiece
    {
        public Piece PieceType;

        public ChessPiece(Piece pieceType)
        {
            PieceType = pieceType;
        }

        public Piece GetPiece()
        {
            return PieceType;
        }

        public override string ToString()
        {
            if (PieceType == Piece.None)
                return "-";
            if (PieceType == Piece.Pawn)
                return "P";
            if (PieceType == Piece.King)
                return "K";
            if (PieceType == Piece.Knight)
                return "N";
            if (PieceType == Piece.Bishop)
                return "B";
            if (PieceType == Piece.Rook)
                return "R";
            if (PieceType == Piece.Queen)
                return "Q";
            throw new InvalidOperationException("Attempted to call ToString() on an invalid piece: " + PieceType);
        }

        public IEnumerable<int> GetAvailableSquares(ChessPiece[] board, Piece pType, int position, bool pawnBackward = false)
        {
            if (pType == Piece.None)
                yield break;
            if (pType == Piece.Pawn)
            {
                // "pawnBackward" is used for both the puzzle generation (backward),
                // as well as moving piece logic (forward), which is yet to be generated.
                // Only needed for pawns, since every other piece can move anywhere regardless of direction.
                if (!pawnBackward)
                {
                    if (position / 8 == 7)
                        yield break;
                    var a = board[position + 8];
                    if (position / 8 == 1 && board[position + 16].PieceType == Piece.None)
                        yield return position + 16;
                    if (board[position + 8].PieceType == Piece.None)
                        yield return position + 8;
                    if (position % 8 != 0 && board[position + 7].PieceType != Piece.None)
                        yield return position + 7;
                    if (position % 8 != 7 && board[position + 9].PieceType != Piece.None)
                        yield return position + 9;
                }
                else
                {
                    if (position / 8 == 0)
                        yield break;
                    if (position / 8 == 3 && board[position - 16].PieceType == Piece.None)
                        yield return position - 16;
                    if (board[position - 8].PieceType == Piece.None)
                        yield return position - 8;
                    if (position % 8 != 0 && board[position - 9].PieceType != Piece.None)
                        yield return position - 9;
                    if (position % 8 != 7 && board[position - 7].PieceType != Piece.None)
                        yield return position - 7;
                }
            }
            if (pType == Piece.Knight)
            {
                if (position % 8 > 0 && position / 8 > 1)
                    yield return position - 17;
                if (position % 8 > 1 && position / 8 > 0)
                    yield return position - 10;
                if (position % 8 > 0 && position / 8 < 6)
                    yield return position + 15;
                if (position % 8 > 1 && position / 8 < 7)
                    yield return position + 6;
                if (position % 8 < 7 && position / 8 > 1)
                    yield return position - 15;
                if (position % 8 < 6 && position / 8 > 0)
                    yield return position - 6;
                if (position % 8 < 7 && position / 8 < 6)
                    yield return position + 17;
                if (position % 8 < 6 && position / 8 < 7)
                    yield return position + 10;
            }
            if (pType == Piece.King)
            {
                if (position % 8 > 0)
                    yield return position - 1;
                if (position % 8 < 7)
                    yield return position + 1;
                if (position / 8 > 0)
                    yield return position - 8;
                if (position / 8 < 7)
                    yield return position + 8;
                if (position % 8 > 0 & position / 8 > 0)
                    yield return position - 9;
                if (position % 8 < 7 & position / 8 > 0)
                    yield return position - 7;
                if (position % 8 > 0 && position / 8 < 7)
                    yield return position + 9;
                if (position % 8 < 7 && position / 8 < 7)
                    yield return position + 7;
            }
            if (pType == Piece.Bishop || pType == Piece.Queen)
            {
                // I removed the yield breaks here because the Queen still needs to
                // go to the Rook square checking. (maybe didn't need to)
                var p = position;
                while (p % 8 > 0 && p / 8 > 0)
                {
                    p -= 9;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 0;
                }
                p = position;
                while (p % 8 < 7 && p / 8 > 0)
                {
                    p -= 7;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 7;
                }
                p = position;
                while (p % 8 > 0 && p / 8 < 7)
                {
                    p += 7;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 0;
                }
                p = position;
                while (p % 8 < 7 && p / 8 < 7)
                {
                    p += 9;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 7;
                }
            }
            if (pType == Piece.Rook || pType == Piece.Queen)
            {
                var p = position;
                while (p % 8 > 0)
                {
                    p--;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 0;
                }
                p = position;
                while (p % 8 < 7)
                {
                    p++;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 7;
                }
                p = position;
                while (p / 8 > 0)
                {
                    p -= 8;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 0;
                }
                p = position;
                while (p / 8 < 7)
                {
                    p += 8;
                    yield return p;
                    if (board[p].PieceType != Piece.None)
                        p = 7;
                }
            }
        }
    }

    public enum Piece
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    private SoloChessPuzzle _puzzle = new SoloChessPuzzle(new ChessPiece[64]);

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _puzzle = _puzzle.GeneratePuzzle();
        var str = _puzzle.ToString();
        Debug.Log(str);
    }
}
