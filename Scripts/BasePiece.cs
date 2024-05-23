using Godot;
using System;

namespace Ches;
public abstract partial class BasePiece : StaticBody2D
{
    private Tween _scaleTween;
    private Vector2 _originalScale;

    [Export]protected int id;
    [Export] protected int player;
    protected int turn;
    
    protected Vector2 OriginalScale { get => _originalScale; set => _originalScale = value; }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            Movement();
        }
    }

    protected abstract void Movement();

    public override void _MouseEnter()
    {
        if (player != turn)
        {
            return;
        }

        Scale = _originalScale;

        _scaleTween?.Kill();
        _scaleTween = CreateTween();

        _scaleTween.TweenProperty(this, "scale", Scale * new Vector2(1.25f, 1.25f), .33f);
    }

    public override void _MouseExit()
    {
        if (player != turn)
        {
            return;
        }

        _scaleTween?.Kill();
        _scaleTween = CreateTween();

        _scaleTween.TweenProperty(this, "scale", _originalScale, .33f);
    }

    public virtual void ChangeTurn(int turn)
    {
        _scaleTween?.Kill();
    }
}
