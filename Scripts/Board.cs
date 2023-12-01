using Godot;

namespace Ches;

public partial class Board : TileMap
{
    [Signal]
    public delegate void boardCellCountEventHandler();

    [Signal]
    public delegate void updateBoardEventHandler();

    [Signal]
    public delegate void setCheckEventHandler();

    [Signal]
    public delegate void playersSetEventHandler();

    private Player _player;
    private Vector2 _selectedPosition = new Vector2(-1, -1);

    public override void _Ready()
	{
        Node2D master = GetNode<Node2D>("..");
        Connect("boardCellCount", new Callable(master, "SetBoardArrays"));
        Connect("updateBoard", new Callable(master, "UpdateBoard"));
        Connect("playersSet", new Callable(master, "PlayersSet"));

        Piece.Board = this;

        EmitSignal(SignalName.boardCellCount, 8, 8);

        for (int i = 1; i < 3; i++)
        {
            _player = new Player();
            _player.SetMeta("player", i);
            AddChild(_player);
        }
        EmitSignal(SignalName.playersSet);
    }

    public void UpdateTiles(Vector2 position, Vector2I cellAtlas, string piece)
    {
        Vector2I cellCoords = LocalToMap(position);
        GD.Print($"{cellCoords} {cellAtlas} {piece} update tiles");
        if (cellAtlas == new Vector2I(0, 3))
        {
            if (_selectedPosition != new Vector2(-1,-1))
            {
                EraseCell(1, LocalToMap(_selectedPosition));
            }

            _selectedPosition = position;
            SetCell(1, cellCoords, 0, cellAtlas);
        }
        else
        {
            SetCell(1, cellCoords, 0, cellAtlas);
        }
    }

    public void ClearDynamicTiles()
    {
        GD.Print("Clearing update tiles");
        _selectedPosition = new Vector2(-1, -1);
        ClearLayer(1);
    }

    public void Reset()
    {
        foreach(Node child in GetChildren())
        {
            child.QueueFree();
        }

        ClearDynamicTiles();

        for (int i = 1; i < 3; i++)
        {
            _player = new Player();
            _player.SetMeta("player", i);
            AddChild(_player);
        }
        EmitSignal(SignalName.playersSet);
    }
}
