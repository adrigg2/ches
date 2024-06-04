using Godot;
using System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersGame : Node2D
{
    private Dictionary<int, CheckersPiece> _pieces;
    private int _turn;
    [Export] private CheckersBoard _board;

    public override void _Ready()
    {
        GD.Print("Game _Ready");
        _pieces = new Dictionary<int, CheckersPiece>();

        List<CheckersPiece> pieces = _board.GeneratePlayers();
        foreach (var piece in pieces)
        {
            _pieces.Add(piece.ID, piece);
        }
    }
}
