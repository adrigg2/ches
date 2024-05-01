using Godot;
using System.Collections.Generic;

namespace Ches.Chess;
public partial class Movement : CharacterBody2D
{
    [Signal]
    public delegate void MoveSelectedEventHandler(Vector2 movePosition);

    [Signal]
    public delegate void EnPassantGeneratedEventHandler(Vector2 enPassantPos);

    private int _enPassantPlayer;

    private bool _isCapture;
    private bool _isCastling;
    private bool _enPassant;

    private Piece _target;

    private Vector2 _castlingPosition;

    private List<Vector2> _enPassantPositions;

    private Tween _scaleTween;

    private Vector2 _originalScale;

    public override void _Ready()
    {
        Node2D master = GetNode<Node2D>("../../../..");
        TileMap newParent = GetNode<TileMap>("../../..");

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        _isCapture = (bool)GetMeta("Is_Capture");

        _originalScale = Scale;
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            if (_isCastling)
            {
                _target.Castle(_castlingPosition);
            }
            else if (_enPassant)
            {
                foreach (Vector2 pos in _enPassantPositions)
                {
                    EmitSignal(SignalName.EnPassantGenerated, pos);
                }
            }
            else if (_isCapture)
            {
                _target.Capture();
            }

            EmitSignal(SignalName.MoveSelected, Position);
            GD.Print("Move selected, update tiles");
        }
    }

    public void SetCastling(Piece target, Vector2 targetPostion)
    {
        _isCastling = true;
        _target = target;
        _castlingPosition = targetPostion;
    }

    public void SetEnPassant(List<Vector2> enPassant, int player)
    {
        _enPassant = true;
        _enPassantPositions = enPassant;
        _enPassantPlayer = player;
    }

    public void SetCapture(Piece target)
    {
        _isCapture = true;
        _target = target;
    }

    public override void _MouseEnter()
    {
        Scale = _originalScale;

        _scaleTween?.Kill();
        _scaleTween = CreateTween();

        _scaleTween.TweenProperty(this, "scale", Scale * new Vector2(1.25f, 1.25f), .33f);
    }

    public override void _MouseExit()
    {
        _scaleTween?.Kill();
        _scaleTween = CreateTween();

        _scaleTween.TweenProperty(this, "scale", _originalScale, .33f);
    }
}