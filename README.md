# MinimalChessEngine

This repository tracks the journey of writing my first chess engine. It's written in C# and contains 3 projects:
1. a command-line based GUI (*MinimalChessBoard*) 
1. an [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) chess engine (*MinimalChessEngine*)
1. a library implementing chess logic and algorithms used by the other two applications (*MinimalChess*)

## Motivation

My focus is on creating a *minimal* chess engine with just enough features and optimizations to become a reasonably strong player. 

__Version 0.2__ of MinimalChess uses iterative deepenging search with alpha-beta pruning and a simple killer-move heuristic and evaluates a position by counting material. That's all. This lack of sophistication causes it to play rather weak at only a little over 1000 ELO. Nothing to brag about but it makes it a good sparring partner for weak human players like myself and chess programmers who are just starting out. (Again - like myself) The engine is open source and I tried to write code that is as simple as possible to both understand and explain. It could be smaller or faster but I doubt it could be much simpler than it currently is. ;)

## Making Of Videos (Chess programming tutorial)

I have documented each milestone of the development in an accomanying [Youtube](https://www.youtube.com/playlist?list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du) video.

1. [Making of MinimalChessEngine - Episode 1: Hello World](https://www.youtube.com/watch?v=hnedjeTApfY&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 2: Let's Play](https://www.youtube.com/watch?v=pKB51c9WUrk&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 3: Move Generation](https://www.youtube.com/watch?v=j6bNdkQnL0Q&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 4: Search & Eval](https://www.youtube.com/watch?v=b3DMIhmPSvE&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)

## Play the Engine

You can find prebuild binaries of different versions of MinimalChess for Mac, Linux and Windows here on github.

### MinimalChessEngine

To play I recommend you add MinimalChessEngine as an engine to an UCI compatible Chess GUI such as [CuteChess](https://cutechess.com/). 

### MinimalChessBoard

You can also run MinimalChessBoard to get a console based Chess GUI that allows you to play chess against the engine. I don't recommend it though. This part of the project is mainly used during development for analysis and debugging purposes!

Command           | Description
----------------- | -------------
[move]			      | You can play the game by typing in the move you want to make in the long algebraic notation e.g. "e2e4" to move white's King's Pawn.
reset 			      | Reset the board to the start position.
fen [fenstring]		| Setup the board to represent the given position.
perft [depth]	  	| Compute perft values of the given depth
divide [depth]  	| Compute perft values of all available moves
! [depth]		      | Play the best move, search it with the given depth
? [depth]		      | List all available moves
??			          | Print the resulting board for each available move

## Help & Support

Please let me know of any bugs or stability issues and must-have features you feel even the most barebones engine should support but MinimalChess is lacking.
Don't hesitate to contact via email or open an issue or engage in the discussions section of this repository. 
