using Godot;

namespace Ches.Chess;
public partial class Movement : CharacterBody2D
{
    [Signal]
    public delegate void pieceSelectedEventHandler();

    [Signal]
    public delegate void moveSelectedEventHandler();

    [Signal]
    public delegate void captureEventHandler();

    private bool _isCapture;
    private bool _isCastling;

    private Piece _castlingTarget;

    private Vector2 _castlingPosition;

    public override void _Ready()
    {
        Node2D master = GetNode<Node2D>("../../../..");
        TileMap newParent = GetNode<TileMap>("../../..");
        Node ogParent = GetParent();

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
            if (_isCapture)
            {
                GD.Print("---------CAPTURE UPDATE TILES---------");
                EmitSignal(SignalName.capture, Position, this);
            }
            else
            {
                EmitSignal(SignalName.moveSelected, Position);
                EmitSignal(SignalName.pieceSelected);

                if (_isCastling)
                {
                    _castlingTarget.Castle(_castlingPosition);
                }
            }
            GD.Print("Move selected, update tiles");
        }
    }

    public void Captured()
    {
        EmitSignal(SignalName.moveSelected, Position);
        EmitSignal(SignalName.pieceSelected);
    }

    public void SetCastling(Piece target, Vector2 targetPostion)
    {
        _isCastling = true;
        _castlingTarget = target;
        _castlingPosition = targetPostion;
    }
}