using MinimalChess;
using System;

namespace MinimalChessBoard
{
    static class AlgebraicNotation
    {
        public static Move ToMove(Board board, string notation)
        {
            //trim check and checkmate symbols.
            notation = notation.TrimEnd('+', '#');

            //queenside castling
            if (notation == "O-O-O" || notation == "0-0-0")
            {
                if (board.SideToMove == Color.White)
                    return Move.WhiteCastlingLong;
                else
                    return Move.BlackCastlingLong;
            }

            //kingside castling
            if (notation == "O-O" || notation == "0-0")
            {
                if (board.SideToMove == Color.White)
                    return Move.WhiteCastlingShort;
                else
                    return Move.BlackCastlingShort;
            }

            //promotion
            Piece promotion = (notation[^2] == '=') ? Notation.ToPiece(notation[^1]).OfColor(board.SideToMove) : default;

            //pawns?
            if (char.IsLower(notation, 0))
            {
                if (notation[1] == 'x')
                {
                    //pawn capture
                    int toSquare = Notation.ToSquare(notation.Substring(2, 2));
                    return SelectMove(board, Piece.Pawn.OfColor(board.SideToMove), toSquare, promotion, notation[0]);
                }
                else
                {
                    //pawn move
                    int toSquare = Notation.ToSquare(notation);
                    return SelectMove(board, Piece.Pawn.OfColor(board.SideToMove), toSquare, promotion);
                }
            }

            Piece piece = Notation.ToPiece(notation[0]).OfColor(board.SideToMove);
            if (notation[1] == 'x')
            {
                //capture
                int toSquare = Notation.ToSquare(notation.Substring(2, 2));
                return SelectMove(board, piece, toSquare, promotion);
            }
            else if (notation[2] == 'x')
            {
                //piece capture with disambiguation
                int toSquare = Notation.ToSquare(notation.Substring(3, 2));
                return SelectMove(board, piece, toSquare, promotion, notation[1]);
            }
            else if (notation.Length == 3)
            {
                //move
                int toSquare = Notation.ToSquare(notation.Substring(1, 2));
                return SelectMove(board, piece, toSquare, promotion);
            }
            else if (notation.Length == 4)
            {
                //move with disambiguation
                int toSquare = Notation.ToSquare(notation.Substring(2, 2));
                return SelectMove(board, piece, toSquare, promotion, notation[1]);
            }

            throw new ArgumentException($"Move notation {notation} could not be parsed!");
        }

        private static Move SelectMove(Board board, Piece moving, int toSquare, Piece promotion, char? fileOrRank = null)
        {
            foreach (var move in new LegalMoves(board))
            {
                if (move.ToSquare != toSquare)
                    continue;
                if (move.Promotion != promotion)
                    continue;
                if (board[move.FromSquare] != moving)
                    continue;
                if (fileOrRank != null && !Notation.ToSquareName(move.FromSquare).Contains(fileOrRank.Value))
                    continue;

                return move; //this is the move!
            }
            throw new ArgumentException("No move meeting all requirements could be found!");
        }
    }
}