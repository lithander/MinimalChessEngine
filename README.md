# MinimalChess

MinimalChess is a UCI chess engine written in C#.

It's focus on a *minimal* implementation of only the most important features and optimizations makes MinimalChess a good starting point for programmers with a working knowledge of C# but no prior experience in chess programming. MinimalChess is written in only a few hundred lines of idiomatic C# code where other engines (of comparable strength) often have thousands.

The didactic nature of the project is reinforced by it's open source license (MIT) and a dedicated series of explanatory [Youtube](https://www.youtube.com/playlist?list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du) videos.

## Features

* A simple 8x8 Board representation: Just an array to represent the 64 squares and keep track of the pieces.
* A triangular PV-Table to keep track of the Principal Variation of best moves.
* Staged move generation: PV move first, then MVV-LVA sorted captures, followed by known killer moves and finally the remaining quiet moves.
* Iterative Deepening Search with Alpha-Beta pruning and Quiescence Search.
* Tapered Piece-Square Tables with values tuned on a set of 725000 quiet, labeled positions.
* No bitboards, hash tables, not even an undo-move method, just the essentials.

## How to play

MinimalChess, just like most other chess programs, does not provide its own user interface. Instead it implements the [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) protocol to make it compatible with most popular Chess GUIs such as:
* [Arena Chess GUI](http://www.playwitharena.de/) (free)
* [BanksiaGUI](https://banksiagui.com/) (free)
* [Cutechess](https://cutechess.com/) (free)
* [Nibbler](https://github.com/fohristiwhirl/nibbler/releases) (free)
* [Chessbase](https://chessbase.com/) (paid).

Once you have a chess GUI installed you can download the prebuild [binaries for Mac, Linux and Windows](https://github.com/lithander/MinimalChessEngine/releases/tag/v0.4.1) and extract the contents of the zip file into a location of your choice.

As a final step you have to register the engine with the GUI. The details depend on the GUI you chose but there should be something like "Add Engine..." somewhere in the settings.

After this you should be ready to select MinimalChess as a player!

## Version History
```
Version:   0.4.1
Size:      610 LOC
Strength:  1900 ELO
```
[__Version 0.4.1__](https://github.com/lithander/MinimalChessEngine/releases/tag/v0.4.1) now uses tapered Piece-Square tables to evaluate positions. It took two weeks of tuning and testing until I found values that could rival [PeSTOs](https://rofchade.nl/?p=307) famous PSTs in strength.
I also added a [killer heuristic](https://www.chessprogramming.org/Killer_Heuristic) and staged move generation so that MinimalChess does not generate moves which will likely never be played. The resulting speed improvements more than compensate for the slightly more expensive evaluation. 
A new time control logic now allocates the given time budget smarter, especially in modes where there's an increment each move, and the 'nodes' and 'depth' constraints are now supported in all modes.

```
Version:   0.3
Size:      641 LOC
Strength:  1575 ELO
```
[__Version 0.3__](https://github.com/lithander/MinimalChessEngine/releases/tag/v0.3) adds MVV-LVA move ordering, Quiescence Search and replaces material-only evaluation with Piece-Square Tables.
With these changes MinimalChess gains about 500 ELO in playing strength over the previous version and achieved at [1571 ELO](http://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?match_length=30&each_game=1&print=Details&each_game=1&eng=MinimalChess%200.3%2064-bit#MinimalChess_0_3_64-bit) on the CCRL.
This version also introduces a rather unique feature: Sets of PSTs are defined in separate files and can be selected via an UCI option. This allows the user to tweak the values or write their own tables from scratch and by this alter the playstyle of the engine considerably. No programming experience required!

```
Version:   0.2
Size:      502 LOC
Strength:  1059 ELO 
```
[__Version 0.2__](https://github.com/lithander/MinimalChessEngine/releases/tag/v0.2) uses Iterative Deepening search with Alpha-Beta pruning. It collects the Principal Variation (PV) and when available plays PV moves first. Other than that there's no move ordering. Positions are evaluated by counting material only. This lack of sophistication causes it to play rather weak at [1059 ELO](http://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?print=Details&each_game=1&eng=MinimalChess%200.2%2064-bit#MinimalChess_0_2_64-bit) on the CCRL. I tried to the write code to be as simple as possible to both understand and explain. It could be smaller or faster but I doubt it could be much simpler than this version.

## Compiling the engine

This repository contains 3 projects:
1. **MinimalChessBoard** is a command-line based GUI  
1. **MinimalChessEngine** is a [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) compatible chess engine
1. ***MinimalChess*** is a library with shared chess logic and algorithms used by the other two applications

### Windows

To compile MinimalChess on Windows I suggest you install Visual Studio and open **MinimalChessEngine.sln** in it.
You will need to have the [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1) installed. 
Hit the play button and it should compile and start!

### Linux

Read the official instructions on how to [Install .NET on Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux).
There are also [Ubuntu Linux specific installations instructions](https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu).

You can clone the repository and compile it like this:

```
$ wget https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb

# Here I go for 3.1 core version explicitly, not 5.0 to verify compatibility with 3.1
$ sudo apt-get update; \
    sudo apt-get install -y apt-transport-https && \
    sudo apt-get update && \
    sudo apt-get install -y dotnet-sdk-3.1

$ git clone https://github.com/lithander/MinimalChessEngine.git
$ cd MinimalChessEngine/

$ dotnet build -c Release
```

## Chess Programming Tutorial

I have documented important milestones of the development in a series of [Youtube](https://www.youtube.com/playlist?list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du) videos.

1. [Making of MinimalChessEngine - Episode 1: Hello World](https://www.youtube.com/watch?v=hnedjeTApfY&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 2: Let's Play](https://www.youtube.com/watch?v=pKB51c9WUrk&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 3: Move Generation](https://www.youtube.com/watch?v=j6bNdkQnL0Q&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 4: Search & Eval](https://www.youtube.com/watch?v=b3DMIhmPSvE&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)

...if you enjoy the format let me know and I might make some more episodes. :)

## MinimalChessBoard

If you compile the *MinimalChessBoard* project you can get a console based Chess GUI that allows you to play chess against the engine. The UX is lacking, though. This part of the project is mainly used during development for analysis and debugging purposes!

Command           | Description
----------------- | -------------
[move]			      | Play a move by typing in it's name in long algebraic notation e.g. "e2e4" to move white's King's Pawn.
[fenstring]  		| Setup the board to represent the given position.
! [depth]		      | The computer plays the next move, searched with the given depth.
? [depth]		      | List all available moves
??			          | Print the resulting board for each available move
reset 			      | Reset the board to the start position.
perft [depth]	  	| Compute perft values of the given depth
divide [depth]  	| Compute perft values of all available moves

## Help & Support

Please let me know of any bugs or stability issues and must-have features you feel MinimalChess is still lacking.
Don't hesitate to contact me via email, open an issue or engage in the discussions section of this repository. 
