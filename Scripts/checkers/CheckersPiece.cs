using Godot;
using Godot.Collections;
using System;
using SysGeneric = System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersPiece : BasePiece, ISaveable
{
    private struct PosibleMovement
    {
        public PosibleMovement(bool isCapture, Vector2 position, Vector2? capturePosition)
        {
            IsCapture = isCapture;
            Position = position;
            CapturePosition = capturePosition;
        }

        public bool IsCapture { get; set; }
        public Vector2 Position { get; set; }
        public Vector2? CapturePosition { get; set; }
    }

    private static int _lastPieceID = 0;

    private Vector2 _direction;
    private bool _king;
    [Export] private Dictionary<int, Texture2D> _textures;
    private CheckersBoard _board;
    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;

    public int ID { get => id; }

    public void SetFields(bool king, CheckersBoard board, int player)
    {
        _king = king;
        _board = board;
        this.player = player;
    }

    public override void _Ready()
    {
        GD.Print("Piece _Ready");
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

        GetNode<Sprite2D>("Sprite2D").Texture = _textures[id / 100];
    }

    private void SetInitialTurn(int turn)
    {
        Vector2I position = _board.LocalToMap(Position);
        _board[position.X, position.Y] = id;
        this.turn = turn;
    }

    protected override void Movement()
    {
        if (turn != player) return;

        EmitSignal(SignalName.PieceSelected);

        SysGeneric.List<PosibleMovement> validMovements = new();

        if (!_king)
        {
            for (int i = -1; i < 2; i += 2)
            {
                Vector2 movePos = Position + new Vector2(i, 1) * _direction;
                var (availablePosition, movement) = CheckPosition(movePos, i);
                if (availablePosition)
                {
                    validMovements.Add(movement);
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

                    var (availablePosition, movement) = CheckPosition(movePos, j, j * i);
                    if (availablePosition && !validMovements.Exists(move => move.Position == movePos))
                    {
                        validMovements.Add(movement);
                    }
                    else if (!availablePosition)
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

                    var (availablePosition, movement) = CheckPosition(movePos, j, j * i);
                    if (availablePosition && !validMovements.Exists(move => move.Position == movePos))
                    {
                        validMovements.Add(movement);
                    }
                    else if (!availablePosition)
                    {
                        break;
                    }
                }
            }
        }

        bool capture = false;
        if (validMovements.Exists(move => move.IsCapture))
        {
            validMovements.RemoveAll(move => !move.IsCapture);
            capture = true;
        }

        foreach (var move in validMovements)
        {
            CheckersMovement movement = (CheckersMovement)_movement.Instantiate();
            movement.Position = move.Position;
            
            if (capture)
            {
                //movement.SetCapture(move.CapturePosition); //FIXME: use piece id to retrieve piece
            }
        }

        (bool, PosibleMovement) CheckPosition(Vector2 position, int xIncrease, int yIncrease = 1)
        {
            bool availablePosition = false;
            bool capture = false;
            Vector2 movementPosition = position;
            Vector2? capturePosition = null;

            bool notOutOfBounds = Position.X >= 0 && Position.X < 8 && Position.Y >= 0 && Position.Y < 8;
            if (notOutOfBounds && CheckBoard(Position) == 0)
            {
                availablePosition = true;
            }
            else if (notOutOfBounds && CheckBoard(Position) / 1000 != player)
            {
                position += new Vector2(xIncrease, yIncrease).Normalized() * (float)Math.Sqrt(2) * _direction;

                Vector2 posibleCapture = position;

                notOutOfBounds = Position.X >= 0 && Position.X < 8 && Position.Y >= 0 && Position.Y < 8;
                if (notOutOfBounds && CheckBoard(Position) == 0)
                {
                    availablePosition = true;
                    capture = true;
                    capturePosition = posibleCapture;
                }
            }
            return (availablePosition, new PosibleMovement(capture, movementPosition, capturePosition));
        }
    }

    public override void Capture()
    {
        GD.PrintRich($"[color=red]Capturing {this}[/color]");
        Vector2I position = _board.LocalToMap(Position);
        _board[position.X, position.Y] = 0;
        Delete();
    }

    private int CheckBoard(Vector2 position)
    {
        Vector2I positionI = _board.LocalToMap(position);
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
