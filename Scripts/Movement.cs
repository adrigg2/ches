using Godot;
using System;

public partial class Movement : CharacterBody2D
{
    [Signal]
    public delegate void pieceSelectedEventHandler();

    [Signal]
    public delegate void moveSelectedEventHandler();

    [Signal]
    public delegate void captureEventHandler();

    bool isCapture;

    public override void _Ready()
    {
        Node2D master = GetNode<Node2D>("../../../..");
        TileMap newParent = GetNode<TileMap>("../../..");
        CharacterBody2D ogParent = (CharacterBody2D)GetParent();

        Callable _ogParent = new Callable(ogParent, "MovementSelected");
        Connect("moveSelected", _ogParent);

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        isCapture = (bool)GetMeta("Is_Capture");

        Callable _master = new Callable(master, "DisableMovement");
        Callable _master1 = new Callable(master, "Capture");
        Connect("pieceSelected", _master);
        Connect("capture", _master1);
    }
    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            if (isCapture == true)
            {
                EmitSignal(SignalName.capture, Position, this);
            }
            else
            {
                EmitSignal(SignalName.moveSelected, Position);
                EmitSignal(SignalName.pieceSelected);
            }
            GD.Print("piece " + Position + " selected!");
        }
    }

    public void DestroyMovePos()
    {
        GD.Print("destroying obsolete options");
        QueueFree();
    }

    public void Captured()
    {
        EmitSignal(SignalName.moveSelected, Position);
        EmitSignal(SignalName.pieceSelected);
    }
}