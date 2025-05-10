using ChessBot.AI;
using Godot;
using System;
using System.Collections.Generic;

public partial class ChessBotNoAI
{
    public int searchCounter = 0;
    public Bitboard currentBoard;
    public DataHandler.Move currentMove = new(-1, -1);
    public DataHandler DH = new();
    public bool isBlack = true;

    private NeuralNetwork nn;
    private NeuralNetwork mnn;

    public ChessBotNoAI(string Path)
    {
        nn = new NeuralNetwork(Path);
        mnn = new NeuralNetwork("Train/Model_128_64_32_3.json");
    }

    public void initBot(bool isBlack)
    {
        currentBoard = DataHandler.board;
        this.isBlack = isBlack;
    }

    public int[] FindNextMove()
    {
        Random rnd = new();
        List<DataHandler.Move> bestMoves = new();
        searchCounter = 0;
        List<DataHandler.Move> myMoves = currentBoard.GenerateMoveSet(isBlack);

        double bestEval = double.PositiveInfinity;

        foreach (var myMove in myMoves)
        {
            Bitboard afterMyMove = new();
            afterMyMove.SetBoard(currentBoard.whitePieces, currentBoard.blackPieces);
            afterMyMove.MakeMove(myMove, isBlack);

            if (DataHandler.IsKingUnderAttack(isBlack, afterMyMove))
                continue;

            double[] mating;
            double eval;
            if (!isBlack)
            {
                // Білий бот: перевертаємо позицію, щоб мережа бачила чорного як білого
                Bitboard flipped = FlipBoardPerspective(afterMyMove);
                eval = EvaluateWithNN(flipped);
                mating = EvaluateWithMNN(flipped);
                if (mating[2] == 1)
                    eval += 1;
                if (mating[0] == 1)
                    eval -= 1;
            }
            else
            {
                // Чорний бот: мережа і так бачить супротивника — білих
                eval = EvaluateWithNN(afterMyMove);
                mating = EvaluateWithMNN(afterMyMove);
                if (mating[2] == 1)
                    eval += 1;
                if (mating[0] == 1)
                    eval -= 1;
            }

            if (eval < bestEval)
            {
                bestEval = eval;
                bestMoves.Clear();
                bestMoves.Add(myMove);
            }
            else if (Math.Abs(eval - bestEval) < 1e-6)
            {
                bestMoves.Add(myMove);
            }
        }

        if (bestMoves.Count > 0)
        {
            currentMove = bestMoves[rnd.Next(bestMoves.Count)];
            GD.Print($"[Bot] Selected move: {currentMove.From} → {currentMove.To} (Eval = {bestEval:0.000})");
        }
        else
        {
            currentMove = new DataHandler.Move(-1, -1);
            GD.Print("[Bot] No valid move found.");
        }

        return new int[] { currentMove.From, currentMove.To };
    }


    private Bitboard FlipBoardPerspective(Bitboard board)
    {
        Bitboard flipped = new();
        for (int i = 0; i < 6; i++)
        {
            flipped.whitePieces[i] = ReverseBits(board.blackPieces[i]);
            flipped.blackPieces[i] = ReverseBits(board.whitePieces[i]);
        }
        return flipped;
    }

    private ulong ReverseBits(ulong value)
    {
        ulong result = 0;
        for (int i = 0; i < 64; i++)
            if ((value & (1UL << i)) != 0)
                result |= 1UL << (63 - i);
        return result;
    }

    // Removed: EvaluateMateClass

    private double EvaluateWithNN(Bitboard board)
    {
        double[] input = BitboardTo64Array(board);
        double[] output = nn.Predict(input);
        return output[0]; // Assuming 1 output neuron for evaluation
    }

    private double[] EvaluateWithMNN(Bitboard board)
    {
        double[] input = BitboardTo64Array(board);
        double[] output = mnn.Predict(input);
        return output; // Assuming 1 output neuron for evaluation
    }

    private double[] BitboardTo64Array(Bitboard board)
    {
        double[] input = new double[64];
        for (int i = 0; i < 6; i++)
        {
            for (int square = 0; square < 64; square++)
            {
                ulong mask = 1UL << square;
                if ((board.whitePieces[i] & mask) != 0)
                    input[square] = PieceIndexToValue(i);
                else if ((board.blackPieces[i] & mask) != 0)
                    input[square] = -PieceIndexToValue(i);
            }
        }
        return input;
    }

    private int PieceIndexToValue(int index)
    {
        return index switch
        {
            3 => 1, // pawn
            5 => 2, // rook
            2 => 3, // knight
            0 => 4, // bishop
            4 => 5, // queen
            1 => 6, // king
            _ => 0,
        };
    }
}
