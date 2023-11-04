using Godot;
using System;
using Chess;

namespace Chess;

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

        Connect("moveSelected", new Callable(ogParent, "MovementSelected"));

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        isCapture = (bool)GetMeta("Is_Capture");

        Connect("pieceSelected", new Callable(master, "DisableMovement"));
        Connect("capture", new Callable(master, "Capture"));
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