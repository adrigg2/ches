using Godot;
using System.Collections.Generic;

namespace Ches.Chess;
public partial class Movement : CharacterBody2D
{
    [Signal]
    public delegate void PieceSelectedEventHandler();

    [Signal]
    public delegate void MoveSelectedEventHandler(Vector2 movePosition);

    [Signal]
    public delegate void CaptureEventHandler();

    [Signal]
    public delegate void EnPassantGeneratedEventHandler(Vector2 enPassantPos);

    private int _enPassantPlayer;

    private bool _isCapture;
    private bool _isCastling;
    private bool _enPassant;

    private Piece _castlingTarget;

    private Vector2 _castlingPosition;

    private List<Vector2> _enPassantPositions;

    public override void _Ready()
    {
        Node2D master = GetNode<Node2D>("../../../..");
        TileMap newParent = GetNode<TileMap>("../../..");

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        _isCapture = (bool)GetMeta("Is_Capture");

        Connect("PieceSelected", new Callable(master, "DisableMovement"));
        Connect("Capture", new Callable(master, "Capture"));
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            if (_isCapture)
            {
                GD.Print("---------CAPTURE UPDATE TILES---------");
                EmitSignal(SignalName.Capture, Position, this);
            }
            else
            {
                EmitSignal(SignalName.MoveSelected, Position);
                EmitSignal(SignalName.PieceSelected);

                if (_isCastling)
                {
                    _castlingTarget.Castle(_castlingPosition);
                }
                else if (_enPassant)
                {
                    foreach (Vector2 pos in _enPassantPositions)
                    {
                        EmitSignal(SignalName.EnPassantGenerated, pos);
                    }
                }
            }
            GD.Print("Move selected, update tiles");
        }
    }

    public void Captured()
    {
        EmitSignal(SignalName.MoveSelected, Position);
        EmitSignal(SignalName.PieceSelected);
    }

    public void SetCastling(Piece target, Vector2 targetPostion)
    {
        _isCastling = true;
        _castlingTarget = target;
        _castlingPosition = targetPostion;
    }

    public void SetEnPassant(List<Vector2> enPassant, int player)
    {
        _enPassant = true;
        _enPassantPositions = enPassant;
        _enPassantPlayer = player;
    }
}