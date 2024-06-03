using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;

public partial class ChessBot : Node
{
	public int searchCounter = 0;
	[Export]
	public int maxDepth = 3;
	public Bitboard currentBoard;
	public DataHandlerCS.Move currentMove = new(-1, -1);
	public DataHandlerCS DH = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void initBot(Bitboard board)
	{
		currentBoard = board;
	}

	public int SearchMoves(bool isBlackMove, int depth, Bitboard searchBoard, int alpha = int.MinValue+2, int beta = int.MaxValue)
	{
		searchCounter++;
		if(depth == 0)
		{
			return Evaluate(isBlackMove, searchBoard);
		}
		List<DataHandlerCS.Move> moves = searchBoard.GenerateMoveSet(isBlackMove);

		foreach(DataHandlerCS.Move move in moves)
		{
			Bitboard newBoard = new();
			newBoard.SetBoard(searchBoard.whitePieces, searchBoard.blackPieces);
			newBoard.MakeMove(move, isBlackMove);
			int evaluation = -SearchMoves(!isBlackMove, depth - 1, newBoard, -beta, -alpha);
			if(depth == maxDepth && evaluation > alpha)
			{
				currentMove.From = move.From;
				currentMove.To = move.To;
			}
			alpha = Math.Max(evaluation, alpha);
			if(evaluation >= beta)
			{
				return beta;
			}
		}
		return alpha;
	}

	public int[] FindNextMove()
	{
		searchCounter = 0;
		SearchMoves(true, maxDepth, currentBoard);
		int[] nextMove = { currentMove.From, currentMove.To };
		currentBoard.MakeMove(currentMove, true);
		

		return nextMove;
	}

	public int Evaluate(bool isBlackMove, Bitboard searchBoard)
	{
		int whiteValues = pieceValues(searchBoard.whitePieces);
		int blackValues = pieceValues(searchBoard.blackPieces);
		int Evaluation = whiteValues - blackValues;

		return isBlackMove ? -Evaluation : Evaluation;
    }

	public int pieceValues(ulong[] pieces)
	{
		int totalValue = 0;
		for(int i = 0; i < 6; i++)
		{
			totalValue += BitOperations.PopCount(pieces[i]) * DH.pieceValues[i];
		}
		return totalValue;
	}

}
