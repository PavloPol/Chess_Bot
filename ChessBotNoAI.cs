using ChessBot.AI;
using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;

public partial class ChessBotNoAI
{
    public Bitboard currentBoard;
    public DataHandler.Move currentMove = new(-1, -1);
    public DataHandler DH = new();
    public bool isBlack = true;

    private NeuralNetwork nn;
    private NeuralNetwork mnn;

    private int halfmoveCount = 0;
    private const int MateSearchMoveThreshold = 25;
    private const int OpponentPieceThreshold = 5;
    private const double MateProbabilityThreshold = 0.8;
    private const int IndexMateForWhite = 0;
    private const int IndexNoMate = 1;
    private const int IndexMateForBlack = 2;

    public ChessBotNoAI(string Path)
    {
        nn = new NeuralNetwork(Path);
        mnn = new NeuralNetwork("Train/Model_128_64_32_3.json");
    }

    public void initBot(bool isBlack)
    {
        currentBoard = DataHandler.board;
        this.isBlack = isBlack;
        halfmoveCount = 0;
    }

    public int[] FindNextMove()
    {
        halfmoveCount++;

        if (ShouldSearchForMate())
        {
            var mateMoves = currentBoard.GenerateMoveSet(isBlack);
            foreach (var move in mateMoves)
            {
                // Клонувати дошку і зробити хід
                var boardAfter = new Bitboard();
                boardAfter.SetBoard(currentBoard.whitePieces, currentBoard.blackPieces);
                boardAfter.MakeMove(move, isBlack);

                // Перевірка: чи ставить шах
                if (!DataHandler.IsKingUnderAttack(!isBlack, boardAfter))
                    continue;

                // Миттєвий мат у 1 хід: жодних ходів у опонента після шаху
                var oppMoves = boardAfter.GenerateMoveSet(!isBlack);
                if (oppMoves.Count == 0)
                {
                    currentMove = move;
                    return new[] { currentMove.From, currentMove.To };
                }

                // Інакше – оцінка ймовірності мату нейронною мережею
                double[] cls = EvaluateWithMNN(boardAfter);
                int idx = isBlack ? IndexMateForBlack : IndexMateForWhite;
                double mateProb = cls[idx];
                if (mateProb > MateProbabilityThreshold)
                {
                    currentMove = move;
                    return new[] { currentMove.From, currentMove.To };
                }
            }
        }

        var moves = currentBoard.GenerateMoveSet(isBlack);
        double bestScore = double.NegativeInfinity;
        DataHandler.Move bestMove = new(-1, -1);

        foreach (var myMove in moves)
        {
            var boardAfterBot = new Bitboard();
            boardAfterBot.SetBoard(currentBoard.whitePieces, currentBoard.blackPieces);
            boardAfterBot.MakeMove(myMove, isBlack);
            if (DataHandler.IsKingUnderAttack(isBlack, boardAfterBot))
                continue;

            double score = 0;

            int capByBot = GetPieceValueAt(currentBoard, myMove.To);
            score += capByBot * 0.2;

            var oppMoves = boardAfterBot.GenerateMoveSet(!isBlack);
            int maxCapByOpp = 0;
            foreach (var opp in oppMoves)
            {
                int cap = GetPieceValueAt(boardAfterBot, opp.To);
                if (cap > maxCapByOpp)
                    maxCapByOpp = cap;
            }
            score -= maxCapByOpp * 0.2;

            double eval;
            if (!isBlack)
            {
                var flipped = FlipBoardPerspective(boardAfterBot);
                eval = EvaluateWithNN(flipped);
            }
            else
            {
                eval = EvaluateWithNN(boardAfterBot);
            }
            score += -1 * eval;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = myMove;
            }
        }

        currentMove = bestMove;
        GD.Print($"[Bot] Selected move: {currentMove.From} → {currentMove.To} (Score = {bestScore:0.00})");
        return new[] { currentMove.From, currentMove.To };
    }

    private bool ShouldSearchForMate()
    {
        return halfmoveCount >= MateSearchMoveThreshold || CountOpponentPieces(currentBoard, isBlack) <= OpponentPieceThreshold;
    }

    private int CountOpponentPieces(Bitboard board, bool botIsBlack)
    {
        ulong[] opp = botIsBlack ? board.whitePieces : board.blackPieces;
        int count = 0;
        foreach (ulong bits in opp)
            count += BitOperations.PopCount(bits);
        return count;
    }

    private int GetPieceValueAt(Bitboard board, int sq)
    {
        ulong mask = 1UL << sq;
        for (int i = 0; i < 6; i++)
            if ((board.whitePieces[i] & mask) != 0 || (board.blackPieces[i] & mask) != 0)
                return DataHandler.Instance.pieceValues[i];
        return 0;
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
