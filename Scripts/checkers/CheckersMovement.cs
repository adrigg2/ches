using Godot;
using System;

namespace Ches.Checkers;
public partial class CheckersMovement : CharacterBody2D
{
    [Signal]
    public delegate void MoveSelectedEventHandler(Vector2 movePosition);

    private bool _isCapture;

    private CheckersPiece _target;

    private Tween _scaleTween;

    private Vector2 _originalScale;

    public override void _Ready()
    {
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
            if (_isCapture)
            {
                _target.Capture();
            }

            EmitSignal(SignalName.MoveSelected, Position);
            GD.Print("Move selected, update tiles");
        }
    }

    public void SetCapture(CheckersPiece target)
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
