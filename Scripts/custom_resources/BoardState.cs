using Godot;

namespace Ches;
public partial class BoardState : Resource
{
    public int[,] Board { get; set; }
    public bool Castling { get; set; }

    public BoardState(int[,] board, bool castling)
    {
        Board = board;
        Castling = castling;
    }
}
