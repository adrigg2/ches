using Godot;

namespace Ches;

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

    private int _playerNum;
    private PackedScene _pawn;
    private PackedScene _rook;
    private PackedScene _knight;
    private PackedScene _bishop;
    private PackedScene _king;
    private PackedScene _queen;

    public override void _Ready()
	{
		_playerNum = (int)GetMeta("player");

        GD.Print(_playerNum, "player");

        Node2D master = GetNode<Node2D>("../..");
        Connect("updateBoard", new Callable(master, "UpdateBoard"));
        Connect("checkFinished", new Callable(master, "CheckFinished"));
        Connect("checkmate", new Callable(master, "Checkmate"));

        _pawn = (PackedScene)ResourceLoader.Load("res://scenes/pieces/pawn.tscn");
        _rook = (PackedScene)ResourceLoader.Load("res://scenes/pieces/rook.tscn");
        _knight = (PackedScene)ResourceLoader.Load("res://scenes/pieces/knight.tscn");
        _bishop = (PackedScene)ResourceLoader.Load("res://scenes/pieces/bishop.tscn");
        _king = (PackedScene)ResourceLoader.Load("res://scenes/pieces/king.tscn");
        _queen = (PackedScene)ResourceLoader.Load("res://scenes/pieces/queen.tscn");

        if (_playerNum == 1)
        {
            PlayerSet(6, 7);
        }
        else if (_playerNum == 2)
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
            CharacterBody2D pawn = (CharacterBody2D)_pawn.Instantiate();
            GeneratePiece(pawn, new Vector2I(0, firstRow), new Vector2I(1, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            CharacterBody2D rook = (CharacterBody2D)_rook.Instantiate();
            GeneratePiece(rook, new Vector2I(0, secondRow), new Vector2I(7, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            CharacterBody2D knight = (CharacterBody2D)_knight.Instantiate();
            GeneratePiece(knight, new Vector2I(1, secondRow), new Vector2I(5, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            CharacterBody2D bishop = (CharacterBody2D)_bishop.Instantiate();
            GeneratePiece(bishop, new Vector2I(2, secondRow), new Vector2I(3, 0), i);
        }

        CharacterBody2D king = (CharacterBody2D)_king.Instantiate();
        GeneratePiece(king, new Vector2I(4, secondRow), new Vector2I(0, 0));

        CharacterBody2D queen = (CharacterBody2D)_queen.Instantiate();
        GeneratePiece(queen, new Vector2I(3, secondRow), new Vector2I(0, 0));
    }

    public void GeneratePiece(CharacterBody2D piece, Vector2I icell, Vector2I cells, int i = 0)
    {
        Vector2 ipos;
        Vector2I cell;

        piece.SetMeta("Player", _playerNum);
        AddChild(piece);
        Connect("check", new Callable(piece, "SetCheck"));
        Connect("checkRook", new Callable(piece, "FirstMovementCheck"));
        Connect("castlingAllowed", new Callable(piece, "Castling"));
        Connect("finishCastling", new Callable(piece, "Castle"));
        int id = (int)piece.Get("_id");
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
                if ((bool)piece.Get("_checkUpdatedCheck") == false)
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
                if ((bool)piece.Get("_checkmate") == false)
                {
                    checkmate = false;
                    break;
                }
            }
        }
        if (checkmate)
        {
            EmitSignal(SignalName.checkmate, _playerNum);
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
        Connect("check", new Callable(piece, "SetCheck"));
        Connect("checkRook", new Callable(piece, "FirstMovementCheck"));
        Connect("castlingAllowed", new Callable(piece, "Castling"));
        Connect("finishCastling", new Callable(piece, "Castle"));
        GD.Print($"Finished connecting {piece.Name} to player");
    }
}