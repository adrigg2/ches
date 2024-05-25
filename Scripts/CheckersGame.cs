using Godot;
using System;
using System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersGame : Node2D
{
    private Dictionary<int, CheckersPiece> _pieces;
    private int _turn;
    [Export]private CheckersBoard _board;

    public override void _Ready()
    {
        for (int i = 1; i < 3; i++)
        {
            CheckersPlayer player = new CheckersPlayer(i);
            List<CheckersPiece> pieces = player.GeneratePieces(_board);
            foreach (CheckersPiece piece in pieces)
            {
                _pieces.Add(piece.ID, piece);
            }
        }
    }
}
