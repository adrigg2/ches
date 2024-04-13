using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches.Chess;
public partial class ChessGame : Node2D
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

    public static List<BoardState> BoardHistory { get; set; } = new(); //Check why static
    private Dictionary<int, Piece> _pieces;

    [Export] private int _turn;

    public int Turn { get => _turn; }

    public override void _EnterTree()
	{
        _board.BoardCellCount += SetBoardArrays;
        _board.PlayersSet += PlayersSet;
        _board.TimersSet += (timer, player) => EmitSignal(SignalName.TimersSet, timer, player);

        _turn = 1;
        _pieces = new();

        AddToGroup("to_save");
    }

    public override void _Ready()
    {
        GetTree().CallGroup("players", "ChangeTurn", _turn);
        GetTree().CallGroup("pieces", "ChangeTurn", _turn);
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

    public void SetBoardArrays(int rows, int columns)
    {
        _board.Cells = new int[rows, columns];
        _board.CheckCells = new CellSituation[rows, columns];
        DebugTracking(); //DEBUG
    }

    public void DebugTracking() //DEBUG
    {
        _debugTracker.Text = null;
        _debugTracker2.Text = null;
        for (int i = 0; i < _board.Cells.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Cells.GetLength(1); j++)
            {
                _debugTracker.Text += (_board.Cells[j, i] / 1000).ToString();
                _debugTracker2.Text += Math.Abs((int)_board.CheckCells[j, i]).ToString();
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
        oldArrPos = _board.LocalToMap(oldPos);

        _board.Cells[arrPos.X, arrPos.Y] = id;
        _board.Cells[oldArrPos.X, oldArrPos.Y] = 0;
    }

    private void ChangeTurn(int turn)
    {
        int[,] boardToSave = (int[,])_board.Cells.Clone();
        CellSituation[,] zoneOfControlToSave = (CellSituation[,])_board.CheckCells.Clone();

        int situationCount = BoardHistory.Count(b => _board.Cells.Cast<int>().SequenceEqual(b.Board.Cast<int>()));

        GD.Print($"This situation has been repeated {situationCount} times");

        CheckReset();
        if (turn == 1)
        {
            _turn = 2;
        }
        else if (turn == 2)
        {
            _turn = 1;
        }

        _camera.Zoom *= new Vector2(-1, -1);
        BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, _turn, true));
        EmitSignal(SignalName.TurnChanged, _turn, situationCount);

        if (situationCount == 5)
        {
            Checkmate(0);
        }

        GetTree().CallGroup("players", "ChangeTurn", _turn);
        GetTree().CallGroup("pieces", "ChangeTurn", _turn);
    }

    public int MovementCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return _board.Cells[arrPos.X, arrPos.Y];
    }

    public void PlayersSet()
    {
        Callable checkPiece = new Callable(this, "CheckPiece");

        foreach (Node child in _board.GetChildren())
        {
            if (child is Player player)
            {
                player.CheckFinished += CheckFinished;
                //player.Checkmate += Checkmate;

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

        int[,] boardToSave = new int[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];
        CellSituation[,] zoneOfControlToSave = new CellSituation[_board.CheckCells.GetLength(0), _board.CheckCells.GetLength(1)];

        for (int i = 0; i < _board.Cells.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Cells.GetLength(1); j++)
            {
                boardToSave[i, j] = _board.Cells[i, j];
                zoneOfControlToSave[i, j] = _board.CheckCells[i, j];
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
        _board.CheckCells = new CellSituation[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];
        DebugTracking(); //DEBUG
    }

    public CellSituation CheckCheck(Vector2 posCheck) //FIXME: this and CheckArrayCheck are the same
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return _board.CheckCells[arrPos.X, arrPos.Y];
    }

    public void CheckFinished()
    {
        GetTree().CallGroup("pieces", "CheckCheckState");
    }

    public CellSituation CheckArrayCheck(Vector2 posCheck) //FIXME: this and CheckCheck are the same
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return _board.CheckCells[arrPos.X, arrPos.Y];
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

        _board.Cells = new int[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];
        _board.CheckCells = new CellSituation[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];

        _board.Reset();

        GetTree().CallGroup("pieces", "ChangeTurn");
        GetTree().CallGroup("players", "ChangeTurn", _turn);

        _debugTracker.Visible = true; //DEBUG
        _debugTracker2.Visible = true; //DEBUG
    }

    public void ClearEnPassant(int player)
    {
        for (int i = 0; i < _board.Cells.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Cells.GetLength(1); j++)
            {
                if (_board.Cells[i, j] / 10 == -player)
                {
                    GD.Print("Cleared En Passant");
                    _board.Cells[i, j] = 0;
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
        _board.Cells = BoardHistory[boardIndex].Board;
        _board.CheckCells = BoardHistory[boardIndex].ZoneOfControl;
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

        int[,] boardToSave = (int[,])_board.Cells.Clone();
        CellSituation[,] zoneOfControlToSave = (CellSituation[,])_board.CheckCells.Clone();

        int situationCount = BoardHistory.Count(b => _board.Cells.Cast<int>().SequenceEqual(b.Board.Cast<int>()));

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
        return _pieces[id % 1000];
    }
}