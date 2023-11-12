using Godot;

namespace Chess;

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

    private PackedScene _player;
    private Vector2 _selectedPosition = new Vector2(-1, -1);

    public override void _EnterTree()
    {
        Name = "Board";
    }

    public override void _Ready()
	{
        Node2D master = GetNode<Node2D>("..");
        Connect("boardCellCount", new Callable(master, "BoardCellCount"));
        Connect("updateBoard", new Callable(master, "UpdateBoard"));
        Connect("playersSet", new Callable(master, "PlayersSet"));

        EmitSignal(SignalName.boardCellCount, 8, 8);

        _player = (PackedScene)ResourceLoader.Load("res://scenes/player.tscn");

        for (int i = 1; i < 3; i++)
        {
            Node2D player_ = (Node2D)_player.Instantiate();
            player_.SetMeta("player", i);
            AddChild(player_);
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
        ClearLayer(1);
    }
}
