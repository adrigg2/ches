using Ches.Chess;
using Godot;
using System;
using System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersGame : Node2D
{
    private Dictionary<int, CheckersPiece> _pieces;
    private int _turn;
    [Export] private CheckersBoard _board;
    [Export] private Label _debugTracker;

    public override void _Ready()
    {
        GD.Print("Game _Ready");
        _pieces = new Dictionary<int, CheckersPiece>();
        _turn = 1;

        List<CheckersPiece> pieces = _board.GeneratePlayers();
        foreach (var piece in pieces)
        {
            piece.PieceSelected += DisableMovement;
            piece.SetInitialTurn(_turn);
            _pieces.Add(piece.ID, piece);
        }
    }

    public override void _Process(double delta)
    {
        DebugTracking();
    }

    public void DisableMovement()
    {
        foreach (Node moveOption in _board.GetChildren())
        {
            if (moveOption is CheckersMovement)
            {
                moveOption.QueueFree();
            }
        }
    }

    public void DebugTracking() //DEBUG
    {
        _debugTracker.Text = null;
        for (int i = 0; i < _board.Height; i++)
        {
            for (int j = 0; j < _board.Length; j++)
            {
                _debugTracker.Text += (_board[j, i] / 1000).ToString();
            }
            _debugTracker.Text += "\n";
        }
    }

    public CheckersPiece CheckPiece(int id)
    {
        return _pieces[id];
    }
}
