using Godot;
using System;
using System.Diagnostics.Metrics;
using System.IO.Enumeration;
using System.Xml;

public partial class ChessGame : Node2D
{
    [Signal]
    public delegate void destroyMovementEventHandler();

    [Signal]
    public delegate void moveCheckEventHandler();

    [Signal]
    public delegate void changeTurnEventHandler();

    [Signal]
    public delegate void setCaptureEventHandler();

    [Signal]
    public delegate void returnCheckEventHandler();

    [Signal]
    public delegate void checkCheckEventHandler();

    [Signal]
    public delegate void returnCheckArrayEventHandler();

    PackedScene board;

    int[,] boardCells;
    int[,] boardCellsCheck;

    public override void _Ready()
	{
        board = (PackedScene)ResourceLoader.Load("res://scenes/scenery/board.tscn");
        TileMap board_ = (TileMap)board.Instantiate();
        AddChild(board_);
        board_.Position = new Vector2(256, 64);

        Button button = GetNode<Button>("Button");
        button.Pressed += Reset;
    }

    public void DisableMovement()
    {
        GD.Print("another piece was selected");
        TileMap board_ = GetNode<TileMap>("Board");

        foreach (Node moveOption in board_.GetChildren())
        {
            if (moveOption.HasMeta("Is_Capture"))
            {
                Callable moveOption_ = new Callable(moveOption, "DestroyMovePos");
                Connect("destroyMovement", moveOption_);
            }
        }
        GD.Print("Loop ended");
        EmitSignal(SignalName.destroyMovement);
    }

    public void BoardCellCount(int rows, int columns)
    {
        boardCells = new int[rows, columns];
        boardCellsCheck = new int[rows, columns];
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                boardCells[i, j] = 0;
                boardCellsCheck[i, j] = 0;
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
        for (int i = 0; i < boardCells.GetLength(0); i++)
        {
            for (int j = 0; j < boardCells.GetLength(1); j++)
            {
                debugTracker.Text += (boardCells[j, i] / 10).ToString();
                debugTracker2.Text += Math.Abs(boardCellsCheck[j, i]).ToString();
            }
            debugTracker.Text += "\n";
            debugTracker2.Text += "\n";
        }
        GD.Print($"{boardCellsCheck[7, 4]} SEES (7, 4)");
    }

    public void UpdateBoard(Vector2 piecePos, Vector2 oldPos, int player, bool promotion)
    {
        Vector2I arrPos;
        Vector2I oldArrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        Camera2D camera = GetNode<Camera2D>("Camera2D");

        arrPos = board_.LocalToMap(piecePos);
        oldArrPos = board_.LocalToMap(oldPos);

        GD.Print(arrPos, "new");
        GD.Print(oldArrPos, "old");

        boardCells[arrPos.X, arrPos.Y] = player;
        boardCells[oldArrPos.X, oldArrPos.Y] = 0;

        CheckReset();
        if (!promotion)
        {
            if (player / 10 == 1)
            {
                camera.Zoom = new Vector2(-1, -1);
                EmitSignal(SignalName.changeTurn, 2);
            } 
            else if (player / 10 == 2)
            {
                camera.Zoom = new Vector2(1, 1);
                EmitSignal(SignalName.changeTurn, 1);
            }
        }

        DebugTracking(); //DEBUG
    }

    public void MovementCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        arrPos = board_.LocalToMap(posCheck);
        EmitSignal(SignalName.moveCheck, boardCells[arrPos.X, arrPos.Y]);
    }

    public void PlayersSet()
    {
        TileMap board = GetNode<TileMap>("Board");
        foreach (Node player_ in board.GetChildren())
        {
            if (player_.HasMeta("player"))
            {
                foreach (Node piece_ in player_.GetChildren())
                {
                    if (piece_.HasMeta("Piece_Type"))
                    {
                        GD.Print("Piece ", piece_.Name);
                        Callable piece = new Callable(piece_, "MovementCheck");
                        Callable piece1 = new Callable(piece_, "ChangeTurn");
                        Callable piece2 = new Callable(piece_, "Capture");
                        Callable piece3 = new Callable(piece_, "Check");
                        Callable piece4 = new Callable(piece_, "CheckCheckState");
                        Callable piece5 = new Callable(piece_, "CheckCheck");
                        Connect("moveCheck", piece);
                        Connect("changeTurn", piece1);
                        Connect("setCapture", piece2);
                        Connect("returnCheck", piece3);
                        Connect("checkCheck", piece4);
                        Connect("returnCheckArray", piece5);
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

        int cell = boardCellsCheck[arrPos.X, arrPos.Y];

        if (pieceCell)
        {
            if (checkSituation == SEES_ENEMY_KING)
            {
                if (cell == PROTECTED)
                {
                    boardCellsCheck[arrPos.X, arrPos.Y] = PROTECTED_AND_SEES;
                }
                else
                {
                    boardCellsCheck[arrPos.X, arrPos.Y] = NOT_PROTECTED_AND_SEES;
                }
            }
            else if (protectedPiece)
            {
                if (cell == NOT_PROTECTED_AND_SEES)
                {
                    boardCellsCheck[arrPos.X, arrPos.Y] = PROTECTED_AND_SEES;
                }
                else
                {
                    boardCellsCheck[arrPos.X, arrPos.Y] = PROTECTED;
                }
            }
            else
            {
                if (cell != PROTECTED && cell != PROTECTED_AND_SEES && cell != NOT_PROTECTED_AND_SEES)
                {
                    boardCellsCheck[arrPos.X, arrPos.Y] = NOT_PROTECTED;
                }
            }
        }
        else
        {
            if (cell == 0 && checkSituation != SEES_ENEMY_KING && checkSituation != KING_IN_CHECK)
            {
                boardCellsCheck[arrPos.X, arrPos.Y] = PATH;
            }
            else if (checkSituation == SEES_ENEMY_KING)
            {
                boardCellsCheck[arrPos.X, arrPos.Y] = SEES_ENEMY_KING;
            }
            else if (checkSituation == KING_IN_CHECK)
            {
                boardCellsCheck[arrPos.X, arrPos.Y] = KING_IN_CHECK;
            }
        }

        DebugTracking(); //DEBUG
    }

    public void CheckReset()
    {
        for (int i = 0; i < boardCells.GetLength(0); i++)
        {
            for (int j = 0; j < boardCells.GetLength(1); j++)
            {
                boardCellsCheck[i, j] = 0;
            }
        }
        DebugTracking(); //DEBUG
    }

    public void CheckCheck(Vector2 posCheck)
    {
        Vector2I arrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        arrPos = board_.LocalToMap(posCheck);
        GD.Print(arrPos, " check check");
        GD.Print(boardCellsCheck[arrPos.X, arrPos.Y], " check check");
        EmitSignal(SignalName.returnCheck, boardCellsCheck[arrPos.X, arrPos.Y]);
    }

    public void CheckFinished()
    {
        EmitSignal(SignalName.checkCheck);
    }

    public void CheckArrayCheck(Vector2 posCheck)
    {
        GD.Print("CheckArrayCheck in process");
        Vector2I arrPos;
        TileMap board_ = GetNode<TileMap>("Board");
        arrPos = board_.LocalToMap(posCheck);
        GD.Print("Check array ", boardCellsCheck[arrPos.X, arrPos.Y]);
        EmitSignal(SignalName.returnCheckArray, boardCellsCheck[arrPos.X, arrPos.Y]);
    }

    public void Checkmate(int looser)
    {
        Label winnerText = GetNode<Label>("EndGame");
        Label debug1 = GetNode<Label>("DebugTracker"); //DEBUG
        Label debug2 = GetNode<Label>("DebugTracker2"); //DEBUG
        Camera2D camera = GetNode<Camera2D>("Camera2D");
        Button button = GetNode<Button>("Button");

        camera.Zoom = new Vector2(1, 1);

        winnerText.Visible = true;
        winnerText.MoveToFront();
        if (looser == 1)
        {
            winnerText.Text = "Black Wins";
        } 
        else if (looser == 2)
        {
            winnerText.Text = "White Wins";
        }

        button.Position = new Vector2(316, 215);
        button.MoveToFront();

        debug1.Visible = false; //DEBUG
        debug2.Visible = false; //DEBUG
    }

    private void Reset()
    {
        TileMap ogBoard = GetNode<TileMap>("Board");
        TileMap board_ = (TileMap)board.Instantiate();
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
        Callable piece0 = new Callable(piece, "MovementCheck");
        Callable piece1 = new Callable(piece, "ChangeTurn");
        Callable piece2 = new Callable(piece, "Capture");
        Callable piece3 = new Callable(piece, "Check");
        Callable piece4 = new Callable(piece, "CheckCheckState");
        Callable piece5 = new Callable(piece, "CheckCheck");
        Connect("moveCheck", piece0);
        Connect("changeTurn", piece1);
        Connect("setCapture", piece2);
        Connect("returnCheck", piece3);
        Connect("checkCheck", piece4);
        Connect("returnCheckArray", piece5);
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
        for (int i = 0; i < boardCells.GetLength(0); i++)
        {
            for (int j = 0; j < boardCells.GetLength(1); j++)
            {
                if (boardCells[i, j] / 10 == -player)
                {
                    GD.Print("Cleared En Passant");
                    boardCells[i, j] = 0;
                }
            }
        }
    }
}