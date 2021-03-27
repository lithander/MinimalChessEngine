using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class Killers
    {
        List<Move> _moves = new List<Move>();
        List<Move> _backup = new List<Move>();
        int _depth = -1;

        public void Grow(int depth)
        {
            _depth = depth;
            while (_moves.Count < _depth)
                _moves.Add(default);

            while (_backup.Count < _depth)
                _backup.Add(default);
        }

        public void Consider(Board position, Move move, int depth)
        {
            if (position[move.ToIndex] != Piece.None)
                return; //not a quiet move!

            int index = _depth - depth;
            if(_moves[index] != move)
            {
                _backup[index] = _moves[index];
                _moves[index] = move;
            }
        }

        public bool Contains(Move move, int depth)
        {
            int index = _depth - depth;
            return (_moves[index] == move || _backup[index] == move);
        }

        public int Index(int depth) => _depth - depth;
    }

    public class History
    {
        const int SIZE = 64 * 64;
        long[] _dec = new long[SIZE];
        long[] _inc = new long[SIZE];

        int Index(Move move) => move.FromIndex + (move.ToIndex << 6);

        public void Increase(Move move, int depth)
        {
            _inc[Index(move)] += depth * depth;
        }

        public void Decrease(Move move, int depth)
        {
            _dec[Index(move)] -= depth * depth;
        }

        public bool Contains(Move move, out long value)
        {
            int index = Index(move);
            value = _inc[index] + _dec[index];
            return _inc[index] > 0;
        }

        public void Print()
        {
            for(int i = 0; i < 64; i++)
                for(int j = 0; j < 64; j++)
                {
                    int index = i + (j << 6);
                    if(_inc[index] > 0 || _dec[index] < 0)
                    {
                        long value = _inc[index] + _dec[index];
                        Move move = new Move(i, j);
                        Console.WriteLine($"{move} Value: {value} (+{_inc[index]} {_dec[index]})");
                    }
                }
        }

        public void Stats()
        {
            int good = 0;
            int bad = 0;
            for (int i = 0; i < 64; i++)
                for (int j = 0; j < 64; j++)
                {
                    int index = i + (j << 6);
                    if (_inc[index] > 0 || _dec[index] < 0)
                    {
                        Move move = new Move(i, j);
                        if (Contains(move, out long value))
                        {
                            if (value > 0)
                                good++;
                            else
                                bad++;
                        }
                    }
                }
            Console.WriteLine($"History contains {good} good and {bad} bad moves!");
        }
    }

    public class DebugSearch : ISearch
    {
        public long PositionsEvaluated { get; private set; }
        public long MovesGenerated { get; private set; }
        public long MovesPlayed { get; private set; }

        public long PVCuts { get; private set; }
        public long QuietGoodHistoryCuts { get; private set; }
        public long QuietBadHistoryCuts { get; private set; }
        public long KillerCuts { get; private set; }
        public long CaptureCuts { get; private set; }
        public long CaptureKillerCuts { get; private set; }
        public long QuietCuts { get; private set; }
        public double QuietCutRatioAccu { get; private set; }
        public long NoCuts { get; private set; }

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => _pv.GetLine(Depth);
        public bool Aborted => _killSwitch.Triggered;
        public bool GameOver => PrincipalVariation?.Length < Depth;

        Board _root = null;
        List<Move> _rootMoves = null;
        PrincipalVariation _pv;
        KillSwitch _killSwitch;
        Killers _killers;
        History _history;

        public DebugSearch(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
            _pv = new PrincipalVariation();
            _killers = new Killers();
            _history = new History();
        }

        public DebugSearch(Board board, List<Move> rootMoves)
        {
            _root = new Board(board);
            _rootMoves = rootMoves;
            _pv = new PrincipalVariation();
            _killers = new Killers();
            _history = new History();
        }

        public void Search(int maxDepth)
        {
            while (!GameOver && Depth < maxDepth)
                SearchDeeper();

        }

        public void SearchDeeper(Func<bool> killSwitch = null)
        {
            if (GameOver)
                return;

            _pv.Grow(++Depth);
            _killers.Grow(Depth);
            //Print PV
            //for (int i = Depth; i >= 0; i--)
            //{
            //    var line = _pv.GetLine(i);
            //    Console.WriteLine(string.Join(' ', line));
            //}
            _killSwitch = new KillSwitch(killSwitch);
            var window = SearchWindow.Infinite;
            Score = EvalPosition(_root, Depth, window);
            //_history.Print();
            long allQuietCuts = QuietCuts + QuietGoodHistoryCuts + QuietBadHistoryCuts;
            int quietCutPercentile = (int)(100 * QuietCutRatioAccu / Math.Max(allQuietCuts,1));
            Console.WriteLine($"Cuts by PV: {PVCuts}, Captures {CaptureCuts+CaptureKillerCuts} (~Killer {CaptureKillerCuts}), " +
                $"Killer {KillerCuts}, Quiet {allQuietCuts} (~Good {QuietGoodHistoryCuts}, ~Bad {QuietBadHistoryCuts}, {quietCutPercentile}%), Played {NoCuts}");
            //_history.Stats();
        }

        private IEnumerable<(Move, Board)> Expand(Board position, int depth)
        {
            MoveSequence2 moves = new MoveSequence2(_pv, _killers, _history, depth);
            if (depth == Depth)
                moves.FromList(position, _rootMoves);
            else
                moves.AllMoves(position);

            MovesGenerated += moves.Count;
            return moves.PlayMoves();
        }

        private IEnumerable<Board> Expand(Board position, bool escapeCheck)
        {
            MoveSequence nodes = escapeCheck ? MoveSequence.AllMoves(position) : MoveSequence.CapturesOnly(position);
            return nodes.SortCaptures().Play();
        }

        private int EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (_killSwitch.Triggered)
                return 0;

            if (depth == 0)
            {
                //Console.WriteLine(depth + new string(' ', Depth - depth) + "QEval!");
                return QEval(position, window);
            }

            PositionsEvaluated++;
            Color color = position.ActiveColor;

            int expandedNodes = 0;
            foreach ((Move move, Board child) in Expand(position, depth))
            {
                expandedNodes++;

                //For all rootmoves after the first search with "null window"
                if (expandedNodes > 1 && depth == Depth)
                {
                    SearchWindow nullWindow = window.GetNullWindow(color);
                    int nullScore = EvalPosition(child, depth - 1, nullWindow);
                    if (nullWindow.Outside(nullScore, color))
                        continue;
                }

                float moveorder = MoveSequence2._MoveOrderRatio;
                int score = EvalPosition(child, depth - 1, window);

                bool isQuiet = position[move.ToIndex] == Piece.None;
                if (window.Inside(score, color))
                {
                    bool wasPV = _pv[depth] == move;

                    //bool anyPV = _pv.Contains(move, depth);
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                    {
                        Piece victim = position[move.ToIndex];
                        if (wasPV)
                        {
                            PVCuts++;
                            //Console.WriteLine(depth + new string(' ', Depth - depth) + "X by PV " + move);
                        }
                        else if(victim != Piece.None)
                        {
                            if (_killers.Contains(move, depth))
                                CaptureKillerCuts++;
                            else
                                CaptureCuts++;
                            //Console.WriteLine(depth + new string(' ', Depth - depth) + "X by Capture " + move);
                        }
                        else if (_killers.Contains(move, depth))
                        {
                            KillerCuts++;
                            //Console.WriteLine(depth + new string(' ', Depth - depth) + "X by Killer " + move);
                        }
                        else
                        {
                            //Console.WriteLine("Quiet Cut: " + moveorder);
                            QuietCutRatioAccu += moveorder;
                            if (_history.Contains(move, out long value))
                            {
                                if(value > 0)
                                    QuietGoodHistoryCuts++;
                                else
                                    QuietBadHistoryCuts++;
                            }
                            else
                                QuietCuts++;
                        }
                        if (isQuiet)
                            _history.Increase(move, depth);

                        _killers.Consider(position, move, depth);
                        return window.GetScore(color);
                    }
                }
                //if (isQuiet)
                //    _history.Decrease(move, depth);
            }
            MovesPlayed += expandedNodes;
            if(depth > 1)
                NoCuts++;
            //Console.WriteLine(depth + new string(' ', Depth - depth) + "Played");

            if (expandedNodes == 0) //no expansion happened from this node!
            {
                //having no legal moves can mean two things: (1) lost or (2) draw?
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * PeSTO.LostValue : 0;
            }

            return window.GetScore(color);
        }

        private int QEval(Board position, SearchWindow window)
        {
            PositionsEvaluated++;
            Color color = position.ActiveColor;

            //if inCheck we can't use standPat, need to escape check!
            bool inCheck = position.IsChecked(color);
            if (!inCheck)
            {
                int standPatScore = PeSTO.Evaluate(position);
                //Cut will raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(standPatScore, color))
                    return window.GetScore(color);
            }

            int expandedNodes = 0;
            //play remaining captures (or any moves if king is in check)
            foreach (Board child in Expand(position, inCheck))
            {
                expandedNodes++;
                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval(child, window);

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            //checkmate?
            if (expandedNodes == 0 && inCheck)
                return (int)color * PeSTO.LostValue;

            //stalemate?
            if (expandedNodes == 0 && !AnyLegalMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }

        public bool AnyLegalMoves(Board position)
        {
            var moves = new AnyLegalMoves(position);
            return moves.CanMove;
        }
    }
}
