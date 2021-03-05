using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MinimalChess
{
    public struct Move
    {
        public byte FromIndex;
        public byte ToIndex;
        public Piece Promotion;

        public Move(byte fromIndex, byte toIndex, Piece promotion)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Promotion = promotion;
        }

        public Move(int fromIndex, int toIndex)
        {
            FromIndex = (byte)fromIndex;
            ToIndex = (byte)toIndex;
            Promotion = Piece.None;
        }

        public Move(int fromIndex, int toIndex, Piece promotion)
        {
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

        public void Consider(int from, int to, Piece promotion)
        {
            Add(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Add(new Move(from, to));
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

    public class ChildNodes : IMovesVisitor, IEnumerable<Board>
    {
        struct MoveEntry
        {
            public Move Move;
            public int Score;

            public MoveEntry(Move move, int score)
            {
                Move = move;
                Score = score;
            }
        }

        bool _includeNonCaptures;
        List<MoveEntry> _captures;
        Stack<Move> _nonCaptures = null;
        Board _parentNode;

        public ChildNodes(Board parentPosition, bool includeNonCaptures)
        {
            _parentNode = parentPosition;
            _includeNonCaptures = includeNonCaptures;
        }

        private void Add(Move move)
        {
            int score = ComputeMvvLvaRating(move);
            if (score >= 0)
                _captures.Add(new MoveEntry(move, score));
            else if (_nonCaptures != null)
                _nonCaptures.Push(move);
        }

        private int ComputeMvvLvaRating(Move move)
        {
            //*** MVV-LVA ***
            //Sort by the value of the victim in descending order.
            //Groups of same-value victims are sorted by value of attecer in ascending order.
            //***************
            Piece attacker = _parentNode[move.FromIndex];
            Piece victim = _parentNode[move.ToIndex];
            //We can compute a rating that produces this order in one sorting pass:
            //-> scale the victim value by max rank and then offset it by the attacker value
            return Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
        }

        public IEnumerator<Board> GetEnumerator()
        {
            _captures = new List<MoveEntry>(10);
            if (_includeNonCaptures)
                _nonCaptures = new Stack<Move>(40);
            _parentNode.CollectMoves(this);

            //Return the best capture and remove it until captures are depleated
            while (_captures.Count > 0)
            {
                int last = _captures.Count - 1;
                int bestScore = int.MinValue;
                int iBest = -1;
                for (int i = 0; i <= last; i++)
                {
                    if (_captures[i].Score <= bestScore)
                        continue;

                    bestScore = _captures[i].Score;
                    iBest = i;
                }

                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                var childNode = new Board(_parentNode, _captures[iBest].Move);
                if (!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return childNode;

                //swap and cheaply remove last
                _captures[iBest] = _captures[last];
                _captures.RemoveAt(last);
            }

            //Return nonCaptures in any order until stack is depleated
            while (_nonCaptures?.Count > 0)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                var childNode = new Board(_parentNode, _nonCaptures.Pop());
                if (!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return childNode;
            }
        }

        //INTERFACES

        public bool Done => false;

        public bool AnyLegalMoves
        {
            get
            {
                var moves = new AnyLegalMoves(_parentNode);
                return moves.CanMove;
            }
        }

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            Add(move);
        }

        public void Consider(int from, int to, Piece promotion)
        {
            Add(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Add(new Move(from, to));
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ChildNodes2 : IMovesVisitor, IEnumerable<Board>
    {
        Board _parentNode;
        bool _includeNonCaptures;
        int _next;
        List<(int, Move)> _moves = new List<(int, Move)>(40);
        
        public ChildNodes2(Board parentPosition, bool includeNonCaptures)
        {
            //Getting *all* possible moves in that position for 'color'
            _includeNonCaptures = includeNonCaptures;
            _parentNode = parentPosition;
            _parentNode.CollectMoves(this);
            _moves.Sort((a, b) => b.Item1.CompareTo(a.Item1));
        }

        private void Add(Move move)
        {
            int score = ComputeMvvLvaRating(move);
            if (_includeNonCaptures || score >= 0)
                _moves.Add((score, move));
        }

        private int ComputeMvvLvaRating(Move move)
        {
            //*** MVV-LVA ***
            //Sort by the value of the victim in descending order.
            //Groups of same-value victims are sorted by value of attecer in ascending order.
            //***************
            Piece attacker = _parentNode[move.FromIndex];
            Piece victim = _parentNode[move.ToIndex];
            //We can compute a rating that produces this order in one sorting pass:
            //-> scale the victim value by max rank and then offset it by the attacker value
            return Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
        }

        public bool Next(out Board childNode)
        {
            while (_next < _moves.Count)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                (_, var move) = _moves[_next++];
                childNode = new Board(_parentNode, move);
                if (!childNode.IsChecked(_parentNode.ActiveColor))
                    return true;
            }
            childNode = null;
            return false;
        }

        public IEnumerator<Board> GetEnumerator()
        {
            while (_next < _moves.Count)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                (_, var move) = _moves[_next++];
                var childNode = new Board(_parentNode, move);
                if (!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return childNode;
            }
        }

        //INTERFACES

        public bool Done => false;

        public bool AnyLegalMoves
        {
            get
            {
                var moves = new AnyLegalMoves(_parentNode);
                return moves.CanMove;
            }
        }

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            Add(move);
        }

        public void Consider(int from, int to, Piece promotion)
        {
            Add(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Add(new Move(from, to));
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /*
    public class ChildNodes3 : IMovesVisitor, IEnumerable<Board>
    {
        struct MoveEntry : IComparable<MoveEntry>
        {
            public Move Move;
            public int Score;

            public MoveEntry(Move move, int score)
            {
                Move = move;
                Score = score;
            }

            public int CompareTo([AllowNull] MoveEntry other)
            {
                return -Score.CompareTo(other.Score);
            }
        }

        Board _parentNode;
        bool _includeNonCaptures;
        PriorityQueue<MoveEntry> _moves;

        public ChildNodes3(Board parentPosition, bool includeNonCaptures)
        {
            //Getting *all* possible moves in that position for 'color'
            _includeNonCaptures = includeNonCaptures;
            _parentNode = parentPosition;
        }

        private void Add(Move move)
        {
            int score = ComputeMvvLvaRating(move);
            if (_includeNonCaptures || score >= 0)
                _moves.Enqueue(new MoveEntry(move, score));
        }

        private int ComputeMvvLvaRating(Move move)
        {
            //*** MVV-LVA ***
            //Sort by the value of the victim in descending order.
            //Groups of same-value victims are sorted by value of attecer in ascending order.
            //***************
            Piece attacker = _parentNode[move.FromIndex];
            Piece victim = _parentNode[move.ToIndex];
            //We can compute a rating that produces this order in one sorting pass:
            //-> scale the victim value by max rank and then offset it by the attacker value
            return Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
        }

        public IEnumerator<Board> GetEnumerator()
        {
            _moves = new PriorityQueue<MoveEntry>(40);
            _parentNode.CollectMoves(this);
            while (_moves.Count > 0)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                var move = _moves.Dequeue().Move;
                var childNode = new Board(_parentNode, move);
                if (!childNode.IsChecked(_parentNode.ActiveColor))
                    yield return childNode;
            }
        }

        //INTERFACES

        public bool Done => false;

        public bool AnyLegalMoves
        {
            get
            {
                var moves = new AnyLegalMoves(_parentNode);
                return moves.CanMove;
            }
        }

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            Add(move);
        }

        public void Consider(int from, int to, Piece promotion)
        {
            Add(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Add(new Move(from, to));
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    */

    public class ChildNodes4 : IMovesVisitor, IEnumerable<Board>
    {
        List<(int Score, Move Move)> _captures;
        List<Move> _nonCaptures = null;
        Board _parentNode;
        bool _includeNonCaptures;

        public ChildNodes4(Board parentPosition, bool includeNonCaptures)
        {
            _parentNode = parentPosition;
            _includeNonCaptures = includeNonCaptures;
        }

        private void Add(Move move)
        {
            //*** MVV-LVA ***
            //Sort by the value of the victim in descending order.
            //Groups of same-value victims are sorted by value of attecer in ascending order.
            //***************
            Piece victim = _parentNode[move.ToIndex];
            if(victim != Piece.None)
            {
                Piece attacker = _parentNode[move.FromIndex];
                //We can compute a rating that produces this order in one sorting pass:
                //-> scale the victim value by max rank and then offset it by the attacker value
                int score = Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
                _captures.Add((score, move));
            }
            else if(_includeNonCaptures)
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

        public IEnumerator<Board> GetEnumerator()
        {
            _captures = new List<(int, Move)>(10);
            if (_includeNonCaptures)
                _nonCaptures = new List<Move>(40);
            
            _parentNode.CollectMoves(this);
            _captures.Sort((a, b) => b.Score.CompareTo(a.Score));

            //Return the best capture and remove it until captures are depleated
            foreach(var capture in _captures)
                if(TryMove(capture.Move, out Board childNode))
                    yield return childNode;

            if (_nonCaptures == null)
                yield break;

            //Return nonCaptures in any order until stack is depleated
            foreach (var move in _nonCaptures)
                if (TryMove(move, out Board childNode))
                    yield return childNode;
        }

        public bool Done => false;

        public bool AnyLegalMoves
        {
            get
            {
                var moves = new AnyLegalMoves(_parentNode);
                return moves.CanMove;
            }
        }

        public void Consider(Move move) => Add(move);

        public void Consider(int from, int to, Piece promotion) => Add(new Move(from, to, promotion));

        public void Consider(int from, int to) => Add(new Move(from, to));

        public void AddUnchecked(Move move) => Add(move);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
