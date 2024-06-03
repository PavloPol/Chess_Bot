using Godot;
using System;
using System.Collections.Generic;

public partial class DataHandler : Node
{
    private static DataHandler _instance;
    public static DataHandler Instance => _instance;

    public List<Vector2I> PiecesIcons = new List<Vector2I>();

	public Dictionary<char, int> FenDictionary = new Dictionary<char, int> 
	{
		{'b', (int)PieceNames.BLACK_BISHOP},
		{'r', (int)PieceNames.BLACK_ROOK},
		{'n', (int)PieceNames.BLACK_KNIGHT},
		{'q', (int)PieceNames.BLACK_QUEEN},
		{'k', (int)PieceNames.BLACK_KING},
		{'p', (int)PieceNames.BLACK_PAWN},
		{'B', (int)PieceNames.WHITE_BISHOP},
		{'R', (int)PieceNames.WHITE_ROOK},
		{'N', (int)PieceNames.WHITE_KNIGHT},
		{'Q', (int)PieceNames.WHITE_QUEEN},
		{'K', (int)PieceNames.WHITE_KING},
		{'P', (int)PieceNames.WHITE_PAWN}
    };

	public enum PieceNames
	{
		WHITE_BISHOP,
		WHITE_KING,
		WHITE_KNIGHT,
        WHITE_PAWN,
		WHITE_QUEEN,
		WHITE_ROOK,
		BLACK_BISHOP,
		BLACK_KING,
		BLACK_KNIGHT,
		BLACK_PAWN,
		BLACK_QUEEN,
		BLACK_ROOK,
	}

	public enum SlotStates { NONE, FREE }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PiecesIcons.Add(new Vector2I(2, 0));
		PiecesIcons.Add(new Vector2I(0, 0));
		PiecesIcons.Add(new Vector2I(3, 0));
		PiecesIcons.Add(new Vector2I(5, 0));
		PiecesIcons.Add(new Vector2I(1, 0));
		PiecesIcons.Add(new Vector2I(4, 0));
		PiecesIcons.Add(new Vector2I(2, 1));
		PiecesIcons.Add(new Vector2I(0, 1));
		PiecesIcons.Add(new Vector2I(3, 1));
		PiecesIcons.Add(new Vector2I(5, 1));
		PiecesIcons.Add(new Vector2I(1, 1));
		PiecesIcons.Add(new Vector2I(4, 1));
    }

    public override void _EnterTree()
    {
        if (_instance != null)
        {
            this.QueueFree(); // The Singletone is already loaded, kill this instance
        }
        _instance = this;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
