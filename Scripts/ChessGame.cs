using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches.Chess;
public partial class ChessGame : Node2D, ISaveable
{
    [Signal]
    public delegate void TurnChangedEventHandler(int turn, int situationCount);

    [Signal]
    public delegate void GameEndedEventHandler(int loser);

    [Signal]
    public delegate void TimersSetEventHandler(Timer timer, int player);

    [Export] private Board _board;
    [Export] private Label _debugTracker;
    [Export] private Label _debugTracker2;
    [Export] private Camera2D _camera;

    public static List<BoardState> BoardHistory { get; set; } = new();
    private Dictionary<int, Piece> _pieces;

    [Export] private int _turn;

    public int Turn { get => _turn; }

    public override void _EnterTree()
    {
        _board.PlayersSet += PlayersSet;
        _board.TimersSet += (timer, player) => EmitSignal(SignalName.TimersSet, timer, player);

        _turn = 2;
        _pieces = new();

        AddToGroup("to_save");
    }

    public override void _Ready()
    {
        GetTree().CallGroup("pieces", "SetInitialTurn", _turn);
        GetTree().CallGroup("pieces", "UpdateCheck");
        ChangeTurn(_turn);
    }

    public override void _Process(double delta)
    {
        DebugTracking();
    }

    public void DisableMovement()
    {
        foreach (Node moveOption in _board.GetChildren())
        {
            if (moveOption is Movement)
            {
                moveOption.QueueFree();
            }
        }
    }

    public void DebugTracking() //DEBUG
    {
        _debugTracker.Text = null;
        _debugTracker2.Text = null;
        for (int i = 0; i < _board.Squares.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Squares.GetLength(1); j++)
            {
                _debugTracker.Text += (_board.Squares[j, i] / 1000).ToString();
                _debugTracker2.Text += Math.Abs((int)_board.CheckSquares[j, i]).ToString();
            }
            _debugTracker.Text += "\n";
            _debugTracker2.Text += "\n";
        }
    }

    public void UpdateBoard(Vector2 piecePos, Vector2 oldPos, int id)
    {
        Vector2I arrPos;
        Vector2I oldArrPos;

        arrPos = _board.LocalToMap(piecePos);
        _board.Squares[arrPos.X, arrPos.Y] = id;

        if (oldPos != new Vector2(-1, -1))
        {
            oldArrPos = _board.LocalToMap(oldPos);
            _board.Squares[oldArrPos.X, oldArrPos.Y] = 0;
        }
    }

    private void ChangeTurn(int turn)
    {
        int[,] boardToSave = (int[,])_board.Squares.Clone();
        SquareSituation[,] zoneOfControlToSave = (SquareSituation[,])_board.CheckSquares.Clone();

        int situationCount = BoardHistory.Count(b => _board.Squares.Cast<int>().SequenceEqual(b.Board.Cast<int>()));

        GD.Print($"This situation has been repeated {situationCount} times");

        CheckReset();
        if (turn == 1)
        {
            _turn = 2;
            _camera.Zoom = new Vector2(-1, -1);
        }
        else if (turn == 2)
        {
            _turn = 1;
            _camera.Zoom = new Vector2(1, 1);
        }

        BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, _turn, true));
        EmitSignal(SignalName.TurnChanged, _turn, situationCount);

        if (situationCount == 5)
        {
            Checkmate(0);
        }

        GetTree().CallGroup("pieces", "UpdateCheck");
        GetTree().CallGroup("players", "ChangeTurn", _turn);
        GetTree().CallGroup("pieces", "ChangeTurn", _turn);
    }

    public int MovementCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return _board.Squares[arrPos.X, arrPos.Y];
    }

    public void PlayersSet()
    {
        Callable checkPiece = new Callable(this, "CheckPiece");

        foreach (Node child in _board.GetChildren())
        {
            if (child is Player player)
            {
                player.CheckFinished += CheckFinished;
                player.Checkmate += Checkmate;

                foreach (Node grandchild in player.GetChildren())
                {
                    if (grandchild is Piece piece)
                    {
                        piece.PieceSelected += DisableMovement;
                        piece.PieceMoved += UpdateBoard;
                        piece.ClearEnPassant += ClearEnPassant;
                        piece.TurnFinished += ChangeTurn;
                        piece.CheckPiece = checkPiece;
                        _pieces.Add(piece.ID, piece);
                    }
                }
            }
        }

        GetTree().CallGroup("pieces", "SetInitialTurn");
        GetTree().CallGroup("pieces", "CheckMobility");
        GetTree().CallGroup("black_pieces", "UpdateCheck");

        int[,] boardToSave = new int[_board.Squares.GetLength(0), _board.Squares.GetLength(1)];
        SquareSituation[,] zoneOfControlToSave = new SquareSituation[_board.CheckSquares.GetLength(0), _board.CheckSquares.GetLength(1)];

        for (int i = 0; i < _board.Squares.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Squares.GetLength(1); j++)
            {
                boardToSave[i, j] = _board.Squares[i, j];
                zoneOfControlToSave[i, j] = _board.CheckSquares[i, j];
            }
        }

        BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, _turn, true));

        _camera.Zoom = new Vector2(1, 1);
    }

    public void Capture(Vector2 capturePos, CharacterBody2D capture)
    {
        GetTree().CallGroup("pieces", "Capture", capturePos, capture);
    }

    public void CheckReset()
    {
        _board.CheckSquares = new SquareSituation[_board.Squares.GetLength(0), _board.Squares.GetLength(1)];
    }

    public void CheckFinished()
    {
        GetTree().CallGroup("pieces", "CheckCheckState");
    }

    public void Checkmate(int loser)
    {
        _debugTracker.Visible = false; //DEBUG
        _debugTracker2.Visible = false; //DEBUG
        EmitSignal(SignalName.GameEnded, loser);
    }

    public void Reset()
    {
        BoardHistory.Clear();

        _board.Squares = new int[_board.Squares.GetLength(0), _board.Squares.GetLength(1)];
        _board.CheckSquares = new SquareSituation[_board.Squares.GetLength(0), _board.Squares.GetLength(1)];

        _board.Reset();

        GetTree().CallGroup("pieces", "ChangeTurn");
        GetTree().CallGroup("players", "ChangeTurn", _turn);

        _debugTracker.Visible = true; //DEBUG
        _debugTracker2.Visible = true; //DEBUG
    }

    public void ClearEnPassant(int player)
    {
        for (int i = 0; i < _board.Squares.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Squares.GetLength(1); j++)
            {
                if (_board.Squares[i, j] / 1000 == -player)
                {
                    GD.Print("Cleared En Passant");
                    _board.Squares[i, j] = 0;
                }
            }
        }
    }

    public void AgreedDraw()
    {
        Checkmate(0);
    }

    public void RevertGameStatus(int boardIndex)
    {
        int[,] board = BoardHistory[boardIndex].Board;
        
        int lastBoardHistory = BoardHistory.Count - 1;

        while (lastBoardHistory > boardIndex)
        {
            BoardHistory.RemoveAt(lastBoardHistory);
            lastBoardHistory--;
        }

        foreach (var player in _board.GetChildren())
        {
            if (player is Player player_)
            {
                player_.RevertPieces(board);
            }
        }

        _board.ClearDynamicTiles();
        _board.Squares = BoardHistory[boardIndex].Board;
        _board.CheckSquares = BoardHistory[boardIndex].ZoneOfControl;
        _turn = BoardHistory[boardIndex].Turn;

        if (_turn == 1)
        {
            _camera.Zoom = new Vector2(1, 1);
        }
        else if (_turn == 2)
        {
            _camera.Zoom = new Vector2(-1, -1);
        }

        PlayersSet();
    }

    public void PromotionComplete(Piece piece, int player)
    {
        piece.PieceSelected += DisableMovement;
        piece.PieceMoved += UpdateBoard;
        piece.ClearEnPassant += ClearEnPassant;

        int[,] boardToSave = (int[,])_board.Squares.Clone();
        SquareSituation[,] zoneOfControlToSave = (SquareSituation[,])_board.CheckSquares.Clone();

        int situationCount = BoardHistory.Count(b => _board.Squares.Cast<int>().SequenceEqual(b.Board.Cast<int>()));

        if (player == 1)
        {
            _turn = 2;
        }
        else if (player == 2)
        {
            _turn = 1;
        }
        _camera.Zoom *= new Vector2(-1, -1);
        BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, _turn, true));
        GetTree().CallGroup("pieces", "ChangeTurn");
        EmitSignal(SignalName.TurnChanged, _turn, situationCount);
    }

    public Piece CheckPiece(int id)
    {
        return _pieces[Math.Abs(id % 1000)];
    }

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { "Filename", SceneFilePath },
            { "Parent", GetParent().GetPath() },
            { "PosX", Position.X },
            { "PosY", Position.Y }
        }; //Find a way to store BoardHistory
    }

    public void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        Position = new Vector2((float)data["PosX"], (float)data["PosY"]);
    }
}