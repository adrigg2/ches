using Godot;
using Godot.Collections;
using System;

namespace Ches.Checkers;
public partial class CheckersPiece : BasePiece, ISaveable
{
    private static int _lastPieceID = 0;

    private Vector2 _direction;
    private bool _king;

    public void SetFields(bool king)
    {
        _king = king;
    }

    public override void _Ready()
    {
        if (!_king)
        {
            id = player * 1000 + _lastPieceID;
        }
        else
        {
            id = player * 1000 + 100 + _lastPieceID;
        }
        _lastPieceID++;
    }

    protected override void Movement()
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, Variant> Save()
    {
        throw new NotImplementedException();
    }

    public void Load(Dictionary<string, Variant> data)
    {
        throw new NotImplementedException();
    }
}
