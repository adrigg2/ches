using Godot;
using System;
using System.Net.NetworkInformation;

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

    Vector2[] oldPositions;
    PackedScene player;

    public override void _EnterTree()
    {
        Name = "Board";
    }

    public override void _Ready()
	{
        Node2D master = GetNode<Node2D>("..");
        Node2D master1 = GetNode<Node2D>("..");
        Callable master_ = new Callable(master, "BoardCellCount");
        Callable master1_ = new Callable(master1, "UpdateBoard");
        Callable master2_ = new Callable(master1, "PlayersSet");
        Connect("boardCellCount", master_);
        Connect("updateBoard", master1_);
        Connect("playersSet", master2_);

        EmitSignal(SignalName.boardCellCount, 8, 8);

        player = (PackedScene)ResourceLoader.Load("res://Scenes/player.tscn");

        for (int i = 1; i < 3; i++)
        {
            Node2D player_ = (Node2D)player.Instantiate();
            player_.SetMeta("player", i);
            AddChild(player_);
        }
        EmitSignal(SignalName.playersSet);
    }

    public void ConncectChildren()
    {
        foreach (Node player_ in GetChildren())
        {
            if (player_.HasMeta("player"))
            {
                foreach (Node piece_ in player_.GetChildren())
                {
                    if (piece_.HasMeta("Piece_Type"))
                    {
                        Callable piece = new Callable(piece_, "SetCheck");
                        Connect("setCheck", piece);
                        EmitSignal(SignalName.setCheck, 8, 8);
                    }
                }
            }
        }
    }

    public void UpdateTiles(Vector2 position, Vector2I cellAtlas)
    {
        Vector2I cellCoords = LocalToMap(position);
        GD.Print($"{cellCoords} {cellAtlas} update tiles");
        if (cellAtlas == new Vector2I(0, 0))
        {
            Godot.Collections.Array<Vector2I> neighboringCell = GetSurroundingCells(cellCoords);
            for (int i = 0; i < 4; i++)
            {
                Vector2I neighboringCellAtlas = GetCellAtlasCoords(0, neighboringCell[i]);
                if (neighboringCellAtlas == new Vector2I(0, 0))
                {
                    Vector2I newCellAtlas = new Vector2I(1, 0);
                    SetCell(0, cellCoords, 0, newCellAtlas);
                    break;
                }
                else if (neighboringCellAtlas == new Vector2I(1, 0))
                {
                    Vector2I newCellAtlas = new Vector2I(0, 0);
                    SetCell(0, cellCoords, 0, newCellAtlas);
                    break;
                }
            }
        }
        else
        {
            SetCell(0, cellCoords, 0, cellAtlas);
        }
    }

    public void StorePos(Vector2[] positions)
    {
        if (oldPositions != null)
        {
            foreach (Vector2 pos in oldPositions)
            {
                UpdateTiles(pos, new Vector2I(0, 0));
            }
        }
        oldPositions = positions;
    }
}
