using Godot;
using Godot.Collections;
using System;
using SysGeneric = System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersPiece : BasePiece, ISaveable
{
    [Signal]
    public delegate void TurnFinishedEventHandler();

    [Signal]
    public delegate void PieceCapturedEventHandler();

    private struct PosibleMovement
    {
        public PosibleMovement(bool isCapture, Vector2 position, int? captureID)
        {
            IsCapture = isCapture;
            Position = position;
            CaptureID = captureID;
        }

        public bool IsCapture { get; set; }
        public Vector2 Position { get; set; }
        public int? CaptureID { get; set; }
    }

    private const int CellPixels = 32; // Move to board?

    private static int _lastPieceID = 0;

    private Vector2I _direction;
    private bool _king;
    [Export] private Dictionary<int, Texture2D> _textures;
    private CheckersBoard _board;
    private CheckersGame _game; //Temporary, find better solution
    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;

    public int ID { get => id; }

    public void SetFields(bool king, CheckersBoard board, int player, CheckersGame game)
    {
        _king = king;
        _board = board;
        _game = game;
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
        OriginalScale = Scale;

        if (player == 1)
        {
            _direction = new Vector2I(1, -1);
            //AddToGroup("white_pieces");
        }
        else
        {
            _direction = new Vector2I(1, 1);
            //AddToGroup("black_pieces");
        }

        GetNode<Sprite2D>("Sprite2D").Texture = _textures[id / 100];
    }

    public void SetInitialTurn(int turn)
    {
        _board[Position] = id;
        this.turn = turn;
    }

    protected override void Movement()
    {
        if (turn != player)
        {
            GD.Print($"Not my turn: turn: {turn} player: {player}");
            return;
        }

        EmitSignal(SignalName.PieceSelected);

        SysGeneric.List<PosibleMovement> validMovements = new();

        if (!_king)
        {
            for (int i = -1; i < 2; i += 2)
            {
                Vector2I movePosI = _board.LocalToMap(Position) + new Vector2I(i, 1) * _direction;
                Vector2 movePos = _board.MapToLocal(movePosI);
                var (availablePosition, movement) = CheckPosition(movePos, i);
                GD.Print($"Generating move {movePosI}, available: {availablePosition}, capture: {movement.IsCapture}");
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
                    Vector2I movePosI = _board.LocalToMap(Position) + new Vector2I(j, j * i) * _direction;
                    Vector2 movePos = _board.MapToLocal(movePosI);

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
                    Vector2I movePosI = _board.LocalToMap(Position) + new Vector2I(j, j * i) * _direction;
                    Vector2 movePos = _board.MapToLocal(movePosI);

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
            movement.MoveSelected += Move;
            
            if (capture)
            {
                movement.SetCapture(_game.CheckPiece(move.CaptureID ?? -1));
            }

            AddChild(movement);
        }

        (bool, PosibleMovement) CheckPosition(Vector2 position, int xIncrease, int yIncrease = 1)
        {
            bool availablePosition = false;
            bool capture = false;
            Vector2 movementPosition = position;
            int? captureID = null;

            bool notOutOfBounds = position.X >= 0 && position.X < 8 * CellPixels && position.Y >= 0 && position.Y < 8 * CellPixels;
            if (notOutOfBounds && CheckBoard(position) == 0)
            {
                availablePosition = true;
            }
            else if (notOutOfBounds && CheckBoard(position) / 1000 != player)
            {
                int posibleCapture = CheckBoard(position);

                position += new Vector2(xIncrease, yIncrease).Normalized() * (float)Math.Sqrt(2) * _direction * new Vector2(CellPixels, CellPixels);

                notOutOfBounds = position.X >= 0 && position.X < 8 * CellPixels && position.Y >= 0 && position.Y < 8 * CellPixels;
                if (notOutOfBounds && CheckBoard(position) == 0)
                {
                    availablePosition = true;
                    capture = true;
                    captureID = posibleCapture;
                    movementPosition = position;
                }
            }
            return (availablePosition, new PosibleMovement(capture, movementPosition, captureID));
        }
    }

    public void Move(Vector2 position)
    {
        Vector2 oldPosition = Position;
        Position = position;
        EmitSignal(SignalName.PieceSelected);
        _board[Position] = id;
        _board[oldPosition] = 0;
        EmitSignal(SignalName.TurnFinished);
    }

    public override void ChangeTurn(int turn)
    {
        base.ChangeTurn(turn);

        this.turn = turn;

        if (this.turn == 2)
        {
            Scale = new Vector2(-1, -1);
            OriginalScale = Scale;
        }
        else if (this.turn == 1)
        {
            Scale = new Vector2(1, 1);
            OriginalScale = Scale;
        }
    }

    public override void Capture()
    {
        GD.PrintRich($"[color=red]Capturing {this}[/color]");
        EmitSignal(SignalName.PieceCaptured);
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

    public override string ToString()
    {
        return $"Piece at ({(int)(Position.X / 32)}, {(int)(Position.Y / 32)}) from {player}";
    }
}
