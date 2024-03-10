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

    public static List<BoardState> BoardHistory { get; set; } = new();

    public override void _EnterTree()
	{
        _board.BoardCellCount += SetBoardArrays;
        _board.PlayersSet += PlayersSet;
        _board.TimersSet += (timer, player) => EmitSignal(SignalName.TimersSet, timer, player);

        AddToGroup("to_save");
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
        _board.CheckCells = new int[rows, columns];
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
                _debugTracker2.Text += Math.Abs(_board.CheckCells[j, i]).ToString();
            }
            _debugTracker.Text += "\n";
            _debugTracker2.Text += "\n";
        }
    }

    public void UpdateBoard(Vector2 piecePos, Vector2 oldPos, int player, bool promotion)
    {
        Vector2I arrPos;
        Vector2I oldArrPos;

        arrPos = _board.LocalToMap(piecePos);
        oldArrPos = _board.LocalToMap(oldPos);

        _board.Cells[arrPos.X, arrPos.Y] = player;
        _board.Cells[oldArrPos.X, oldArrPos.Y] = 0;

        int[,] boardToSave = (int[,])_board.Cells.Clone();
        int[,] zoneOfControlToSave = (int[,])_board.CheckCells.Clone();

        int situationCount = BoardHistory.Count(b => _board.Cells.Cast<int>().SequenceEqual(b.Board.Cast<int>()));
            
        GD.Print($"This situation has been repeated {situationCount} times");

        CheckReset();
        if (!promotion)
        {
            if (player / 10 == 1)
            {
                Piece.Turn = 2;
            } 
            else if (player / 10 == 2)
            {
                Piece.Turn = 1;
            }
            _camera.Zoom *= new Vector2(-1, -1);
            BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, Piece.Turn, true));
            GetTree().CallGroup("pieces", "ChangeTurn");
            EmitSignal(SignalName.TurnChanged, Piece.Turn, situationCount);
        }
        
        if (situationCount == 5)
        {
            Checkmate(0);
        }

        GetTree().CallGroup("players", "ChangeTurn", Piece.Turn);

        DebugTracking(); //DEBUG
    }

    public int MovementCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return _board.Cells[arrPos.X, arrPos.Y];
    }

    public void PlayersSet()
    {
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
                    }
                }
            }
        }

        GetTree().CallGroup("pieces", "SetInitialTurn");
        GetTree().CallGroup("pieces", "CheckMobility");
        GetTree().CallGroup("black_pieces", "UpdateCheck");

        int[,] boardToSave = new int[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];
        int[,] zoneOfControlToSave = new int[_board.CheckCells.GetLength(0), _board.CheckCells.GetLength(1)];

        for (int i = 0; i < _board.Cells.GetLength(0); i++)
        {
            for (int j = 0; j < _board.Cells.GetLength(1); j++)
            {
                boardToSave[i, j] = _board.Cells[i, j];
                zoneOfControlToSave[i, j] = _board.CheckCells[i, j];
            }
        }

        BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, Piece.Turn, true));

        _camera.Zoom = new Vector2(1, 1);
    }

    public void Capture(Vector2 capturePos, CharacterBody2D capture)
    {
        GetTree().CallGroup("pieces", "Capture", capturePos, capture);
    }

    public void CheckReset()
    {
        _board.CheckCells = new int[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];
        DebugTracking(); //DEBUG
    }

    public int CheckCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return _board.CheckCells[arrPos.X, arrPos.Y];
    }

    public void CheckFinished()
    {
        GetTree().CallGroup("pieces", "CheckCheckState");
    }

    public int CheckArrayCheck(Vector2 posCheck) //REWRITE
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
        _board.CheckCells = new int[_board.Cells.GetLength(0), _board.Cells.GetLength(1)];

        _board.Reset();

        GetTree().CallGroup("pieces", "ChangeTurn");
        GetTree().CallGroup("players", "ChangeTurn", Piece.Turn);

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
        Piece.Turn = BoardHistory[boardIndex].Turn;

        if (Piece.Turn == 1)
        {
            _camera.Zoom = new Vector2(1, 1);
        }
        else if (Piece.Turn == 2)
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
        int[,] zoneOfControlToSave = (int[,])_board.CheckCells.Clone();

        int situationCount = BoardHistory.Count(b => _board.Cells.Cast<int>().SequenceEqual(b.Board.Cast<int>()));

        if (player == 1)
        {
            Piece.Turn = 2;
        }
        else if (player == 2)
        {
            Piece.Turn = 1;
        }
        _camera.Zoom *= new Vector2(-1, -1);
        BoardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, Piece.Turn, true));
        GetTree().CallGroup("pieces", "ChangeTurn");
        EmitSignal(SignalName.TurnChanged, Piece.Turn, situationCount);
    } 
}