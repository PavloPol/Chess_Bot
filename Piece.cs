using Godot;
using System;

public partial class Piece : Node2D
{
	[Signal]
	public delegate void PieceSelectedEventHandler(Node2D piece);

	public Sprite2D Icon;
	public int SlotID = -1;
	public int Type;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Icon = GetNode<Sprite2D>("Icon");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetSlotID(int id)
	{
		SlotID = id;
	}

	public void SetType(int type)
	{
		Type = type;
		Icon.FrameCoords = DataHandler.Instance.PiecesIcons[type];
	}

	public void SetGlobalPosition(Vector2 position)
	{
        GlobalPosition = position;
	}


    public void OnButtonPressed()
    {
		EmitSignal(SignalName.PieceSelected, this);
    }
}
