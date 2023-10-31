using Godot;
using GodotPlugins.Game;
using System;

public partial class Player : Node2D
{
    [Signal]
    public delegate void updateBoardEventHandler();

    [Signal]
    public delegate void checkFinishedEventHandler();
    
    [Signal]
    public delegate void checkEventHandler();

    [Signal]
    public delegate void checkmateEventHandler();

    [Signal]
    public delegate void checkRookEventHandler();

    [Signal]
    public delegate void castlingAllowedEventHandler();

    [Signal]
    public delegate void finishCastlingEventHandler();

    int playerNum;
    PackedScene pawn;
    PackedScene rook;
    PackedScene knight;
    PackedScene bishop;
    PackedScene king;
    PackedScene queen;

    public override void _Ready()
	{
		playerNum = (int)GetMeta("player");

        GD.Print(playerNum, "player");

        Node2D master = GetNode<Node2D>("../..");
        Callable master_ = new Callable(master, "UpdateBoard");
        Callable master1 = new Callable(master, "CheckFinished");
        Callable master2 = new Callable(master, "Checkmate");
        Connect("updateBoard", master_);
        Connect("checkFinished", master1);
        Connect("checkmate", master2);

        pawn = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/pawn.tscn");
        rook = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/rook.tscn");
        knight = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/knight.tscn");
        bishop = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/bishop.tscn");
        king = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/king.tscn");
        queen = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/queen.tscn");

        if (playerNum == 1)
        {
            PlayerSet(6, 7);
        }
        else if (playerNum == 2)
        {
            PlayerSet(1, 0);
        }
    }
    public Vector2 SetPos(Vector2I tilepos)
    {
        TileMap board = GetNode<TileMap>("..");
        Vector2 fpos;
        fpos = board.MapToLocal(tilepos);
        GD.Print(fpos);
        return fpos;
    }

    public void PlayerSet(int firstRow, int secondRow)
    {
        for (int i = 0; i < 8; i++)
        {
            CharacterBody2D _pawn = (CharacterBody2D)pawn.Instantiate();
            GeneratePiece(_pawn, new Vector2I(0, firstRow), new Vector2I(1, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            CharacterBody2D _rook = (CharacterBody2D)rook.Instantiate();
            GeneratePiece(_rook, new Vector2I(0, secondRow), new Vector2I(7, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            CharacterBody2D _knight = (CharacterBody2D)knight.Instantiate();
            GeneratePiece(_knight, new Vector2I(1, secondRow), new Vector2I(5, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            CharacterBody2D _bishop = (CharacterBody2D)bishop.Instantiate();
            GeneratePiece(_bishop, new Vector2I(2, secondRow), new Vector2I(3, 0), i);
        }

        CharacterBody2D _king = (CharacterBody2D)king.Instantiate();
        GeneratePiece(_king, new Vector2I(4, secondRow), new Vector2I(0, 0));

        CharacterBody2D _queen = (CharacterBody2D)queen.Instantiate();
        GeneratePiece(_queen, new Vector2I(3, secondRow), new Vector2I(0, 0));
    }

    public void GeneratePiece(CharacterBody2D piece, Vector2I icell, Vector2I cells, int i = 0)
    {
        Vector2 ipos;
        Vector2I cell;

        piece.SetMeta("Player", playerNum);
        AddChild(piece);
        Callable pieceCallable = new Callable(piece, "SetCheck");
        Callable pieceCallable1 = new Callable(piece, "FirstMovementCheck");
        Callable pieceCallable2 = new Callable(piece, "Castling");
        Callable pieceCallable3 = new Callable(piece, "Castle");
        Connect("check", pieceCallable);
        Connect("checkRook", pieceCallable1);
        Connect("castlingAllowed", pieceCallable2);
        Connect("finishCastling", pieceCallable3);
        int id = (int)piece.Get("id");
        cell = icell + i * cells;
        ipos = SetPos(cell);
        EmitSignal(SignalName.updateBoard, ipos, new Vector2(128, 128), id, false);
        piece.Position = ipos;
    }

    public void CheckUpdate()
    {
        bool checkUpdated = true;
        foreach (Node piece in GetChildren())
        {
            if (piece.HasMeta("Piece_Type"))
            {
                if ((bool)piece.Get("checkUpdatedCheck") == false)
                {
                    checkUpdated = false;
                    break;
                }
            }
        }
        if (checkUpdated)
        {
            EmitSignal(SignalName.checkFinished);
        }
    }

    public void PlayerInCheck(bool isInCheck)
    {
        EmitSignal(SignalName.check, isInCheck);
    }

    public void CheckmateCheck()
    {
        bool checkmate = true;
        foreach (Node piece in GetChildren())
        {
            if (piece.HasMeta("Piece_Type"))
            {
                if ((bool)piece.Get("checkmate") == false)
                {
                    checkmate = false;
                    break;
                }
            }
        }
        if (checkmate)
        {
            EmitSignal(SignalName.checkmate, playerNum);
        }
    }

    public void CastlingSetup(Vector2 position)
    {
        EmitSignal(SignalName.checkRook, position);
    }

    public void AllowCastling(bool castlingAllowed, Vector2 position)
    {
        EmitSignal(SignalName.castlingAllowed, castlingAllowed, position);
    }

    public void Castle(Vector2 position)
    {
        GD.Print("Finish castling 1");
        TileMap board = GetNode<TileMap>("..");
        Vector2I cell;
        cell = board.LocalToMap(position);
        if (cell.X == 2)
        {
            Vector2 rookPosition = board.MapToLocal(new Vector2I(0, cell.Y));
            Vector2 newPosition = board.MapToLocal(new Vector2I(3, cell.Y));
            EmitSignal(SignalName.finishCastling, rookPosition, newPosition);
        }
        else if (cell.X == 6)
        {
            Vector2 rookPosition = board.MapToLocal(new Vector2I(7, cell.Y));
            Vector2 newPosition = board.MapToLocal(new Vector2I(5, cell.Y));
            EmitSignal(SignalName.finishCastling, rookPosition, newPosition);
        }
    }

    public void ConnectPromotedPiece(CharacterBody2D piece)
    {
        GD.Print($"Connecting {piece.Name} to player");
        Callable pieceCallable = new Callable(piece, "SetCheck");
        Callable pieceCallable1 = new Callable(piece, "FirstMovementCheck");
        Callable pieceCallable2 = new Callable(piece, "Castling");
        Callable pieceCallable3 = new Callable(piece, "Castle");
        Connect("check", pieceCallable);
        Connect("checkRook", pieceCallable1);
        Connect("castlingAllowed", pieceCallable2);
        Connect("finishCastling", pieceCallable3);
        GD.Print($"Finished connecting {piece.Name} to player");
    }
}