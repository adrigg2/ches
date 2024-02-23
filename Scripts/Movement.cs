using Godot;

namespace Ches;
public partial class Movement : CharacterBody2D
{
    [Signal]
    public delegate void pieceSelectedEventHandler();

    [Signal]
    public delegate void moveSelectedEventHandler();

    [Signal]
    public delegate void captureEventHandler();

    private bool _isCapture;

    public override void _Ready()
    {
        Node2D master = GetNode<Node2D>("../../../..");
        TileMap newParent = GetNode<TileMap>("../../..");
        Node ogParent = (Node)GetParent();

        Connect("moveSelected", new Callable(ogParent, "MovementSelected"));

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        _isCapture = (bool)GetMeta("Is_Capture");

        Connect("pieceSelected", new Callable(master, "DisableMovement"));
        Connect("capture", new Callable(master, "Capture"));
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            if (_isCapture == true)
            {
                GD.Print("---------CAPTURE UPDATE TILES---------");
                EmitSignal(SignalName.capture, Position, this);
            }
            else
            {
                EmitSignal(SignalName.moveSelected, Position);
                EmitSignal(SignalName.pieceSelected);
            }
            GD.Print("Move selected, update tiles");
        }
    }

    public void Captured()
    {
        EmitSignal(SignalName.moveSelected, Position);
        EmitSignal(SignalName.pieceSelected);
    }
}