using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;

public partial class Bitboard : Node
{
	public ulong[] whitePieces = { 0, 0, 0, 0, 0, 0 }; // bishop king knight pawn queen rook
	public ulong[] blackPieces = { 0, 0, 0, 0, 0, 0 };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public ulong GetBlackBitBoard()
	{
		ulong ans = 0;
		foreach(ulong i in blackPieces)
		{
			ans |= i;
		}
		return ans;
	}

	public ulong GetWhiteBitBoard()
	{
        ulong ans = 0;
        foreach (ulong i in whitePieces)
        {
            ans |= i;
        }
        return ans;
    }

	public void ClearBitBoard()
	{
		Array.Clear(whitePieces);
		Array.Clear(blackPieces);
    }

	public void InitBitBoard(string fen)
	{
		ClearBitBoard();
		string[] fenSplit = fen.Split(' ');
		foreach(char i in fenSplit[0])
		{
			if(i.Equals('/'))
			{
				continue;
			}
			if (Char.IsDigit(i))
			{
				int shiftAmount = int.Parse(i.ToString());
				LeftShift(shiftAmount);
				continue;
			}
			LeftShift(1);
			if(Char.IsUpper(i)) 
			{
				whitePieces[DataHandler.FenDict[Char.ToLower(i)]] |= 1UL;
			}
			else
			{
				blackPieces[DataHandler.FenDict[Char.ToLower(i)]] |= 1UL;
            }
        }
    }

	private void LeftShift(int shiftAmount)
	{
		for(int piece = 0; piece < blackPieces.Length; piece++)
		{
			blackPieces[piece] <<= shiftAmount;
		}
		for(int piece = 0; piece < whitePieces.Length; piece++)
		{
			whitePieces[piece] <<= shiftAmount;
		}
	}

	public void RemovePiece(int location, int pieceType)
	{
		if(pieceType > 5)
		{
			blackPieces[pieceType % 6] &= ~(1UL << location);
		}
		else
		{
            whitePieces[pieceType % 6] &= ~(1UL << location);
        }
    }

	public void AddPiece(int location, int pieceType)
	{
		if(pieceType > 5)
		{
			blackPieces[pieceType % 6] |= 1UL << location;
		}
		else
		{
            whitePieces[pieceType % 6] |= 1UL << location;
        }
    }

	public void MakeMove(DataHandler.Move move, bool isBlackMove)
	{
		ulong[] fromList = isBlackMove ? blackPieces : whitePieces;
        ulong[] toList = isBlackMove ? whitePieces : blackPieces;

		ulong fromBit = 1UL << move.From;
		ulong toBit = 1UL << move.To;

		for(int i = 0; i < 6; i++)
		{
			toList[i] &= ~(toBit);
		}
		for(int i = 0; i < 6; i++)
		{
			if (move.castle)
			{
                HandleCastle(move, isBlackMove);
            }
			if ((fromList[i] & fromBit) != 0)
			{
				fromList[i] &= ~(fromBit);
                if (move.promote && IsPawn(i)) // Check for promotion
                {
                    PromoteToQueen(fromList, toBit);
                }
                else
                {
                    fromList[i] |= toBit;
                }
            }
		}
    }

    private bool IsPawn(int pieceIndex)
    {
        // Assuming pieceIndex 0 represents pawns
        return pieceIndex == 3;
    }

    private void PromoteToQueen(ulong[] fromList, ulong toBit)
    {
        fromList[4] |= toBit; // Set the bit for the queen at the destination
    }

    private void HandleCastle(DataHandler.Move move, bool isBlackMove)
    {
        ulong[] fromList = isBlackMove ? blackPieces : whitePieces;

        // Determine the rook's from and to squares based on the king's move
        if (move.To == 2) // Queenside castle
        {
            // Move the rook from its initial square (0) to its new square (3)
            ulong rookFromBit = 1UL << (move.From - 4);
            ulong rookToBit = 1UL << (move.From - 1);
            MoveRookForCastle(fromList, rookFromBit, rookToBit);
        }
        else if (move.To == 6) // Kingside castle
        {
            // Move the rook from its initial square (7) to its new square (5)
            ulong rookFromBit = 1UL << (move.From + 3);
            ulong rookToBit = 1UL << (move.From + 1);
            MoveRookForCastle(fromList, rookFromBit, rookToBit);
        }
    }

    private void MoveRookForCastle(ulong[] fromList, ulong rookFromBit, ulong rookToBit)
    {
        // Loop through the rooks in the fromList to find and move the rook
        for (int i = 0; i < 6; i++)
        {
            if ((fromList[i] & rookFromBit) != 0)
            {
                fromList[i] &= ~(rookFromBit);
                fromList[i] |= rookToBit;
            }
        }
    }

    public List<DataHandler.Move> GenerateMoveSet(bool isBlackMove)
	{
		ulong[] searchList;
		ulong selfBoard, enemyBoard;
		List<DataHandler.Move> moveSet = new();
		GeneratePath pathGenerator = new();
		if (isBlackMove)
		{
			selfBoard = GetBlackBitBoard();
			enemyBoard = GetWhiteBitBoard();
			searchList = blackPieces;
		}
		else
		{
            selfBoard = GetWhiteBitBoard();
            enemyBoard = GetBlackBitBoard();
            searchList = whitePieces;
        }

		//Bishop
		for(int i = 0; i < 64; i++)
		{
			if ((searchList[0] & (1UL << i)) != 0)
			{
				ulong currentMoves = pathGenerator.BishopPath(i, selfBoard, enemyBoard, isBlackMove);
				for(int j = 0; j < 64; j++)
				{
					if((currentMoves & (1UL << j)) != 0)
					{
						DataHandler.Move newMove = new(i, j);
						moveSet.Add(newMove);
					}
				}
			}
		}

        //King
        for (int i = 0; i < 64; i++)
        {
            if ((searchList[1] & (1UL << i)) != 0)
            {
                ulong currentMoves = pathGenerator.KingPath(i, selfBoard, enemyBoard, isBlackMove);
                for (int j = 0; j < 64; j++)
                {
                    if ((currentMoves & (1UL << j)) != 0)
                    {
                        DataHandler.Move newMove = new(i, j);
                        if (Math.Abs(i - j) > 1)
                        {
							newMove.castle = true;
                        }
                        moveSet.Add(newMove);
                    }
                }
            }
        }

        //Knight
        for (int i = 0; i < 64; i++)
        {
            if ((searchList[2] & (1UL << i)) != 0)
            {
                ulong currentMoves = pathGenerator.KnightPath(i, selfBoard, enemyBoard, isBlackMove);
                for (int j = 0; j < 64; j++)
                {
                    if ((currentMoves & (1UL << j)) != 0)
                    {
                        DataHandler.Move newMove = new(i, j);
                        moveSet.Add(newMove);
                    }
                }
            }
        }

        //Pawn
        for (int i = 0; i < 64; i++)
        {
            if ((searchList[3] & (1UL << i)) != 0)
            {
                ulong currentMoves = pathGenerator.PawnPath(i, selfBoard, enemyBoard, isBlackMove);
                for (int j = 0; j < 64; j++)
                {
                    if ((currentMoves & (1UL << j)) != 0)
                    {
                        DataHandler.Move newMove = new(i, j);
                        if (isBlackMove && j < 8)
                        {
                            newMove.promote = true;
                        }
                        if (!isBlackMove && j > 55)
                        {
                            newMove.promote = true;
                        }
                        moveSet.Add(newMove);
                    }
                }
            }
        }

        //Queen
        for (int i = 0; i < 64; i++)
        {
            if ((searchList[4] & (1UL << i)) != 0)
            {
                ulong currentMoves = pathGenerator.QueenPath(i, selfBoard, enemyBoard, isBlackMove);
                for (int j = 0; j < 64; j++)
                {
                    if ((currentMoves & (1UL << j)) != 0)
                    {
                        DataHandler.Move newMove = new(i, j);
                        moveSet.Add(newMove);
                    }
                }
            }
        }

        //Rook
        for (int i = 0; i < 64; i++)
        {
            if ((searchList[5] & (1UL << i)) != 0)
            {
                ulong currentMoves = pathGenerator.RookPath(i, selfBoard, enemyBoard, isBlackMove);
                for (int j = 0; j < 64; j++)
                {
                    if ((currentMoves & (1UL << j)) != 0)
                    {
                        DataHandler.Move newMove = new(i, j);
                        moveSet.Add(newMove);
                    }
                }
            }
        }

        return moveSet;
    }

	public void SetBoard(ulong[] Whites, ulong[] Blacks)
	{
		Array.Copy(Whites, whitePieces, Whites.Length);
		Array.Copy(Blacks, blackPieces, Blacks.Length);
    }

}
