What is in this archive
====

violent.epd       - 1500k EPDs of quiet and violent positions.
quiet.epd         - 730k EPDs of quiet positions, subset of violent.epd.
quiet-labeled.epd - 725k EPDs of quiet positions, subset of quiet.epd, annotated with a game result.


How to use the data
====

quiet-labeled.epd can be used with the Texel Tuning Method https://chessprogramming.wikispaces.com/Texel%27s+Tuning+Method.
Same training data is used to train zurichess http://www.zurichess.xyz.


Who was the data generated
====

1. 75000 games were played between three slightly different versions of zurichess 
using 2moves_v1.pgn opening book to ensure high play variability.
2. From each game 20 positions were sampled from the millions of positions evaluated by
the engine during the game play. This resulted in 1500k random positions which were stored in violent.epd.
3. From the set were removed all positions on which quiescence search found a wining capture.
The remaining positions were stored in quiet.epd.
4. Next, from each quiet position a game using Stockfish 080916 was played.
The result were stored in quiet-labeled.epd.
