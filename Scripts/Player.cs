using Godot;
using System.Collections.Generic;

namespace Ches.Chess;
public partial class Player : Node2D
{
    [Signal]
    public delegate void CheckFinishedEventHandler();

    [Signal]
    public delegate void CheckmateEventHandler(int looser);

    [Signal]
    public delegate void TimersSetEventHandler(Timer timer, int player);

    private int _playerNum;
    private PackedScene _piece;
    private StringName _playerGroup;
    private Timer _timer; // Nullable (?)
    private ChessGame _game;
    [Export] private double _timeLeft;

    private bool _check = false;

    public void SetFields(int playerNum, ChessGame game)
    {
        _playerNum = playerNum;
        _game = game;
        _piece = (PackedScene)ResourceLoader.Load("res://scenes/piece.tscn");
    }

    public override void _Ready()
    {
        AddToGroup("to_save");
        AddToGroup("players");

        if (_playerNum == 1)
        {
            _playerGroup = "white_pieces";
            PlayerSet(6, 7);
        }
        else if (_playerNum == 2)
        {
            _playerGroup = "black_pieces";
            PlayerSet(1, 0);
        }

        if (Main.Settings.Timer)
        {
            _timer = new Timer();
            _timer.Timeout += Timeout;
            AddChild(_timer);
            _timeLeft = Main.Settings.Minutes * 60;
            EmitSignal(SignalName.TimersSet, _timer, _playerNum);
            GD.Print("Player timer");
            if (_game.Turn == _playerNum)
            {
                _timer.Start(_timeLeft);
            }
        }

    }
    public Vector2 SetPos(Vector2I tilepos)
    {
        TileMap board = GetNode<TileMap>("..");
        Vector2 fpos;
        fpos = board.MapToLocal(tilepos);
        return fpos;
    }

    public void PlayerSet(int firstRow, int secondRow)
    {
        for (int i = 0; i < 8; i++)
        {
            PackedScene pawnPreset = (PackedScene)ResourceLoader.Load("res://scenes/pieces/pawn.tscn");
            Piece pawn = (Piece)pawnPreset.Instantiate();
            pawn.SetFields(_playerNum);
            GeneratePiece(pawn, new Vector2I(0, firstRow), new Vector2I(1, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            PackedScene rookPreset = (PackedScene)ResourceLoader.Load("res://scenes/pieces/rook.tscn");
            Piece rook = (Piece)rookPreset.Instantiate();
            rook.SetFields(_playerNum);
            GeneratePiece(rook, new Vector2I(0, secondRow), new Vector2I(7, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            PackedScene knightPreset = (PackedScene)ResourceLoader.Load("res://scenes/pieces/knight.tscn");
            Piece knight = (Piece)knightPreset.Instantiate();
            knight.SetFields(_playerNum);
            GeneratePiece(knight, new Vector2I(1, secondRow), new Vector2I(5, 0), i);
        }

        for (int i = 0; i < 2; i++)
        {
            PackedScene bishopPreset = (PackedScene)ResourceLoader.Load("res://scenes/pieces/bishop.tscn");
            Piece bishop = (Piece)bishopPreset.Instantiate();
            bishop.SetFields(_playerNum);
            GeneratePiece(bishop, new Vector2I(2, secondRow), new Vector2I(3, 0), i);
        }

        PackedScene kingPreset = (PackedScene)ResourceLoader.Load("res://scenes/pieces/king.tscn");
        Piece king = (Piece)kingPreset.Instantiate();
        king.SetFields(_playerNum);
        GeneratePiece(king, new Vector2I(4, secondRow), new Vector2I(0, 0));

        PackedScene queenPreset = (PackedScene)ResourceLoader.Load("res://scenes/pieces/queen.tscn");
        Piece queen = (Piece)queenPreset.Instantiate();
        queen.SetFields(_playerNum);
        GeneratePiece(queen, new Vector2I(3, secondRow), new Vector2I(0, 0));
    }

    public void GeneratePiece(Piece piece, Vector2I isquare, Vector2I squares, int index = 0)
    {
        Vector2 ipos;
        Vector2I square;

        AddChild(piece);

        piece.PlayerInCheck += PlayerInCheck;

        square = isquare + index * squares;
        ipos = SetPos(square);
        piece.Position = ipos;
    }

    public void PlayerInCheck(bool isInCheck)
    {
        _check = isInCheck;
        GetTree().CallGroup(_playerGroup, "SetCheck", isInCheck);
        CheckmateCheck();
    }

    public void CheckmateCheck()
    {
        GD.PrintRich("[color=red]Checkmate Check[/color]");
        foreach (Node child in GetChildren())
        {
            if (child is Piece piece)
            {
                if (!piece.CheckUnmovable())
                {
                    GD.PrintRich($"[color=green]Checkmate Check movable {piece}[/color]");
                    return;
                }
            }
        }

        if (_check)
        {
            EmitSignal(SignalName.Checkmate, _playerNum);
        }
        else
        {
            EmitSignal(SignalName.Checkmate, 0);
        }
    }

    // FIXME: use SetFields when respawning old pieces
    public void RevertPieces(int[,] newSituation)
    {
        int squareSituation;
        Vector2I position;

        foreach (var child in GetChildren())
        {
            if (child is Piece piece)
            {
                piece.Delete();
            }
        }

        for (int i = 0; i < newSituation.GetLength(0); i++)
        {
            for (int j = 0; j < newSituation.GetLength(1); j++)
            {
                squareSituation = newSituation[i, j];
                if (squareSituation > 0 && squareSituation / 10 == _playerNum)
                {
                    Piece piece = (Piece)_piece.Instantiate();
                    position = new Vector2I(i, j);
                    GeneratePiece(piece, position, new Vector2I(0, 0));
                }
            }
        }
    }

    private void Timeout()
    {
        EmitSignal(SignalName.Checkmate, _playerNum);
    }

    public void ChangeTurn(int turn)
    {
        if (Main.Settings.Timer)
        {
            if (turn == _playerNum)
            {
                _timer.Start(_timeLeft);
            }
            else
            {
                _timeLeft = _timer.TimeLeft + Main.Settings.Seconds;
                _timer.Stop();
            }
        }
    }

    public void Reset()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Piece piece)
            {
                piece.Delete();
            }
        }

        if (_playerNum == 1)
        {
            PlayerSet(6, 7);
        }
        else if (_playerNum == 2)
        {
            PlayerSet(1, 0);
        }

        _timeLeft = Main.Settings.Minutes * 60;
    }
}