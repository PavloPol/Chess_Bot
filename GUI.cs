using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class GUI : Control
{
	PackedScene pieceScene;
    GridContainer BoardGrid;
	ColorRect ChessBoard;
	Bitboard Bitboard;
	GeneratePath GeneratePath;
	Piece SelectedPiece = null;
	ChessBot ChessBot;
	Label Message;
	bool GameStart = false;
	bool PlayersTurn = true;
	bool isPlayerBlack = false;

	Array<slot> GridArray = new Array<slot>();
	Vector2 IconOffset = new Vector2(39, 39);

	const string StartFen = "K7/7r/8/8/8/8/r7/k7 w KQkq - 0 1";
    // "8/8/k4r1R/8/8/8/8/8";
    // "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		ChessBoard = GetNode<ColorRect>("ChessBoard");
		BoardGrid = GetNode<GridContainer>("ChessBoard/BoardGrid");
		Bitboard = GetNode<Bitboard>("Bitboard");
		GeneratePath = GetNode<GeneratePath>("GeneratePath");
		ChessBot = GetNode<ChessBot>("ChessBot");
		Message = GetNode<Label>("Message");
		CreateSlots();
		PaintSlots();

		pieceScene = GD.Load<PackedScene>("res://piece.tscn");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("RightMouse") && SelectedPiece != null)
		{
			SelectedPiece = null;
			ClearBoardFilter();
		}
	}

	public void WinCheck(bool isBlack)
	{
		int counter = 0;

        List<DataHandler.Move> moves = Bitboard.GenerateMoveSet(!isBlack);

        foreach (DataHandler.Move move in moves)
        {
            Bitboard newBoard = new();
            newBoard.SetBoard(Bitboard.whitePieces, Bitboard.blackPieces);
            newBoard.MakeMove(move, !isBlack);
            if (!DataHandler.IsKingUnderAttack(!isBlack, newBoard))
            {
				counter++;
				break;
            }
        }

		if(counter == 0)
		{
			if(DataHandler.IsKingUnderAttack(!isBlack, Bitboard))
			{
				GameOver((isBlack) ? "Black win!" : "White win!");
				return;
			}
			GameOver("Stalemate");
		}
    }

	public void GameOver(string message)
	{
        Message.Text = message;
        Message.Visible = true;
        //ClearBoardFilter();
        //ClearPieceArray();
        SelectedPiece = null;
        GameStart = false;
    }

	public void CreateSlots()
	{
		foreach(slot item in BoardGrid.GetChildren())
		{
			GridArray.Add(item);
			item.Connect("SlotClicked", new Callable(this, "OnSlotClicked"));
		}
	}

	public void OnSlotClicked(slot slot)
	{
		if(GameStart && PlayersTurn)
		{
			if(SelectedPiece == null)
			{
				return;
			}
			if(slot.state != (int)DataHandler.SlotStates.FREE)
			{
				SelectedPiece = null;
				ClearBoardFilter();
				return;
			}
            PlayersTurn = !PlayersTurn;
            MovePiece(SelectedPiece, slot.SlotID);
			ClearBoardFilter();
			SelectedPiece = null;

			WinCheck(isPlayerBlack);

			if (GameStart)
			{
				BotsTurn();
				WinCheck(!isPlayerBlack);
			}
		}
	}

	public void BotsTurn()
	{
        var move = ChessBot.FindNextMove();
        PlayersTurn = !PlayersTurn;
        MovePiece(DataHandler.PieceArray[63 - move[0]], 63 - move[1]);
    }

	public void MovePiece(Piece piece, int location)
	{
		if (piece.Type == 1)
		{
			if (piece.SlotID - location == 2) // quenn side castle white
			{
				MovePiece(DataHandler.PieceArray[56], location + 1);
			}
			else if (piece.SlotID - location == -2) // king side castle white
			{
				MovePiece(DataHandler.PieceArray[63], location - 1);
			}
		}
        if (piece.Type == 7)
        {
            if (piece.SlotID - location == 2) // quenn side castle black
            {
                MovePiece(DataHandler.PieceArray[0], location + 1);
            }
            else if (piece.SlotID - location == -2) // king side castle black
            {
                MovePiece(DataHandler.PieceArray[7], location - 1);
            }
        }

        if (DataHandler.PieceArray[location] != null)
		{
			RemoveFromBitBoard(DataHandler.PieceArray[location]);
			DataHandler.PieceArray[location].QueueFree();
			DataHandler.PieceArray[location] = null;
		}
		
		RemoveFromBitBoard(piece);
		piece.GlobalPosition =  GridArray[location].GlobalPosition + IconOffset;
        DataHandler.PieceArray[piece.SlotID] = null;
		DataHandler.PieceArray[location] = piece;
		piece.SlotID = location;
        piece.IsMoved = true;

        if (piece.Type == 3 && location < 8) // white pawn
        {
            piece.SetType(4); // promote to queen
        }
        if (piece.Type == 9 && location > 55)
        {
            piece.SetType(10);
        }

        Bitboard.AddPiece(63 - location, piece.Type);
	}

	public void RemoveFromBitBoard(Piece piece)
	{
		Bitboard.RemovePiece(63 - piece.SlotID, piece.Type);
	}

	public void PaintSlots()
	{
        int ColorBit = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (j % 2 == ColorBit)
                {
                    GridArray[i * 8 + j].Color = new Color(0.960784f, 0.960784f, 0.862745f, 1);
                }
            }
            if (ColorBit == 0)
            {
                ColorBit = 1;
            }
            else
            {
                ColorBit = 0;
            }
        }
    }

	public void AddPiece(PackedScene pieceScene, int pieceType, int location)
	{
		Piece newPiece = (Piece)pieceScene.Instantiate();
		ChessBoard.AddChild(newPiece);
		newPiece.Call("SetType", pieceType);
		newPiece.Call("SetSlotID", location);
		newPiece.Call("SetGlobalPosition", GridArray[location].GlobalPosition + IconOffset);
		DataHandler.PieceArray[location] = newPiece;
		newPiece.Connect("PieceSelected", new Callable(this, "OnPieceSelected"));
	}

	public void OnPieceSelected(Piece piece)
	{
		if (GameStart && PlayersTurn)
		{
			if (SelectedPiece != null)
			{
				OnSlotClicked(GridArray[piece.SlotID]);
			}
			else if ((piece.Type < 6 && isPlayerBlack == false) || (piece.Type > 5 && isPlayerBlack == true))
			{
				SelectedPiece = piece;
				ulong selfBitBoard = Bitboard.GetBlackBitBoard();
				ulong enemyBitBoard = Bitboard.GetWhiteBitBoard();
				bool isBlack = true;
				if (piece.Type < 6)
				{
					isBlack = false;
					ulong tmp = selfBitBoard;
					selfBitBoard = enemyBitBoard;
					enemyBitBoard = tmp;
				}
				ulong legalMoves = 1;
				switch (piece.Type % 6)
				{
					case 0:
						legalMoves = GeneratePath.BishopPath(63 - piece.SlotID, selfBitBoard, enemyBitBoard, isBlack);
						break;
					case 1:
						legalMoves = GeneratePath.KingPath(63 - piece.SlotID, selfBitBoard, enemyBitBoard, isBlack);
						break;
					case 2:
						legalMoves = GeneratePath.KnightPath(63 - piece.SlotID, selfBitBoard, enemyBitBoard, isBlack);
						break;
					case 3:
						legalMoves = GeneratePath.PawnPath(63 - piece.SlotID, selfBitBoard, enemyBitBoard, isBlack);
						break;
					case 4:
						legalMoves = GeneratePath.QueenPath(63 - piece.SlotID, selfBitBoard, enemyBitBoard, isBlack);
						break;
					case 5:
						legalMoves = GeneratePath.RookPath(63 - piece.SlotID, selfBitBoard, enemyBitBoard, isBlack);
						break;
				}
                legalMoves = DataHandler.CheckLegalMoves(legalMoves, 63 - piece.SlotID, isBlack);
                SetBoardFilter(legalMoves);
            }
		}
	}

    public void SetBoardFilter(ulong bitmap)
	{
		for(int i = 0; i < 64; i++)
		{
			if((bitmap & 1) != 0)
			{
				GridArray[63 - i].Call("SetFilter", (int)DataHandler.SlotStates.FREE);
			}
			bitmap = bitmap >> 1;
		}
	}

	public void ParseFen(string fen)
	{
		string[] BoardState = fen.Split(' ');
		int BoardIndex = 0;
		foreach(char i in BoardState[0])
		{
			if(i == '/')
			{
				continue;
			}
			if (char.IsDigit(i))
			{
				BoardIndex += (int)char.GetNumericValue(i);
			}
			else
			{
				AddPiece(pieceScene, DataHandler.Instance.FenDictionary[i], BoardIndex);
				BoardIndex++;
			}
		}
	}

    public void ClearBoardFilter()
    {
        foreach(slot i in GridArray)
		{
			i.SetFilter();
		}

    }

	public void OnPlayWhiteButtonPressed()
	{
		isPlayerBlack = false;
		PlayersTurn = true;

		StartGame();

        ChessBot.initBot(true);
	}

    public void OnPlayBlackButtonPressed()
    {
		isPlayerBlack = true;
		PlayersTurn = false;

		StartGame();

        ChessBot.initBot(false);
		BotsTurn();
    }

	private void StartGame()
	{
        Message.Visible = false;
        ClearBoardFilter();
        ClearPieceArray();
        SelectedPiece = null;
        ParseFen(StartFen);
        Bitboard.InitBitBoard(StartFen);
        DataHandler.board = Bitboard;
        GameStart = true;
    }

    public void ClearPieceArray()
	{
		for(int i = 0; i < 64; i++)
		{
			if(DataHandler.PieceArray[i] != null)
			{
                DataHandler.PieceArray[i].QueueFree();
                DataHandler.PieceArray[i] = null;

            }
		}
	}

}
