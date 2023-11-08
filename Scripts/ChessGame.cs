using Godot;
using System;
using System.Diagnostics.Metrics;
using System.IO.Enumeration;
using System.Xml;

namespace Chess;

public partial class ChessGame : Node2D
{
    [Signal]
    public delegate void destroyMovementEventHandler();

    [Signal]
    public delegate void changeTurnEventHandler();

    [Signal]
    public delegate void setCaptureEventHandler();

    [Signal]
    public delegate void checkCheckEventHandler();

    private PackedScene _board;

    private int[,] _boardCells;
    private int[,] _boardCellsCheck;

    public override void _Ready()
	{
        _board = (PackedScene)ResourceLoader.Load("res://scenes/scenery/board.tscn");
        TileMap board = (TileMap)_board.Instantiate();
        AddChild(board);
        board.Position = new Vector2(256, 64);

        Button button = GetNode<Button>("Button");
        button.Pressed += Reset;

        Piece.MovementCheck = new Callable(this, "MovementCheck");
        Piece.CheckCheck = new Callable(this, "CheckCheck");
        Piece.CheckArrayCheck = new Callable(this, "CheckArrayCheck");
    }

    public void DisableMovement()
    {
        GD.Print("another piece was selected");
        TileMap board_ = GetNode<TileMap>("Board");

        foreach (Node moveOption in board_.GetChildren())
        {
            if (moveOption.HasMeta("Is_Capture"))
            {
                Connect("destroyMovement", new Callable(moveOption, "DestroyMovePos"));
            }
        }
        GD.Print("Loop ended");
        EmitSignal(SignalName.destroyMovement);
    }

    public void BoardCellCount(int rows, int columns)
    {
        _boardCells = new int[rows, columns];
        _boardCellsCheck = new int[rows, columns];
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                _boardCells[i, j] = 0;
                _boardCellsCheck[i, j] = 0;
            }
        }
        DebugTracking(); //DEBUG
    }

    public void DebugTracking() //DEBUG
    {
        Label debugTracker = GetNode<Label>("DebugTracker"); //DEBUG
        Label debugTracker2 = GetNode<Label>("DebugTracker2"); //DEBUG
        debugTracker.Text = null;
        debugTracker2.Text = null;
        for (int i = 0; i < _boardCells.GetLength(0); i++)
        {
            for (int j = 0; j < _boardCells.GetLength(1); j++)
            {
                debugTracker.Text += (_boardCells[j, i] / 10).ToString();
                debugTracker2.Text += Math.Abs(_boardCellsCheck[j, i]).ToString();
            }
            debugTracker.Text += "\n";
            debugTracker2.Text += "\n";
        }
        GD.Print($"{_boardCellsCheck[7, 4]} SEES (7, 4)");
    }

    public void UpdateBoard(Vector2 piecePos, Vector2 oldPos, int player, bool promotion)
    {
        Vector2I arrPos;
        Vector2I oldArrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        Camera2D camera = GetNode<Camera2D>("Camera2D");
        Button reset = GetNode<Button>("Button");

        arrPos = board_.LocalToMap(piecePos);
        oldArrPos = board_.LocalToMap(oldPos);

        GD.Print(arrPos, "new");
        GD.Print(oldArrPos, "old");

        _boardCells[arrPos.X, arrPos.Y] = player;
        _boardCells[oldArrPos.X, oldArrPos.Y] = 0;

        CheckReset();
        if (!promotion)
        {
            if (player / 10 == 1)
            {
                camera.Zoom = new Vector2(-1, -1);
                reset.Scale = new Vector2(-1, -1);
                reset.Position = new Vector2(748, 91);
                EmitSignal(SignalName.changeTurn, 2);
            } 
            else if (player / 10 == 2)
            {
                camera.Zoom = new Vector2(1, 1);                
                reset.Scale = new Vector2(1, 1);
                reset.Position = new Vector2(20, 293);
                EmitSignal(SignalName.changeTurn, 1);
            }
        }

        DebugTracking(); //DEBUG
    }

    public int MovementCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        arrPos = board_.LocalToMap(posCheck);
        return _boardCells[arrPos.X, arrPos.Y];
    }

    public void PlayersSet()
    {
        TileMap board = GetNode<TileMap>("Board");

        foreach (Node player_ in board.GetChildren())
        {
            if (player_.HasMeta("player"))
            {
                foreach (Node piece in player_.GetChildren())
                {
                    if (piece.HasMeta("Piece_Type"))
                    {
                        GD.Print("Piece ", piece.Name);
                        Connect("changeTurn", new Callable(piece, "ChangeTurn"));
                        Connect("setCapture", new Callable(piece, "Capture"));
                        Connect("checkCheck", new Callable(piece, "CheckCheckState"));
                    }
                }
            }
        }
    }

    public void Capture(Vector2 capturePos, CharacterBody2D capture)
    {
        EmitSignal(SignalName.setCapture, capturePos, capture);
    }

    public void Check(Vector2I arrPos, int checkSituation, bool pieceCell, bool protectedPiece)
    {
        const int PATH = 2;
        const int PROTECTED = 3;
        const int SEES_ENEMY_KING = 4;
        const int PROTECTED_AND_SEES = 5;
        const int NOT_PROTECTED_AND_SEES = 6;
        const int NOT_PROTECTED = 7;
        const int KING_IN_CHECK = 8;

        int cell = _boardCellsCheck[arrPos.X, arrPos.Y];

        if (pieceCell)
        {
            if (checkSituation == SEES_ENEMY_KING)
            {
                if (cell == PROTECTED)
                {
                    _boardCellsCheck[arrPos.X, arrPos.Y] = PROTECTED_AND_SEES;
                }
                else
                {
                    _boardCellsCheck[arrPos.X, arrPos.Y] = NOT_PROTECTED_AND_SEES;
                }
            }
            else if (protectedPiece)
            {
                if (cell == NOT_PROTECTED_AND_SEES)
                {
                    _boardCellsCheck[arrPos.X, arrPos.Y] = PROTECTED_AND_SEES;
                }
                else
                {
                    _boardCellsCheck[arrPos.X, arrPos.Y] = PROTECTED;
                }
            }
            else
            {
                if (cell != PROTECTED && cell != PROTECTED_AND_SEES && cell != NOT_PROTECTED_AND_SEES)
                {
                    _boardCellsCheck[arrPos.X, arrPos.Y] = NOT_PROTECTED;
                }
            }
        }
        else
        {
            if (cell == 0 && checkSituation != SEES_ENEMY_KING && checkSituation != KING_IN_CHECK)
            {
                _boardCellsCheck[arrPos.X, arrPos.Y] = PATH;
            }
            else if (checkSituation == SEES_ENEMY_KING)
            {
                _boardCellsCheck[arrPos.X, arrPos.Y] = SEES_ENEMY_KING;
            }
            else if (checkSituation == KING_IN_CHECK)
            {
                _boardCellsCheck[arrPos.X, arrPos.Y] = KING_IN_CHECK;
            }
        }

        DebugTracking(); //DEBUG
    }

    public void CheckReset()
    {
        for (int i = 0; i < _boardCells.GetLength(0); i++)
        {
            for (int j = 0; j < _boardCells.GetLength(1); j++)
            {
                _boardCellsCheck[i, j] = 0;
            }
        }
        DebugTracking(); //DEBUG
    }

    public int CheckCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        arrPos = board_.LocalToMap(posCheck);
        GD.Print(arrPos, " check check");
        GD.Print(_boardCellsCheck[arrPos.X, arrPos.Y], " check check");
        return _boardCellsCheck[arrPos.X, arrPos.Y];
    }

    public void CheckFinished()
    {
        EmitSignal(SignalName.checkCheck);
    }

    public int CheckArrayCheck(Vector2 posCheck) //REWRITE
    {
        GD.Print("CheckArrayCheck in process");
        Vector2I arrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        arrPos = board_.LocalToMap(posCheck);
        GD.Print("Check array ", _boardCellsCheck[arrPos.X, arrPos.Y]);
        return _boardCellsCheck[arrPos.X, arrPos.Y];
    }

    public void Checkmate(int looser)
    {
        Label winnerText = GetNode<Label>("EndGame");
        Label debug1 = GetNode<Label>("DebugTracker"); //DEBUG
        Label debug2 = GetNode<Label>("DebugTracker2"); //DEBUG
        Button button = GetNode<Button>("Button");

        winnerText.Visible = true;
        winnerText.MoveToFront();
        button.MoveToFront();
        if (looser == 1)
        {
            winnerText.Text = Tr("BLACK");
            winnerText.Position = new Vector2(0, 0);
            winnerText.Scale = new Vector2(1, 1);
            button.Position = new Vector2(316, 215);
            button.Scale = new Vector2(1, 1);
        }
        else if (looser == 2)
        {
            winnerText.Text = Tr("WHITE");
            winnerText.Position = new Vector2(768, 384);
            winnerText.Scale = new Vector2(-1, -1);
            button.Position = new Vector2(452, 169);
            button.Scale = new Vector2(-1, -1);
        }

        debug1.Visible = false; //DEBUG
        debug2.Visible = false; //DEBUG
    }

    private void Reset()
    {
        TileMap ogBoard = GetNode<TileMap>("Board");
        TileMap board_ = (TileMap)_board.Instantiate();
        Label winnerText = GetNode<Label>("EndGame");
        Label debug1 = GetNode<Label>("DebugTracker"); //DEBUG
        Label debug2 = GetNode<Label>("DebugTracker2"); //DEBUG
        Button button = GetNode<Button>("Button");

        ogBoard.Name = "oldBoard";
        ogBoard.QueueFree();

        board_.Name = "Board";
        AddChild(board_);
        board_.Position = new Vector2(256, 64);

        winnerText.Visible = false;

        button.Position = new Vector2(20, 293);

        debug1.Visible = true; //DEBUG
        debug2.Visible = true; //DEBUG
    }

    public void ConnectPromotedPiece(CharacterBody2D piece, int player)
    {
        Camera2D camera = GetNode<Camera2D>("Camera2D");

        GD.Print($"Connecting {piece.Name} to master");
        Connect("changeTurn", new Callable(piece, "ChangeTurn"));
        Connect("setCapture", new Callable(piece, "Capture"));
        Connect("checkCheck", new Callable(piece, "CheckCheckState"));
        GD.Print($"Finished connecting {piece.Name} to master");

        if (player == 1)
        {
            camera.Zoom = new Vector2(-1, -1);
            EmitSignal(SignalName.changeTurn, 2);
        }
        else if (player == 2)
        {
            camera.Zoom = new Vector2(1, 1);
            EmitSignal(SignalName.changeTurn, 1);
        }
    }

    public void ClearEnPassant(int player)
    {
        GD.Print("Clearing En Passant");
        for (int i = 0; i < _boardCells.GetLength(0); i++)
        {
            for (int j = 0; j < _boardCells.GetLength(1); j++)
            {
                if (_boardCells[i, j] / 10 == -player)
                {
                    GD.Print("Cleared En Passant");
                    _boardCells[i, j] = 0;
                }
            }
        }
    }
}