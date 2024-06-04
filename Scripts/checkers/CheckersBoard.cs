using Godot;

namespace Ches.Checkers;
public partial class CheckersBoard : TileMap
{
    private int[,] _squares;

    public int this[int i, int j] { get => _squares[i, j]; set => _squares[i, j] = value; }

    public override void _Ready()
    {
        _squares = new int[8, 8];
    }
}
