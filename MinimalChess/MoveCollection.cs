using System;
using System.Collections.Generic;

namespace MinimalChess
{

    public interface IMovesVisitor
    {
        public bool Done { get; }
        public void Consider(Move move);
        public void Consider(int from, int to);
        public void Consider(int from, int to, Piece promotion);
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

        public void Consider(int from, int to, Piece promotion)
        {
            Consider(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Consider(new Move(from, to));
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

        public void Consider(int from, int to, Piece promotion)
        {
            Consider(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Consider(new Move(from, to));
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
                if (_nonCaptures.Remove(move))
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
            if (_includeNonCaptures)
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

        public void Consider(int from, int to, Piece promotion) => Add(new Move(from, to, promotion));

        public void Consider(int from, int to) => Add(new Move(from, to));

        public void AddUnchecked(Move move) => Add(move);
    }
}
