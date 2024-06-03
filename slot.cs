using Godot;
using System;

public partial class slot : ColorRect
{

	[Export]
	public int SlotID { get; set; } = -1;

	[Signal]
    public delegate void SlotClickedEventHandler(ColorRect slot);

    public ColorRect FilterPath;
	public int state = (int)DataHandler.SlotStates.NONE;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		FilterPath = GetNode<ColorRect>("Filter");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetFilter(int color = (int)DataHandler.SlotStates.NONE)
	{
		state = color;
		switch (color)
		{
			case (int)DataHandler.SlotStates.NONE:
				FilterPath.Color = new Color(0, 0, 0, 0);
				break;
			case (int)DataHandler.SlotStates.FREE:
				FilterPath.Color = new Color(0, 1, 0, 0.4f);
				break;
		}

	}

	public void OnFilterGuiInput(InputEvent @event)
	{
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                EmitSignal(SignalName.SlotClicked, this);
            }
        }
    }
}
