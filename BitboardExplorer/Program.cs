using MinimalChess;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                    PrintBitboardComment(bitboard);
                }
                else if (command == ">>" && tokens.Length == 2)
                {
                    bitboard = bitboard >> int.Parse(tokens[1]);
                }
                else if (command == "<<" && tokens.Length == 2)
                {
                    bitboard = bitboard << int.Parse(tokens[1]);
                }
                else if(command == "rook" && tokens.Length == 2)
                {
                    int square;
                    if(!int.TryParse(tokens[1], out square))
                        square = Notation.ToSquare(tokens[1]);
                    ulong bbPiece = 1UL << square;
                    Console.WriteLine("Piece:");
                    PrintBitboard(bbPiece);
 
                    ulong bbBlocker = bitboard & ~bbPiece; /* remove the source square from the occupation */
                    Console.WriteLine("Blocker:");
                    PrintBitboard(bbBlocker);

                    //mask the bits below bbPiece
                    ulong bbBelow = bbPiece - 1;
                    Console.WriteLine("Low bits:");
                    PrintBitboard(bbBelow);

                    //************************
                    //** HORIZONTAL TARGETS **
                    //************************

                    //horizontal full line
                    int file = square % 8;
                    ulong bbHorizontal = 0x00000000000000FFUL << (square - file);
                    Console.WriteLine("Horizontal:");
                    PrintBitboard(bbHorizontal);

                    ulong bbBlockersAbove = (bbBlocker & bbHorizontal) & ~bbBelow;
                    Console.WriteLine("bbBlockersAbove");
                    PrintBitboard(bbBlockersAbove);
                    //x^(x-1) sets all bits up to and including the first set bit, the rest are zeroed out.
                    ulong bbMaskAbove = bbBlockersAbove ^ (bbBlockersAbove - 1);
                    //mask above has now all bits up to the first blocker above origin set
                    Console.WriteLine("bbMaskAbove");
                    PrintBitboard(bbMaskAbove);

                    //count the bits until the first blocker below origin
                    ulong bbBlockersBelow = (bbBlocker & bbHorizontal) & bbBelow;
                    int lzcnt = (int)Lzcnt.X64.LeadingZeroCount(bbBlockersBelow | 1);
                    //and shift a mask from the top to clear all bits up to that blocker
                    ulong bbMaskBelow = 0x7FFFFFFFFFFFFFFFUL >> lzcnt;
                    Console.WriteLine("bbMaskBelow");
                    PrintBitboard(bbMaskBelow);

                    //the difference between the two masks is our horizontal attack line (including the first occupied square)
                    Console.WriteLine("hLine == (bbMaskAbove ^ bbMaskBelow) & bbHorizontal");
                    ulong bbHorizontalAttacks = (bbMaskAbove ^ bbMaskBelow) & bbHorizontal;
                    PrintBitboard(bbHorizontalAttacks);

                    //**********************
                    //** VERTICAL TARGETS **
                    //**********************

                    //vertical full line
                    ulong bbVertical = 0x0101010101010101UL << file;
                    Console.WriteLine("Vertical:");
                    PrintBitboard(bbVertical);

                    bbBlockersAbove = (bbBlocker & bbVertical) & ~bbBelow;
                    Console.WriteLine("bbBlockersAbove");
                    PrintBitboard(bbBlockersAbove);
                    //x^(x-1) sets all bits up to and including the first set bit, the rest are zeroed out.
                    bbMaskAbove = bbBlockersAbove ^ (bbBlockersAbove - 1);
                    //mask above has now all bits up to the first blocker above origin set
                    Console.WriteLine("bbMaskAbove");
                    PrintBitboard(bbMaskAbove);

                    //count the bits until the first blocker below origin
                    bbBlockersBelow = (bbBlocker & bbVertical) & bbBelow;
                    lzcnt = (int)Lzcnt.X64.LeadingZeroCount(bbBlockersBelow | 1);
                    //and shift a mask from the top to clear all bits up to that blocker
                    bbMaskBelow = 0x7FFFFFFFFFFFFFFFUL >> lzcnt;
                    Console.WriteLine("bbMaskBelow");
                    PrintBitboard(bbMaskBelow);

                    //the difference between the two masks is our horizontal attack line (including the first occupied square)
                    Console.WriteLine("vLine == (bbMaskAbove ^ bbMaskBelow) & bbVertical");
                    ulong bbVerticalAttacks = (bbMaskAbove ^ bbMaskBelow) & bbVertical;
                    PrintBitboard(bbMaskAbove ^ bbMaskBelow);

                    Console.WriteLine("ROOK ATTACKS:");
                    PrintBitboard(bbVerticalAttacks | bbHorizontalAttacks);
                    Console.WriteLine("*****");
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

        private static void PrintHex(ulong bitboard)
        {
            string result = Convert.ToString((long)bitboard, 16).PadLeft(16, '0').ToUpperInvariant();
            Console.WriteLine($"0x{result}UL");
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

        private static void PrintBitboardComment(ulong bitboard)
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
