using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;

public partial class GUI : Control
{
	PackedScene pieceScene;
    GridContainer BoardGrid;
	ColorRect ChessBoard;
	Bitboard Bitboard;
	GeneratePath GeneratePath;
	Piece SelectedPiece = null;
	ChessBotNoAI WhiteChessBot;
	ChessBotNoAI BlackChessBot;
    TextEdit WhitePath;
    TextEdit BlackPath;
    Label Message;
	bool GameStart = false;
	bool PlayersTurn = true;
	bool isPlayerBlack = false;
    bool isWhiteTurn = true; // White starts
    bool BotVsBotMode = false;
    float botMoveDelay = 0.5f;
    private bool botGameRunning = false;


    Array<slot> GridArray = new Array<slot>();
	Vector2 IconOffset = new Vector2(39, 39);

	const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    // "8/8/k4r1R/8/8/8/8/8";
    // "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        WhitePath = GetNode<TextEdit>("WhitePath");
        BlackPath = GetNode<TextEdit>("BlackPath");
        ChessBoard = GetNode<ColorRect>("ChessBoard");
		BoardGrid = GetNode<GridContainer>("ChessBoard/BoardGrid");
		Bitboard = GetNode<Bitboard>("Bitboard");
		GeneratePath = GetNode<GeneratePath>("GeneratePath");
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

    public async void StartBotGameLoop()
    {
        if (botGameRunning) return;
        botGameRunning = true;

        while (GameStart && BotVsBotMode)
        {
            var bot = isWhiteTurn ? WhiteChessBot : BlackChessBot;
            BotsTurn(bot);
            WinCheck(!bot.isBlack);
            isWhiteTurn = !isWhiteTurn;

            await ToSignal(GetTree().CreateTimer(botMoveDelay), "timeout");
        }

        botGameRunning = false;
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
        GameStart = false;
        BotVsBotMode = false;
        SelectedPiece = null;
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
				BotsTurn(WhiteChessBot);
				WinCheck(!isPlayerBlack);
			}
		}
	}

    public void BotsTurn(ChessBotNoAI Bot)
    {
        int[] move = Bot.FindNextMove();

        if (move[0] < 0 || move[0] > 63 || move[1] < 0 || move[1] > 63)
        {
            GD.PrintErr($"[BotsTurn] Bot returned invalid move: {move[0]} → {move[1]}");
            return;
        }

        int from = 63 - move[0];
        int to = 63 - move[1];

        Piece piece = DataHandler.PieceArray[from];
        if (piece == null)
        {
            GD.PrintErr($"[BotsTurn] No piece at {from} to move to {to}.");
            return;
        }

        PlayersTurn = !PlayersTurn;
        System.Threading.Thread.Sleep(250);
        MovePiece(piece, to);
    }

    public void MovePiece(Piece piece, int location)
    {
        if (piece == null)
        {
            GD.PrintErr($"[MovePiece] Tried to move a null piece to {location}.");
            return;
        }

        if (location < 0 || location > 63)
        {
            GD.PrintErr($"[MovePiece] Invalid target location: {location}");
            return;
        }

        // Castling logic
        if (piece.Type == 1 && (piece.SlotID - location == 2 || piece.SlotID - location == -2))
        {
            int rookFrom = (piece.SlotID - location == 2) ? 56 : 63;
            int rookTo = (piece.SlotID - location == 2) ? location + 1 : location - 1;
            Piece rook = DataHandler.PieceArray[rookFrom];
            if (rook != null) MovePiece(rook, rookTo);
        }

        if (piece.Type == 7 && (piece.SlotID - location == 2 || piece.SlotID - location == -2))
        {
            int rookFrom = (piece.SlotID - location == 2) ? 0 : 7;
            int rookTo = (piece.SlotID - location == 2) ? location + 1 : location - 1;
            Piece rook = DataHandler.PieceArray[rookFrom];
            if (rook != null) MovePiece(rook, rookTo);
        }

        Piece target = DataHandler.PieceArray[location];
        if (target != null)
        {
            RemoveFromBitBoard(target);
            target.QueueFree();
            DataHandler.PieceArray[location] = null;
        }

        RemoveFromBitBoard(piece);
        piece.GlobalPosition = GridArray[location].GlobalPosition + IconOffset;
        DataHandler.PieceArray[piece.SlotID] = null;
        DataHandler.PieceArray[location] = piece;
        piece.SlotID = location;
        if(piece.Type == 1)
        {
            DataHandler.board.WhiteKingMoved = true;
        }
        if(piece.Type == 5)
        {
            if(piece.SlotID == 63)
            {
                DataHandler.board.WhiteKingsideRookMoved = true;
            }
            if (piece.SlotID == 56)
            {
                DataHandler.board.WhiteQueensideRookMoved = true;
            }
        }
        if (piece.Type == 11)
        {
            if (piece.SlotID == 7)
            {
                DataHandler.board.BlackKingsideRookMoved = true;
            }
            if (piece.SlotID == 0)
            {
                DataHandler.board.BlackQueensideRookMoved = true;
            }
        }
        if (piece.Type == 7)
        {
            DataHandler.board.BlackKingMoved = true;
        }


        if (piece.Type == 3 && location < 8) piece.SetType(4); // Promote white pawn
        if (piece.Type == 9 && location > 55) piece.SetType(10); // Promote black pawn

        Bitboard.AddPiece(63 - location, piece.Type);
    }

    public async void RunBotVsBotGames(int gameCount = 10)
    {
        List<string> results = new();
        for (int i = 1; i <= gameCount; i++)
        {
            GD.Print($"Starting game {i}...");
            StartGame();
            BotVsBotMode = true;
            isPlayerBlack = false;
            PlayersTurn = false;
            isWhiteTurn = true;

            WhiteChessBot.initBot(false);
            BlackChessBot.initBot(true);

            int moveCount = 0;
            int noCaptureOrPawnMove = 0;
            int maxMovesWithoutProgress = 50;

            while (GameStart)
            {
                await ToSignal(GetTree().CreateTimer(botMoveDelay), "timeout");

                var activeBot = isWhiteTurn ? WhiteChessBot : BlackChessBot;
                int[] move = activeBot.FindNextMove();
                if (move[0] == -1)
                {
                    string winner = isWhiteTurn ? "Black win!" : "White win!";
                    GameOver(winner);
                    results.Add($"Game {i}: {winner}");
                    break;
                }

                var targetPiece = DataHandler.PieceArray[63 - move[1]];
                var movingPiece = DataHandler.PieceArray[63 - move[0]];
                bool capture = targetPiece != null;
                bool isPawn = (movingPiece.Type == 3 || movingPiece.Type == 9);

                if (!capture && !isPawn)
                    noCaptureOrPawnMove++;
                else
                    noCaptureOrPawnMove = 0;

                MovePiece(movingPiece, 63 - move[1]);
                WinCheck(!activeBot.isBlack);

                isWhiteTurn = !isWhiteTurn;
                moveCount++;

                if (noCaptureOrPawnMove >= maxMovesWithoutProgress)
                {
                    GameOver("Draw by 50-move rule");
                    results.Add($"Game {i}: Draw");
                    break;
                }
            }

            if (Message.Text.Contains("Stalemate"))
                results.Add($"Game {i}: Draw (Stalemate)");
        }

        GameLogger.WriteLog(results);
        GD.Print("Games complete. Results saved.");
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

	public void OnRunBotGamesPressed()
	{
		RunBotVsBotGames();
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

    public void OnBotVsBotPressed()
    {
        isPlayerBlack = false;
        PlayersTurn = false;
        BotVsBotMode = true;
        isWhiteTurn = true;

        StartGame();

        WhiteChessBot.initBot(false); // white
        BlackChessBot.initBot(true);  // black

        StartBotGameLoop(); // <-- Add this line
    }

    public void OnPlayWhiteButtonPressed()
	{
		isPlayerBlack = false;
		PlayersTurn = true;

		StartGame();

        WhiteChessBot.initBot(true);
	}

    public void OnPlayBlackButtonPressed()
    {
		isPlayerBlack = true;
		PlayersTurn = false;

		StartGame();

        WhiteChessBot.initBot(false);
		BotsTurn(WhiteChessBot);
    }

	private void StartGame()
	{
        InitNNs();
        Message.Visible = false;
        ClearBoardFilter();
        ClearPieceArray();
        SelectedPiece = null;
        ParseFen(StartFen);
        Bitboard.InitBitBoard(StartFen);
        DataHandler.board = Bitboard;
        GameStart = true;
    }

    private void InitNNs()
    {
        string WhitePaths = WhitePath.Text;
        string BlackPaths = BlackPath.Text;
        if (WhitePaths.Substring(WhitePaths.Length - 6) != ".json")
            WhitePaths = "Train/Model_128_256_64_32.json";
        else if(!File.Exists(WhitePaths))
            WhitePaths = "Train/Model_128_256_64_32.json";

        if (BlackPaths.Substring(BlackPaths.Length - 6) != ".json")
            BlackPaths = "Train/Model_128_128_64_64_32_16.json";
        else if (!File.Exists(BlackPaths))
            BlackPaths = "Train/Model_128_128_64_64_32_16.json";

        WhiteChessBot = new ChessBotNoAI(WhitePaths);
        BlackChessBot = new ChessBotNoAI(BlackPaths);
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
