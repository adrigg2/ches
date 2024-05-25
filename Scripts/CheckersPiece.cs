using Godot;
using Godot.Collections;
using System;

namespace Ches.Checkers;
public partial class CheckersPiece : BasePiece, ISaveable
{
    private static int _lastPieceID = 0;

    private Vector2 _direction;
    private bool _king;
    [Export]private Dictionary<int, Texture2D> _textures;
    private CheckersBoard _board;

    public int ID { get => id; }

    public void SetFields(bool king, CheckersBoard board)
    {
        _king = king;
        _board = board;
    }

    public override void _Ready()
    {
        AddToGroup("pieces");
        AddToGroup("to_save");

        if (!_king)
        {
            id = player * 1000 + _lastPieceID;
        }
        else
        {
            id = player * 1000 + 100 + _lastPieceID;
        }
        _lastPieceID++;

        GetNode<Sprite2D>("Sprite2D").Texture = _textures[id % 100];
    }

    private void SetInitialTurn(int turn)
    {
        Vector2I position = _board.LocalToMap(Position);
        _board[position.X, position.Y] = id;
        this.turn = turn;
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
