using Godot;

namespace Ches;

public partial class Board : TileMap
{
    [Signal]
    public delegate void BoardCellCountEventHandler(int rows, int columns);

    [Signal]
    public delegate void PlayersSetEventHandler();

    [Signal]
    public delegate void TimersSetEventHandler(Timer timer, int player);

    private Player _player;
    private Vector2 _selectedPosition = new Vector2(-1, -1);

    public override void _Ready()
	{
        AddToGroup("to_save");

        Piece.Board = this;

        EmitSignal(SignalName.BoardCellCount, 8, 8);

        for (int i = 1; i < 3; i++)
        {
            _player = new Player();
            _player.SetMeta("player", i);
            _player.TimersSet += (timer, player) => EmitSignal(SignalName.TimersSet, timer, player);
            AddChild(_player);
        }
        EmitSignal(SignalName.PlayersSet);

        foreach (Node player in GetChildren())
        {
            if (player is Player)
            {
                foreach (Node piece in player.GetChildren())
                {
                    if (piece is Piece piece1)
                    {
                        piece1.UpdateTiles += UpdateTiles;
                        piece1.ClearDynamicTiles += ClearDynamicTiles;
                    }
                }
            }
        }
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
        Piece.Turn = 1;

        foreach(Node child in GetChildren())
        {
            if (child is Player player)
            {
                player.Reset();
            }
        }

        EmitSignal(SignalName.PlayersSet);

        ClearDynamicTiles();
    }
}
