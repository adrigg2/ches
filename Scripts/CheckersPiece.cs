using Godot;
using Godot.Collections;
using System;
using SysGeneric = System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersPiece : BasePiece, ISaveable
{
    private static int _lastPieceID = 0;

    private Vector2 _direction;
    private bool _king;
    [Export] private Dictionary<int, Texture2D> _textures;
    private CheckersBoard _board;
    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;

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
        SysGeneric.List<Vector2> validMovements = new();

        if (!_king)
        {
            for (int i = -1; i < 2; i += 2)
            {
                Vector2 movePos = Position + new Vector2(i, 1) * _direction;
                if (CheckPosition(movePos, i))
                {
                    validMovements.Add(movePos);
                }
            }
        }
        else
        {
            for (int i = -1; i < 2; i += 2)
            {
                for (int j = -1; j > -9; j--)
                {
                    Vector2 movePos = Position + new Vector2(j, j * i) * _direction;
                    
                    if (CheckPosition(movePos, j, j * i) && !validMovements.Contains(movePos))
                    {
                        validMovements.Add(movePos);
                    }
                    else if (!CheckPosition(movePos, j, j * i))
                    {
                        break;
                    }
                }
            }

            for (int i = -1; i < 2; i += 2)
            {
                for (int j = 1; j < 9; j++)
                {
                    Vector2 movePos = Position + new Vector2(j, j * i) * _direction;
                    if (CheckPosition(movePos, j, j * i) && !validMovements.Contains(movePos))
                    {
                        validMovements.Add(movePos);
                    }
                    else if (!CheckPosition(movePos, j, j * i))
                    {
                        break;
                    }
                }
            }
        }

        bool CheckPosition(Vector2 position, int xIncrease, int yIncrease = 1)
        {
            bool availablePosition = false;
            bool notOutOfBounds = Position.X >= 0 && Position.X < 8 && Position.Y >= 0 && Position.Y < 8;
            if (notOutOfBounds && CheckBoard(Position) == 0)
            {
                availablePosition = true;
            }
            else if (notOutOfBounds && CheckBoard(Position) / 1000 != player)
            {
                Position += new Vector2(xIncrease, yIncrease).Normalized() * (float)Math.Sqrt(2) * _direction;
                notOutOfBounds = Position.X >= 0 && Position.X < 8 && Position.Y >= 0 && Position.Y < 8;
                if (notOutOfBounds && CheckBoard(Position) == 0)
                {
                    availablePosition = true;
                }
            }
            return availablePosition;
        }
    }

    private int CheckBoard(Vector2 position)
    {
        Vector2I positionI = _board.LocalToMap(Position);
        return _board[positionI.X, positionI.Y];
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
