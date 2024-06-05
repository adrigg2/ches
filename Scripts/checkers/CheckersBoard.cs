using Godot;
using System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersBoard : TileMap
{
    private int[,] _squares;
    private int _length;
    private int _height;

    public int Length { get => _length; }
    public int Height { get => _height; }

    public int this[int i, int j] { get => _squares[i, j]; set => _squares[i, j] = value; }

    public override void _Ready()
    {
        GD.Print("Board _Ready");
        _length = 8;
        _height = 8;
        _squares = new int[_length, _height];
    }

    public List<CheckersPiece> GeneratePlayers()
    {
        List<CheckersPiece> pieces = new();
        for (int i = 1; i < 3; i++)
        {
            CheckersPlayer player = new(i);
            AddChild(player);
            pieces.AddRange(player.GeneratePieces(this));
        }
        return pieces;
    }
}
