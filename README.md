# MinimalChess

This repository tracks the journey of writing my first chess engine. It's written in C# and contains 3 projects:
1. a command-line based GUI (*MinimalChessBoard*) 
1. an [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) chess engine (*MinimalChessEngine*)
1. a library implementing chess logic and algorithms used by the other two applications (*MinimalChess*)

## Motivation

My focus was on creating a *minimal* engine with just enough features and optimizations to become a reasonably strong player. 
I try to keep the codebase small but more importantly simple and human readable.

I learned a lot about how a chess engine operates and how critical components like move generation or tree search can be implemented correctly. 

## Making Of Videos (Chess programming tutorial)

I have documented each milestone of the development in an accomanying [Youtube](https://www.youtube.com/playlist?list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du) video.

1. [Making of MinimalChessEngine - Episode 1: Hello World](https://www.youtube.com/watch?v=hnedjeTApfY&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 2: Let's Play](https://www.youtube.com/watch?v=pKB51c9WUrk&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 3: Move Generation](https://www.youtube.com/watch?v=j6bNdkQnL0Q&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 4: Search & Eval](https://www.youtube.com/watch?v=b3DMIhmPSvE&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)

## Download the Engine

I've uploaded a Windows build here: https://github.com/lithander/MinimalChessEngine/releases/tag/MakingOfPart4

### MinimalChessEngine

Add MinimalChessEngine.exe as an engine to an UCI compatible Chess GUI such as CuteChess. I expect it to play at roughly 1000 ELO in a very non-human style.
It doesn't provide any options to configure and is hardcoded to make a move in less then one second so it should be compatible with all but the fastest time controls. There are no hash table settings or opening books to disable "advanced" techniques like this aren't implemented.

### MinimalChessBoard

Run MinimalChessBoard.exe directly to get a console based Chess GUI with a few commands that help debugging the code!

Command           | Description
----------------- | -------------
[move]			      | You can play the game by typing in the move you want to make in the long algebraic notation e.g. "e2e4" to move white's King's Pawn.
reset 			      | Reset the board to the start position.
fen [fenstring]		| Setup the board to represent the given position.
perft [integer]		| Compute perft values of the given depth
divide [integer]	| Compute perft values of all available moves
! [integer]		    | Play the best move, search it with the given depth
? [integer]		    | List all available moves
??			          | Print the resulting board for each available move

## Help & Support

If you encounter any problems of have questions or comments don't hesitate to contact me or open an issue or engage in the discussions section of this repositor. 
