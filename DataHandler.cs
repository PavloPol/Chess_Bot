using Godot;
using Godot.Collections;

public partial class DataHandler : Node
{
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

    private static DataHandler _instance;
    public static DataHandler Instance => _instance;

    public static Array<Piece> PieceArray = new Array<Piece>(new Piece[64]);

	public static Bitboard board;

    public static Dictionary<char, int> FenDict = new Dictionary<char, int>()
    {
        {'b', 0},
        {'k', 1},
        {'n', 2},
        {'p', 3},
        {'q', 4},
        {'r', 5}
    };

    public Array<Vector2I> PiecesIcons = new Array<Vector2I>();

    public int[] pieceValues = { 3, 1000000, 3, 1, 9, 5 };

    public struct Move
    {
        public int From { get; set; }
        public int To { get; set; }
		public bool castle = false;
		public bool promote = false;
        public Move(int a, int b)
        {
            From = a;
            To = b;
        }
    }

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

    public static bool IsKingUnderAttack(bool isBlack, Bitboard board)
    {
        ulong[] toList = isBlack ? board.blackPieces : board.whitePieces;
        System.Collections.Generic.List<DataHandler.Move> moves = board.GenerateMoveSet(!isBlack);
        foreach (DataHandler.Move move in moves)
        {
            ulong toBit = 1UL << move.To;
            if ((toList[1] & ~(toBit)) == 0)
            {
                return true;
            }
        }
        return false;
    }

    public static ulong CheckLegalMoves(ulong legalMoves, int position, bool isBlack)
    {
        for (int j = 0; j < 64; j++)
        {
            if ((legalMoves & (1UL << j)) != 0)
            {
                DataHandler.Move newMove = new(position, j);
                Bitboard newBoard = new();
                newBoard.SetBoard(DataHandler.board.whitePieces, DataHandler.board.blackPieces);
                newBoard.MakeMove(newMove, isBlack);
                if (DataHandler.IsKingUnderAttack(isBlack, newBoard))
                {
                    legalMoves ^= (1UL << j);
                }
            }
        }
        return legalMoves;
    }

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
