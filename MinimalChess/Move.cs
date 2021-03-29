﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MinimalChess
{
    public enum MoveFlags
    {
        Empty = 0,
        Capture = 1,
        PV      = 2,
        Killer  = 4,
        History = 8
    }

    public struct Move
    {
        public byte FromIndex;
        public byte ToIndex;
        public Piece Promotion;
        public MoveFlags Flags;


        public Move(int fromIndex, int toIndex)
        {
            Flags = MoveFlags.Empty;
            FromIndex = (byte)fromIndex;
            ToIndex = (byte)toIndex;
            Promotion = Piece.None;
        }

        public Move(int fromIndex, int toIndex, Piece promotion)
        {
            Flags = MoveFlags.Empty;
            FromIndex = (byte)fromIndex;
            ToIndex = (byte)toIndex;
            Promotion = promotion;
        }

        public Move(string uciMoveNotation)
        {
            if (uciMoveNotation.Length < 4)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too short!");
            if (uciMoveNotation.Length > 5)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too long!");

            //expected format is the long algebraic notation without piece names
            https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: e2e4, e7e5, e1g1(white short castling), e7e8q(for promotion)
            string fromSquare = uciMoveNotation.Substring(0, 2);
            string toSquare = uciMoveNotation.Substring(2, 2);
            FromIndex = Notation.ToSquareIndex(fromSquare);
            ToIndex = Notation.ToSquareIndex(toSquare);
            //the presence of a 5th character should mean promotion
            Promotion = (uciMoveNotation.Length == 5) ? Notation.ToPiece(uciMoveNotation[4]) : Piece.None;
            Flags = MoveFlags.Empty;
        }

        public bool HasFlags(MoveFlags mask)
        {
            return (Flags & mask) == mask;
        }

        public override bool Equals(object obj)
        {
            if (obj is Move move)
                return this.Equals(move);

            return false;
        }

        public bool Equals(Move other)
        {
            return (FromIndex == other.FromIndex) && (ToIndex == other.ToIndex) && (Promotion == other.Promotion);
        }

        public override int GetHashCode()
        {
            //int is big enough to represent move fully. maybe use that for optimization at some point
            return FromIndex + (ToIndex << 8) + ((int)Promotion << 16);
        }

        public static bool operator ==(Move lhs, Move rhs) => lhs.Equals(rhs);

        public static bool operator !=(Move lhs, Move rhs) => !lhs.Equals(rhs);

        public override string ToString()
        {
            //result represents the move in the long algebraic notation (without piece names)
            string result = Notation.ToSquareName(FromIndex);
            result += Notation.ToSquareName(ToIndex);
            //the presence of a 5th character should mean promotion
            if (Promotion != Piece.None)
                result += Notation.ToChar(Promotion);

            return result;
        }

        public static Move BlackCastlingShort = new Move("e8g8");
        public static Move BlackCastlingLong = new Move("e8c8");
        public static Move WhiteCastlingShort = new Move("e1g1");
        public static Move WhiteCastlingLong = new Move("e1c1");

        public static Move BlackCastlingShortRook = new Move("h8f8");
        public static Move BlackCastlingLongRook = new Move("a8d8");
        public static Move WhiteCastlingShortRook = new Move("h1f1");
        public static Move WhiteCastlingLongRook = new Move("a1d1");
    }

    public interface IMovesVisitor
    {
        public bool Done { get; }
        public void Consider(Move move);
        void AddUnchecked(Move move);
    }

    public class LegalMoves : List<Move>, IMovesVisitor
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public LegalMoves(Board reference) : base(40)
        {
            _reference = reference;
            _reference.CollectMoves(this);
            _reference = null;
        }

        public bool Done => false;

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            _tempBoard.Copy(_reference);
            _tempBoard.Play(move);
            if (_tempBoard.IsChecked(_reference.ActiveColor))
                return;

            Add(move);
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        public void Randomize()
        {
            Random rnd = new Random();
            for(int i = 0; i < Count; i++)
            {
                int j = rnd.Next(0, Count);
                //swap i with j
                Move temp = this[i];
                this[i] = this[j];
                this[j] = temp;
            }
        }
    }

    public class PseudoLegalMoves : List<Move>, IMovesVisitor
    {
        private Board _reference;

        public PseudoLegalMoves(Board reference) : base(40)
        {
            _reference = reference;
            _reference.CollectMoves(this);
            _reference = null;
        }

        public bool Done => false;

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            Add(move);
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        public void Randomize()
        {
            Random rnd = new Random();
            for (int i = 0; i < Count; i++)
            {
                int j = rnd.Next(0, Count);
                //swap i with j
                Move temp = this[i];
                this[i] = this[j];
                this[j] = temp;
            }
        }
    }

    public class AnyLegalMoves : IMovesVisitor
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public bool CanMove { get; private set; }

        public AnyLegalMoves(Board reference)
        {
            _reference = reference;
            _reference.CollectMoves(this);
            _reference = null;
        }

        public bool Done => CanMove;

        public void Consider(Move move)
        {
            if (CanMove)//no need to look at any more moves if we got our answer already!
                return;

            //only add if the move doesn't result in a check for active color
            _tempBoard.Copy(_reference);
            _tempBoard.Play(move);
            if (_tempBoard.IsChecked(_reference.ActiveColor))
                return;

            CanMove = true;
        }

        public void AddUnchecked(Move move)
        {
            CanMove = true;
        }
    }


    public static class MoveOrdering
    {
        public static void SortMvvLva(List<Move> moves, Board context)
        {
            int Score(Move move)
            {
                Piece victim = context[move.ToIndex];
                if (victim == Piece.None)
                    return 0;//we don't sort those
                Piece attacker = context[move.FromIndex];
                //Rating: Victims value first - offset by the attackers value
                return ((100 * (int)victim) - (int)attacker);
            }
            moves.Sort((a, b) => Score(b).CompareTo(Score(a)));
        }

        public static void RemoveNonCapturesAndSortMvvLva(List<Move> moves, Board context)
        {
            //remove all non captures
            moves.RemoveAll(move => context[move.ToIndex] == Piece.None);
            //and sort the rest
            SortMvvLva(moves, context);
        }
    }



    public class MoveSequence : IMovesVisitor
    {
        List<(int Score, Move Move)> _captures;
        List<Move> _nonCaptures = null;
        Board _parentNode;
        bool _includeNonCaptures;

        public static MoveSequence FromList(Board position, List<Move> moves)
        {
            return new MoveSequence(position, moves);
        }

        public static MoveSequence AllMoves(Board position)
        {
            return new MoveSequence(position, true);
        }
        public static MoveSequence CapturesOnly(Board position)
        {
            return new MoveSequence(position, false);
        }

        private MoveSequence(Board parent, bool includeNonCaptures)
        {
            _parentNode = parent;
            _includeNonCaptures = includeNonCaptures;
            _captures = new List<(int, Move)>(10);
            if (_includeNonCaptures)
                _nonCaptures = new List<Move>(40);

            _parentNode.CollectMoves(this);
        }

        public MoveSequence(Board parent, List<Move> moves)
        {
            _parentNode = parent;
            _includeNonCaptures = true;
            _captures = new List<(int, Move)>(10);
            _nonCaptures = new List<Move>(moves.Count); //won't need more space than this

            foreach (var move in moves)
                Add(move);
        }

        internal MoveSequence Boost(Move move)
        {
            int priorityScore = Pieces.MaxRank * Pieces.MaxRank;
            Piece victim = _parentNode[move.ToIndex];
            if (victim == Piece.None)
            {
                if(_nonCaptures.Remove(move))
                    _captures.Add((priorityScore, move));
            }
            else
            {
                int index = _captures.FindIndex(0, entry => entry.Move == move);
                if (index >= 0)
                    _captures[index] = (priorityScore, move);
            }
            return this;
        }

        public MoveSequence SortCaptures()
        {
            _captures.Sort((a, b) => b.Score.CompareTo(a.Score));
            return this;
        }

        private void Add(Move move)
        {
            //*** MVV-LVA ***
            //Sort by the value of the victim in descending order.
            //Groups of same-value victims are sorted by value of attecer in ascending order.
            //***************
            Piece victim = _parentNode[move.ToIndex];
            if (victim != Piece.None)
            {
                Piece attacker = _parentNode[move.FromIndex];
                //We can compute a rating that produces this order in one sorting pass:
                //-> scale the victim value by max rank and then offset it by the attacker value
                int score = Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
                _captures.Add((score, move));
            }
            else if (_includeNonCaptures)
            {
                _nonCaptures.Add(move);
            }
        }

        private bool TryMove(Move move, out Board childNode)
        {
            //because the move was 'pseudolegal' we make sure it doesnt result in a position 
            //where our (color) king is in check - in that case we can't play it
            childNode = new Board(_parentNode, move);
            return !childNode.IsChecked(_parentNode.ActiveColor);
        }

        internal IEnumerable<Board> Play()
        {
            //Return the best capture and remove it until captures are depleated
            foreach (var capture in _captures)
                if (TryMove(capture.Move, out Board childNode))
                    yield return childNode;

            //Return nonCaptures in any order until stack is depleated
            if(_includeNonCaptures)
                foreach (var move in _nonCaptures)
                    if (TryMove(move, out Board childNode))
                        yield return childNode;
        }

        internal IEnumerable<(Move, Board)> PlayMoves()
        {
            //Return the best capture and remove it until captures are depleated
            foreach (var capture in _captures)
                if (TryMove(capture.Move, out Board childNode))
                    yield return (capture.Move, childNode);

            //Return nonCaptures in any order until stack is depleated
            if (_includeNonCaptures)
                foreach (var move in _nonCaptures)
                    if (TryMove(move, out Board childNode))
                        yield return (move, childNode);
        }

        public int Count => _captures.Count + _nonCaptures?.Count ?? 0;

        public bool Done => false;

        public void Consider(Move move) => Add(move);

        public void AddUnchecked(Move move) => Add(move);
    }


    /*
    class QSearchSortedMoveSequence : IMovesVisitor
    {
        List<(int Score, Move Move)> _scoredCaptures;
        List<(int Score, Move Move)> _recaptures;
        List<(int Score, Move Move)> _pvMoves;
        List<Move> _moves;
        Board _parentNode;
        PrincipalVariation _pv;
        int _pvDepth;
        Move _lastMove;

        public QSearchSortedMoveSequence(Board parent, PrincipalVariation pv, int pvDepth, Move lastMove)
        {
            _parentNode = parent;
            _moves = new List<Move>(40);
            _pvMoves = new List<(int Score, Move Move)>(pvDepth);
            _recaptures = new List<(int Score, Move Move)>(3);
            _pv = pv;
            _pvDepth = pvDepth;
            _lastMove = lastMove;
            _parentNode.CollectMoves(this);
        }

        public QSearchSortedMoveSequence(Board parent, List<Move> moves, PrincipalVariation pv, int pvDepth)
        {
            _parentNode = parent;
            _moves = new List<Move>(40);
            _pvMoves = new List<(int Score, Move Move)>(pvDepth);
            _recaptures = new List<(int Score, Move Move)>(3);
            _pv = pv;
            _pvDepth = pvDepth;
            foreach (var move in moves)
                Add(move);
        }

        private void Add(Move move)
        {
            if (_pv.Contains(move, _pvDepth, 1, out int offset))
            {
                _pvMoves.Add((-offset, move));
            }
            else if(_lastMove != default && move.ToIndex == _lastMove.ToIndex)
            {
                _recaptures.Add((0, move));
            }
            else 
                _moves.Add(move);
        }

        internal IEnumerable<(Move, Board)> PlayMoves()
        {
            //string pvString = string.Join(' ', _pvMoves);
            //Console.WriteLine(pvString);
            //_pvMoves.Sort((a, b) => b.Score.CompareTo(a.Score));
            foreach (var move in _pvMoves)
            {
                var childNode = new Board(_parentNode, move.Move);
                if(!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return (move.Move, childNode);
            }

            foreach (var move in _recaptures)
            {
                var childNode = new Board(_parentNode, move.Move);
                if (!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return (move.Move, childNode);
            }

            _scoredCaptures = new List<(int Score, Move Move)>(10);
            foreach (var move in _moves)
            {
                Piece victim = _parentNode[move.ToIndex];
                if (victim == Piece.None)
                    continue;

                Board childNode = new Board(_parentNode, move);
                if (childNode.IsChecked(_parentNode.ActiveColor))
                    continue;

                Piece attacker = _parentNode[move.FromIndex];
                //We can compute a rating that produces this order in one sorting pass:
                //-> scale the victim value by max rank and then offset it by the attacker value
                int score = Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
                //score *= 50;
                //score = PeSTO.Evaluate(childNode) * (int)_parentNode.ActiveColor;
                //score -= Pieces.Rank(attacker);
                _scoredCaptures.Add((score, move));
            }

            _scoredCaptures.Sort((a, b) => b.Score.CompareTo(a.Score));

            //Return the best capture and remove it until captures are depleated
            foreach (var move in _scoredCaptures)
            {
                //Board childNode = new Board(_parentNode, move.Move);
                //if (!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return (move.Move, new Board(_parentNode, move.Move));
            }

            //_scoredMoves = new List<(int Score, Move Move)>(_moves.Count);
            foreach (var move in _moves)
            {
                Piece victim = _parentNode[move.ToIndex];
                if (victim != Piece.None)
                    continue;

                Board childNode = new Board(_parentNode, move);
                if (childNode.IsChecked(_parentNode.ActiveColor))
                    continue;

                yield return (move, childNode);

                //int score = PeSTO.Evaluate(childNode) * (int)_parentNode.ActiveColor;
                //score -= Pieces.Rank(_parentNode[move.FromIndex]);
                //_scoredMoves.Add((score, move));
            }

            //_scoredMoves.Sort((a, b) => b.Score.CompareTo(a.Score));
            //
            ////Return the best capture and remove it until captures are depleated
            //foreach (var move in _scoredMoves)
            //    yield return (move.Move, new Board(_parentNode, move.Move));
        }

        public int Count => _moves.Count;

        public bool Done => false;

        public void Consider(Move move) => Add(move);

        public void Consider(int from, int to, Piece promotion) => Add(new Move(from, to, promotion));

        public void Consider(int from, int to) => Add(new Move(from, to));

        public void AddUnchecked(Move move) => Add(move);
    }
    */
}
