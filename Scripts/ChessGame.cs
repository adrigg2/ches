using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches;
public partial class ChessGame : Node2D
{
    [Export] private Board _board;
    [Export] private Button _restart;
    [Export] private Button _draw;
    [Export] private Button _revert;
    [Export] private Button _reject;
    [Export] private Label _debugTracker;
    [Export] private Label _debugTracker2;
    [Export] private Label _endGame;
    [Export] private Camera2D _camera;
    [Export] private RevertMenu _revertMenu;
    [Export] private Control _ui;

    private List<BoardState> _boardHistory = new();

    public override void _EnterTree()
	{
        _restart.Pressed += Reset;
        _draw.Pressed += AgreedDraw;
        _revert.Pressed += Revert;
        _reject.Pressed += Reject;
        _revertMenu.PreviousBoardSelected += RevertGameStatus;
        _board.BoardCellCount += SetBoardArrays;
        _board.PlayersSet += PlayersSet;
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
        Piece.BoardCells = new int[rows, columns];
        Piece.BoardCellsCheck = new int[rows, columns];
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                Piece.BoardCells[i, j] = 0;
                Piece.BoardCellsCheck[i, j] = 0;
            }
        }
        DebugTracking(); //DEBUG
    }

    public void DebugTracking() //DEBUG
    {
        _debugTracker.Text = null;
        _debugTracker2.Text = null;
        for (int i = 0; i < Piece.BoardCells.GetLength(0); i++)
        {
            for (int j = 0; j < Piece.BoardCells.GetLength(1); j++)
            {
                _debugTracker.Text += (Piece.BoardCells[j, i] / 10).ToString();
                _debugTracker2.Text += Math.Abs(Piece.BoardCellsCheck[j, i]).ToString();
            }
            _debugTracker.Text += "\n";
            _debugTracker2.Text += "\n";
        }
    }

    public void UpdateBoard(Vector2 piecePos, Vector2 oldPos, int player, bool promotion)
    {
        Vector2I arrPos;
        Vector2I oldArrPos;
        int situationCount;

        arrPos = _board.LocalToMap(piecePos);
        oldArrPos = _board.LocalToMap(oldPos);

        Piece.BoardCells[arrPos.X, arrPos.Y] = player;
        Piece.BoardCells[oldArrPos.X, oldArrPos.Y] = 0;

        int[,] boardToSave = new int[Piece.BoardCells.GetLength(0), Piece.BoardCells.GetLength(1)];
        int[,] zoneOfControlToSave = new int[Piece.BoardCellsCheck.GetLength(0), Piece.BoardCellsCheck.GetLength(1)];

        for (int i = 0; i < Piece.BoardCells.GetLength(0); i++)
        {
            for (int j = 0; j < Piece.BoardCells.GetLength(1); j++)
            {
                boardToSave[i, j] = Piece.BoardCells[i, j];
                zoneOfControlToSave[i, j] = Piece.BoardCellsCheck[i, j];
            }
        }

        _boardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, true));

        situationCount = _boardHistory.Count(b => Piece.BoardCells.Cast<int>().SequenceEqual(b.Board.Cast<int>()));           
            
        GD.Print($"This situation has been repeated {situationCount} times");

        CheckReset();
        if (!promotion)
        {
            if (player / 10 == 1)
            {
                _camera.Zoom *= new Vector2(-1, -1);
                _ui.Scale = new Vector2(-1, -1);
                _ui.Position = new Vector2(768, 384);
                _revertMenu.Camera.Zoom *= new Vector2(-1, -1);
                Piece.Turn = 2;
                GetTree().CallGroup("pieces", "ChangeTurn");
            } 
            else if (player / 10 == 2)
            {
                _camera.Zoom *= new Vector2(-1, -1);
                _ui.Scale = new Vector2(1, 1);
                _ui.Position = new Vector2(0, 0);
                _revertMenu.Camera.Zoom *= new Vector2(-1, -1);
                Piece.Turn = 1;
                GetTree().CallGroup("pieces", "ChangeTurn");
            }
        }

        if (situationCount >= 3 && situationCount < 5)
        {
            _endGame.Text = "Draw by repetition?";
            _draw.Position = new Vector2(316, 215);
            _restart.Visible = false;
            _revert.Visible = false;
            _reject.Visible = true;
            _endGame.Visible = true;
        }
        else if (situationCount == 5)
        {
            Checkmate(0);
        }

        DebugTracking(); //DEBUG
    }

    public int MovementCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return Piece.BoardCells[arrPos.X, arrPos.Y];
    }

    public void PlayersSet()
    {
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
                        piece.ZoneOfControlChecked += Check;
                        piece.ClearEnPassant += ClearEnPassant;
                    }
                }
            }
        }

        GetTree().CallGroup("pieces", "SetInitialTurn");
        GetTree().CallGroup("black_pieces", "UpdateCheck");

        int[,] boardToSave = new int[Piece.BoardCells.GetLength(0), Piece.BoardCells.GetLength(1)];
        int[,] zoneOfControlToSave = new int[Piece.BoardCellsCheck.GetLength(0), Piece.BoardCellsCheck.GetLength(1)];

        for (int i = 0; i < Piece.BoardCells.GetLength(0); i++)
        {
            for (int j = 0; j < Piece.BoardCells.GetLength(1); j++)
            {
                boardToSave[i, j] = Piece.BoardCells[i, j];
                zoneOfControlToSave[i, j] = Piece.BoardCellsCheck[i, j];
            }
        }

        _boardHistory.Add(new BoardState(boardToSave, zoneOfControlToSave, true));
    }

    public void Capture(Vector2 capturePos, CharacterBody2D capture)
    {
        GetTree().CallGroup("pieces", "Capture", capturePos, capture);
    }

    public void Check(Vector2I arrPos, int checkSituation, bool pieceCell, bool protectedPiece)
    {
        const int Path = 2;
        const int Protected = 3;
        const int SeesEnemyKing = 4;
        const int ProtectedAndSees = 5;
        const int NotProtectedAndSees = 6;
        const int NotProtected = 7;
        const int KingInCheck = 8;

        int cell = Piece.BoardCellsCheck[arrPos.X, arrPos.Y];

        if (pieceCell)
        {
            if (checkSituation == SeesEnemyKing)
            {
                if (cell == Protected)
                {
                    Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = ProtectedAndSees;
                }
                else
                {
                    Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = NotProtectedAndSees;
                }
            }
            else if (protectedPiece)
            {
                if (cell == NotProtectedAndSees)
                {
                    Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = ProtectedAndSees;
                }
                else
                {
                    Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = Protected;
                }
            }
            else
            {
                if (cell != Protected && cell != ProtectedAndSees && cell != NotProtectedAndSees)
                {
                    Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = NotProtected;
                }
            }
        }
        else
        {
            if (cell == 0 && checkSituation != SeesEnemyKing && checkSituation != KingInCheck)
            {
                Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = Path;
            }
            else if (checkSituation == SeesEnemyKing)
            {
                Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = SeesEnemyKing;
            }
            else if (checkSituation == KingInCheck)
            {
                Piece.BoardCellsCheck[arrPos.X, arrPos.Y] = KingInCheck;
            }
        }

        DebugTracking(); //DEBUG
    }

    public void CheckReset()
    {
        for (int i = 0; i < Piece.BoardCellsCheck.GetLength(0); i++)
        {
            for (int j = 0; j < Piece.BoardCellsCheck.GetLength(1); j++)
            {
                Piece.BoardCellsCheck[i, j] = 0;
            }
        }
        DebugTracking(); //DEBUG
    }

    public int CheckCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return Piece.BoardCellsCheck[arrPos.X, arrPos.Y];
    }

    public void CheckFinished()
    {
        GetTree().CallGroup("pieces", "CheckCheckState");
    }

    public int CheckArrayCheck(Vector2 posCheck) //REWRITE
    {
        Vector2I arrPos;
        arrPos = _board.LocalToMap(posCheck);
        return Piece.BoardCellsCheck[arrPos.X, arrPos.Y];
    }

    public void Checkmate(int looser)
    {
        _endGame.Visible = true;
        _endGame.MoveToFront();
        _restart.MoveToFront();
        _draw.Visible = false;
        _revert.Visible = false;
        _reject.Visible = false;
        _restart.Visible = true;

        _endGame.Position = new Vector2(0, 0);
        _endGame.Scale = new Vector2(1, 1);
        _restart.Position = new Vector2(316, 215);
        _restart.Scale = new Vector2(1, 1);

        if (looser == 1)
        {
            _endGame.Text = Tr("BLACK");
        }
        else if (looser == 2)
        {
            _endGame.Text = Tr("WHITE");
        }
        else if (looser == 0)
        {
            _endGame.Text = "Draw";
        }

        _debugTracker.Visible = false; //DEBUG
        _debugTracker2.Visible = false; //DEBUG
    }

    private void Reset()
    {
        for (int i = 0; i < Piece.BoardCellsCheck.GetLength(0); i++)
        {
            for (int j = 0; j < Piece.BoardCellsCheck.GetLength(1); j++)
            {
                Piece.BoardCellsCheck[i, j] = 0;
                Piece.BoardCells[i, j] = 0;
            }
        }

        _board.Reset();

        _endGame.Visible = false;

        _restart.Position = new Vector2(20, 293);
        _draw.Position = new Vector2(20, 220);
        _revert.Position = new Vector2(20, 147);
        _draw.Visible = true;
        _revert.Visible = true;
        _reject.Visible = false;

        _debugTracker.Visible = true; //DEBUG
        _debugTracker2.Visible = true; //DEBUG
    }

    public void ClearEnPassant(int player)
    {
        for (int i = 0; i < Piece.BoardCells.GetLength(0); i++)
        {
            for (int j = 0; j < Piece.BoardCells.GetLength(1); j++)
            {
                if (Piece.BoardCells[i, j] / 10 == -player)
                {
                    GD.Print("Cleared En Passant");
                    Piece.BoardCells[i, j] = 0;
                }
            }
        }
    }

    public void AgreedDraw()
    {
        Checkmate(0);
    }

    public void Revert()
    {
        _revertMenu.Visible = true;
        _revertMenu.BoardHistory = _boardHistory;
        _revertMenu.SetUp();
    }

    public void RevertGameStatus(int boardIndex)
    {
        int[,] board = _boardHistory[boardIndex].Board;
        
        int lastBoardHistory = _boardHistory.Count - 1;

        while (lastBoardHistory > boardIndex)
        {
            _boardHistory.RemoveAt(lastBoardHistory);
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
        Piece.BoardCells = _boardHistory[boardIndex].Board;
        Piece.BoardCellsCheck = _boardHistory[boardIndex].ZoneOfControl;

        PlayersSet();
    }

    public void PromotionComplete(Piece piece, int player)
    {
        piece.PieceSelected += DisableMovement;
        piece.PieceMoved += UpdateBoard;
        piece.ZoneOfControlChecked += Check;
        piece.ClearEnPassant += ClearEnPassant;

        if (player == 1)
        {
            _camera.Zoom *= new Vector2(-1, -1);
            _ui.Scale = new Vector2(-1, -1);
            _ui.Position = new Vector2(768, 384);
            _revertMenu.Camera.Zoom *= new Vector2(-1, -1);
            Piece.Turn = 2;
            GetTree().CallGroup("pieces", "ChangeTurn");
        }
        else if (player == 2)
        {
            _camera.Zoom *= new Vector2(-1, -1);
            _ui.Scale = new Vector2(1, 1);
            _ui.Position = new Vector2(0, 0);
            _revertMenu.Camera.Zoom *= new Vector2(-1, -1);
            Piece.Turn = 1;
            GetTree().CallGroup("pieces", "ChangeTurn");
        }
    }

    public void Reject()
    {
        _reject.Visible = false;
        _draw.Position = new Vector2(20, 220);
        _endGame.Visible = false;
    }
}