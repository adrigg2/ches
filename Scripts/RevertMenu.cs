using Ches;
using Godot;
using System.Collections.Generic;

public partial class RevertMenu : Panel
{
	[Signal]
	public delegate void PreviousBoardSelectedEventHandler(int index);

	[Export] private TileMap _board;
	[Export] private Button _buttonRight;
	[Export] private Button _buttonLeft;
	[Export] private Button _return;
	[Export] private Button _revert;
	[Export] private PackedScene _piece;
	[Export] public Camera2D Camera { get; set; }

	private List<BoardState> _boardHistory = ChessGame.BoardHistory;

	private Dictionary<int, string> _pieceDict = new();

	private int _boardHistoryIndex;

	public int Turn { get; set; }

    public override void _Ready()
	{
		_buttonRight.Pressed += Right;
		_buttonLeft.Pressed += Left;
		_return.Pressed += Return;
		_revert.Pressed += Revert;

		_pieceDict.Add(0, "pawn");
		_pieceDict.Add(1, "king");
		_pieceDict.Add(2, "queen");
		_pieceDict.Add(3, "rook");
		_pieceDict.Add(4, "bishop");
		_pieceDict.Add(5, "knight");

		Turn = Piece.Turn;
	}

	public void SetUp()
	{
		_boardHistoryIndex = _boardHistory.Count - 1;
		_buttonRight.Disabled = true;
		
		if (_boardHistoryIndex == 0)
		{
			_buttonLeft.Disabled = true;
		}
		else
		{
			_buttonLeft.Disabled = false;
		}

        SetBoard();
    }

	private void Return()
	{
		Visible = false;
		foreach (Node piece in _board.GetChildren())
		{
			piece.QueueFree();
		}
	}

	private void Right()
	{
		_boardHistoryIndex++;
        _buttonLeft.Disabled = false;
		if (_boardHistoryIndex == _boardHistory.Count - 1)
		{
			_buttonRight.Disabled = true;
		}
        SetBoard();
	}

	private void Left()
	{
		_boardHistoryIndex--;
		_buttonRight.Disabled = false;
		if (_boardHistoryIndex == 0)
		{
			_buttonLeft.Disabled = true;
		}
        SetBoard();
    }

	private void SetBoard()
	{
		int[,] board = _boardHistory[_boardHistoryIndex].Board;
		int cellSituation;

		foreach (var piece in _board.GetChildren())
		{
			piece.QueueFree();
		}

		for (int i = 0; i < board.GetLength(0); i++)
		{
			for (int j = 0; j < board.GetLength(1); j++)
			{
				cellSituation = board[i, j];
				if (cellSituation > 0)
				{
					Piece piece = (Piece)_piece.Instantiate();

					if (Turn == 2)
					{
						piece.Scale = new Vector2(-1, -1);
					}

					piece.SetMeta("Player", cellSituation / 10);
					piece.SetMeta("Piece_Type", _pieceDict[cellSituation % 10]);
					_board.AddChild(piece);
					piece.Position = _board.MapToLocal(new Vector2I(i, j));
					piece.SetScript(new Variant());
				}
			}
		}
	}

	private void Revert()
	{
		EmitSignal(SignalName.PreviousBoardSelected, _boardHistoryIndex);
	}
}
