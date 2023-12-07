using Godot;

namespace Ches;
public partial class BoardState : Resource
{
    public int[,] ZoneOfControl { set; get; }
    public int[,] Board { get; set; }
    public bool Castling { get; set; }

    public BoardState(int[,] board, int[,] zoneOfControl, bool castling)
    {
        Board = board;
        Castling = castling;
        ZoneOfControl = zoneOfControl;
    }
}
