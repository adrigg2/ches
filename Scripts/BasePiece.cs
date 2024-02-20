using Godot;
using System;

namespace Ches;
public partial class BasePiece : StaticBody2D
{
	protected int id;
	protected int player;

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            Movement();
        }
    }

    protected virtual void Movement()
    {

    }
}
