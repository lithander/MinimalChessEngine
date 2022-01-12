using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics.X86;

namespace BitboardExplorer
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Stack<ulong> stack = new Stack<ulong>();
            ulong bitboard = 0;
            while(true)
            {
                Console.WriteLine("A B C D E F G H");
                PrintBitboard(bitboard);
                PrintHex(bitboard);
                string input = Console.ReadLine();
                string[] tokens = input.Split();
                string command = tokens[0];
                if (command.StartsWith("0x") && command.EndsWith("UL")) //0xFF80808080808080UL
                {
                    string hex = command[2..^2];
                    bitboard = ulong.Parse(hex, NumberStyles.HexNumber, null);
                }
                else if(command == "push")
                {
                    stack.Push(bitboard);
                }
                else if(command == "pop" && stack.Count > 0)
                {
                    bitboard = stack.Pop();
                }
                else if (command == "x^(x-1)")
                {
                    bitboard = (bitboard ^ (bitboard - 1));
                }
                else if (command == "//")
                {
                    PrintBitboardAsComment(bitboard);
                }
                else if (command == ">>" && tokens.Length == 2)
                {
                    bitboard = bitboard >> int.Parse(tokens[1]);
                }
                else if (command == "<<" && tokens.Length == 2)
                {
                    bitboard = bitboard << int.Parse(tokens[1]);
                }
                else if(command == "bis" && tokens.Length== 2)
                {
                    if (!int.TryParse(tokens[1], out int square))
                        square = Leorik.Notation.ToSquare(tokens[1]);
                    FindBishopTargetsAnnotated(bitboard, square);
                    PrintBitboard(Bitboard.GetBishopTargets(bitboard, square), "Bitboard.GenBishop");
                }
                else if(command == "rook" && tokens.Length == 2)
                {
                    if (!int.TryParse(tokens[1], out int square))
                        square = Leorik.Notation.ToSquare(tokens[1]);
                    FindRookTargetsAnnotated(bitboard, square);
                    PrintBitboard(Bitboard.GetRookTargets(bitboard, square), "Bitboard.GenRook");
                }
                else if (command == "king_targets")
                {
                    ListTargets(Attacks.King);
                }
                else if (command == "knight_targets")
                {
                    ListTargets(Attacks.Knight);
                }
                else if (command == "bishop_targets")
                {
                    ListTargets(Attacks.Bishop);
                }
                else if (command == "rook_targets")
                {
                    ListTargets(Attacks.Rook);
                }
                else if (command == "lines")
                {
                    ListLines();
                }
                else if (command.Count(c => c == '/') == 7) //Fen-string detection
                {
                    Board board = new Board(input);
                    Leorik.BoardState bitboards = board.BoardState;
                    bitboard = bitboards.White | bitboards.Black;
                    PrintBoardState(bitboards);
                }
                else
                {
                    try
                    {
                        foreach (var token in tokens)
                        {
                            int square = Notation.ToSquare(token);
                            Console.WriteLine($"^ bb[{square}]");
                            ulong bit = 1UL << square;
                            bitboard ^= bit;
                        }
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine("ERROR: " + error.Message);
                    }
                }
            }
        }

        private static void ListLines()
        {
            List<string> diag = new List<string>();
            List<string> anti = new List<string>();
            List<string> vert = new List<string>();
            List<string> horz = new List<string>();
            for (int square = 0; square < 64; square++)
            {
                diag.Add(ToHex(Bitboard.GetDiagonal(square)));
                anti.Add(ToHex(Bitboard.GetAntidiagonal(square)));
                vert.Add(ToHex(Bitboard.GetVertical(square)));
                horz.Add(ToHex(Bitboard.GetHorizontal(square)));
            }
            Console.WriteLine("Diagonal");
            BlockPrint(diag, 4);
            Console.WriteLine("Antidiagonal");
            BlockPrint(anti, 4);
            Console.WriteLine("Horizontal");
            BlockPrint(horz, 4);
            Console.WriteLine("Vertical");
            BlockPrint(vert, 4);
        }

        private static void ListTargets(byte[][] pattern)
        {
            List<string> bbStrings = new List<string>();
            for (int square = 0; square < 64; square++)
            {
                ulong bb = 0;
                foreach (var target in pattern[square])
                    bb |= 1UL << target;
                bbStrings.Add(ToHex(bb));
            }

            BlockPrint(bbStrings, 4);
        }

        private static void ListTargets(byte[][][] pattern)
        {
            List<string> bbStrings = new List<string>();
            for (int square = 0; square < 64; square++)
            {
                ulong bb = 0;
                foreach (var axis in pattern[square])
                    foreach (var target in axis)
                        bb |= 1UL << target;
                bbStrings.Add(ToHex(bb));
            }

            BlockPrint(bbStrings, 4);
        }

        private static void BlockPrint(List<string> bbStrings, int stride)
        {
            int next = 0;
            while (next < bbStrings.Count)
            {
                for (int i = 0; i < stride; i++)
                {
                    Console.Write(bbStrings[next++]);
                    if (next == bbStrings.Count)
                        break;
                    Console.Write(", ");
                }
                Console.WriteLine();
            }

        }

        private static void FindBishopTargetsAnnotated(ulong bitboard, int square)
        {
            ulong bbPiece = 1UL << square;
            PrintBitboard(bbPiece, "Piece:");

            //ulong bbBlocker = bitboard & ~bbPiece; /* remove the source square from the occupation */
            //Console.WriteLine("Blocker:");
            //PrintBitboard(bbBlocker);
            //
            ////mask the bits below bbPiece
            //ulong bbBelow = bbPiece - 1;
            //Console.WriteLine("Low bits:");
            //PrintBitboard(bbBelow);

            //************************
            //** DIAGONALS TARGETS **
            //************************

            //https://www.chessprogramming.org/On_an_empty_Board

            int diag = 8 * (square & 7) - (square & 56);
            int north = -diag & (diag >> 31);
            int south = diag & (-diag >> 31);
            ulong bbDiag = (0x8040201008040201UL >> south) << north;
            PrintBitboard(bbDiag, $"Diagonal diag {diag} north {north} south {south}");

            //************************
            //** ANTI-DIAGONAL TARGETS **
            //************************

            diag = 56 - 8 * (square & 7) - (square & 56);
            north = -diag & (diag >> 31);
            south = diag & (-diag >> 31);
            ulong bbAntiDiag = (0x0102040810204080UL >> south) << north;
            PrintBitboard(bbAntiDiag, $"Anti-Diagonal diag {diag} north {north} south {south}");

            //************************

            int rank = square / 8;
            int file = square & 7;

            const ulong DIAGONAL = 0x8040201008040201UL;
            int verticalShift = file - rank;
            int bitShift = verticalShift << 3;//to shift one rank means to shift by 8 bits
            ulong bbDiag2 = bitShift > 0 ? DIAGONAL >> bitShift : DIAGONAL << -bitShift;
            Debug.Assert(bbDiag == bbDiag2);


            const ulong ANTIDIAGONAL = 0x0102040810204080;
            verticalShift = 7 - file - rank;
            bitShift = verticalShift << 3;//to shift one rank means to shift by 8 bits
            ulong bbAntiDiag2 = bitShift > 0 ? ANTIDIAGONAL >> bitShift : ANTIDIAGONAL << -bitShift;
            Debug.Assert(bbAntiDiag == bbAntiDiag2);

            //...the rest is like for the rooks
        }

        private static void FindRookTargetsAnnotated(ulong bitboard, int square)
        {
            ulong bbPiece = 1UL << square;
            PrintBitboard(bbPiece, "Piece:");

            ulong bbBlocker = bitboard & ~bbPiece; /* remove the source square from the occupation */
            PrintBitboard(bbBlocker, "Blocker:");

            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;
            PrintBitboard(bbBelow, "Low bits:");

            //************************
            //** HORIZONTAL TARGETS **
            //************************

            //horizontal full line
            int file = square % 8;
            ulong bbHorizontal = 0x00000000000000FFUL << (square - file);
            PrintBitboard(bbHorizontal, "Horizontal:");

            ulong bbBlockersAbove = (bbBlocker & bbHorizontal) & ~bbBelow;
            PrintBitboard(bbBlockersAbove, "bbBlockersAbove");
            //x^(x-1) sets all bits up to and including the first set bit, the rest are zeroed out.
            ulong bbMaskAbove = bbBlockersAbove ^ (bbBlockersAbove - 1);
            //mask above has now all bits up to the first blocker above origin set
            PrintBitboard(bbMaskAbove, "bbMaskAbove");

            //count the bits until the first blocker below origin
            ulong bbBlockersBelow = (bbBlocker & bbHorizontal) & bbBelow;
            int lzcnt = (int)Lzcnt.X64.LeadingZeroCount(bbBlockersBelow | 1);
            //and shift a mask from the top to clear all bits up to that blocker
            ulong bbMaskBelow = 0x7FFFFFFFFFFFFFFFUL >> lzcnt;
            PrintBitboard(bbMaskBelow, "bbMaskBelow");

            //the difference between the two masks is our horizontal attack line (including the first occupied square)
            ulong bbHorizontalAttacks = (bbMaskAbove ^ bbMaskBelow) & bbHorizontal;
            PrintBitboard(bbHorizontalAttacks, "hLine == (bbMaskAbove ^ bbMaskBelow) & bbHorizontal");

            //**********************
            //** VERTICAL TARGETS **
            //**********************

            //vertical full line
            ulong bbVertical = 0x0101010101010101UL << file;
            PrintBitboard(bbVertical, "Vertical:");

            bbBlockersAbove = (bbBlocker & bbVertical) & ~bbBelow;
            PrintBitboard(bbBlockersAbove, "bbBlockersAbove");
            //x^(x-1) sets all bits up to and including the first set bit, the rest are zeroed out.
            bbMaskAbove = bbBlockersAbove ^ (bbBlockersAbove - 1);
            //mask above has now all bits up to the first blocker above origin set
            PrintBitboard(bbMaskAbove, "bbMaskAbove");

            //count the bits until the first blocker below origin
            bbBlockersBelow = (bbBlocker & bbVertical) & bbBelow;
            lzcnt = (int)Lzcnt.X64.LeadingZeroCount(bbBlockersBelow | 1);
            //and shift a mask from the top to clear all bits up to that blocker
            bbMaskBelow = 0x7FFFFFFFFFFFFFFFUL >> lzcnt;
            PrintBitboard(bbMaskBelow, "bbMaskBelow");

            //the difference between the two masks is our horizontal attack line (including the first occupied square)
            ulong bbVerticalAttacks = (bbMaskAbove ^ bbMaskBelow) & bbVertical;
            PrintBitboard(bbMaskAbove ^ bbMaskBelow, "vLine == (bbMaskAbove ^ bbMaskBelow) & bbVertical");

            PrintBitboard(bbVerticalAttacks | bbHorizontalAttacks, "ROOK ATTACKS:");
            Console.WriteLine("*****");
        }

        private static string ToHex(ulong bitboard)
        {
            return $"0x{Convert.ToString((long)bitboard, 16).PadLeft(16, '0').ToUpperInvariant()}UL";
        }

        private static void PrintHex(ulong bitboard)
        {
            Console.WriteLine(ToHex(bitboard));
        }

        private static void PrintBitboard(ulong bitboard, string caption)
        {
            Console.WriteLine(caption);
            PrintBitboard(bitboard);
        }

        private static void PrintBoardState(Leorik.BoardState bitboards)
        {
            PrintBitboard(bitboards.Black, $"Black: {ToHex(bitboards.Black)}");
            PrintBitboard(bitboards.White, $"White: {ToHex(bitboards.White)}");

            PrintBitboard(bitboards.Pawns, $"Pawns: {ToHex(bitboards.Pawns)}");
            PrintBitboard(bitboards.Knights, $"Knights: {ToHex(bitboards.Knights)}");
            PrintBitboard(bitboards.Bishops, $"Bishops: {ToHex(bitboards.Bishops)}");
            PrintBitboard(bitboards.Rooks, $"Rooks: {ToHex(bitboards.Rooks)}");
            PrintBitboard(bitboards.Queens, $"Queens: {ToHex(bitboards.Queens)}");
            PrintBitboard(bitboards.Kings, $"Kings: {ToHex(bitboards.Kings)}");
            Console.WriteLine();
        }

        private static void PrintBitboard(ulong bitboard)
        {
            byte[] bbBytes = BitConverter.GetBytes(bitboard);
            Array.Reverse(bbBytes);
            foreach (byte bbByte in bbBytes)
            {
                string line = Convert.ToString(bbByte, 2).PadLeft(8, '0');
                line = line.Replace('1', 'X');
                line = line.Replace('0', '.');
                var chars = line.ToCharArray();
                Array.Reverse(chars);
                Console.WriteLine(string.Join(' ', chars));
            }
        }

        private static void PrintBitboardAsComment(ulong bitboard)
        {
            byte[] bbBytes = BitConverter.GetBytes(bitboard);
            Array.Reverse(bbBytes);
            for (int i = 0; i < 8; i++)
            {
                string line = Convert.ToString(bbBytes[i], 2).PadLeft(8, '0');
                line = line.Replace('1', 'X');
                line = line.Replace('0', '.');
                var chars = line.ToCharArray();
                Array.Reverse(chars);
                var row = string.Join(' ', chars);
                switch (i)
                {
                    case 0: Console.WriteLine($"/* {row}"); break;
                    case 7: Console.WriteLine($"   {row} */"); break;
                    default: Console.WriteLine($"   {row}"); break;
                }
            }
        }
    }
}
