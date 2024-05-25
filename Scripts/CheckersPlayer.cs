using Ches.Chess;
using Godot;
using System;
using System.Collections.Generic;

namespace Ches.Checkers;
public partial class CheckersPlayer : Node2D
{
    private int _playerNum;
    private PackedScene _piece;
    private StringName _playerGroup;

    public CheckersPlayer(int playerNum)
    {
        _playerNum = playerNum;
        _piece = (PackedScene)ResourceLoader.Load("res://scenes/checkers_piece.tscn");
    }

    public List<CheckersPiece> GeneratePieces(CheckersBoard board)
    {
        List<CheckersPiece> pieces = new();
        if (_playerNum == 1)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = i - 1; j < 8; i += 2)
                {
                    if (j >= 0)
                    {
                        CheckersPiece piece = (CheckersPiece)_piece.Instantiate();
                        piece.SetFields(false, board);
                        piece.Position = board.MapToLocal(new Vector2I(j, i));
                        AddChild(piece);
                        pieces.Add(piece);
                    }
                }
            }
        }
        else
        {
            for (int i = 5; i < 8; i++)
            {
                for (int j = i - 7; j < 8; j+= 2)
                {
                    if (j >= 0)
                    {
                        CheckersPiece piece = (CheckersPiece)_piece.Instantiate();
                        piece.SetFields(false, board);
                        piece.Position = board.MapToLocal(new Vector2I(j, i));
                        AddChild(piece);
                        pieces.Add(piece);
                    }
                }
            }
        }

        return pieces;
    }
}
