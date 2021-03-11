# MinimalChess

MinimalChess is a chess engine written in C#. It was developed from scratch in an attempt to learn more about chess programming. The result is a *minimal* chess engine with just the essential features and optimizations, oftentimes choosing simplicity and readability over peak runtime performance. I am also documenting my development journey with [Youtube](https://www.youtube.com/playlist?list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du) videos.

## Features

* Implements the UCI protocol including the common time management options
* Iterative Deepening with Alpha-Beta pruning and Quiescence Search.
* Collects the Principal Variation (PV) of best moves in a Triangular PV-Table.
* Plays the PV move first, followed by MVV-LVA sorted captures.
* Positions are evaluated with Piece-Square Tables.

...that's all. 
Really! 
Okay, I can list some *non-features* if you insist.

* The board is represented as an array of 64 squares. 
* The move generator is really straight forward. 
* There is no hash table, no undo-move method, just the essentials.
* The PSTs are defined in external files making it easy to tweak them or write your own. (Chose one them via UCI option)

## How to play

MinimalChess, just like most other chess programs, does not provide its own user interface. Instead it implements the [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) protocol to make it compatible with most popular Chess GUIs such as:
* [Arena Chess GUI](http://www.playwitharena.de/) (free)
* [BanksiaGUI](https://banksiagui.com/) (free)
* [Cutechess](https://cutechess.com/) (free)
* [Nibbler](https://github.com/fohristiwhirl/nibbler/releases) (free)
* [Chessbase](https://chessbase.com/) (paid).

Once you have a chess GUI installed you can download the prebuild [binaries for Mac, Linux and Windows](https://github.com/lithander/MinimalChessEngine/releases/tag/v0.3) and extract the contents of the zip file into a location of your choice.

As a final step you have to register the engine with the GUI. The details depend on the GUI you chose but there should be something like "Add Engine..." somewhere in the settings.

After this you should be ready to select MinimalChess as a player!

## Version History

__Version 0.3__ adds MVV-LVA move ordering, Quiescence Search and replaces material-only evaluation with Piece-Square Tables.
With these changes it gains about 500 ELO in playing strength over the previous version.
This version also introduces a rather unique feature: Sets of PSTs are defined in separate files and can be selected via an UCI option. This allows the user to tweak the values or write their own tables from scratch and by this alter the playstyle of the engine considerably. No programming experience required! ;)

__Version 0.2__ uses Iterative Deepening search with Alpha-Beta pruning. It collects the Principal Variation (PV) and when available plays PV moves first. Other than that there's no move ordering. Positions are evaluated by counting material only. This lack of sophistication causes it to play rather weak at only a little over [1000 ELO](http://ccrl.chessdom.com/ccrl/404/cgi/engine_details.cgi?print=Details&each_game=1&eng=MinimalChess%200.2%2064-bit#MinimalChess_0_2_64-bit). Nothing to brag about but it makes it a good sparring partner for weak human players like myself and chess programmers who are just starting out. (Again - like myself) The engine is open source and I tried to write code that is as simple as possible to both understand and explain. It could be smaller or faster but I doubt it could be much simpler than it currently is.

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

## Making-Of Videos // Chess Programming Tutorial

I have documented important milestones of the development in an accompanying [Youtube](https://www.youtube.com/playlist?list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du) video.

1. [Making of MinimalChessEngine - Episode 1: Hello World](https://www.youtube.com/watch?v=hnedjeTApfY&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 2: Let's Play](https://www.youtube.com/watch?v=pKB51c9WUrk&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 3: Move Generation](https://www.youtube.com/watch?v=j6bNdkQnL0Q&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)
1. [Making of MinimalChessEngine - Episode 4: Search & Eval](https://www.youtube.com/watch?v=b3DMIhmPSvE&list=PL6vJSkTaZuBtTokp8-gnTsP39GCaRS3du)

...if you enjoy the format let me know and I might make some more episodes. ;)

### MinimalChessBoard

If you compile the MinimalChessBoard project you can get a console based Chess GUI that allows you to play chess against the engine. The UX is lacking, though. This part of the project is mainly used during development for analysis and debugging purposes!

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
Don't hesitate to contact me via email or open an issue or engage in the discussions section of this repository. 
