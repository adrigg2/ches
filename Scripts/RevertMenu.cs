using Ches;
using Godot;
using System.Collections.Generic;

public partial class RevertMenu : Panel
{
	[Signal]
	public delegate void previousBoardSelectedEventHandler(int index);

	[Export] private TileMap _board;
	[Export] private Button _buttonRight;
	[Export] private Button _buttonLeft;
	[Export] private Button _return;
	[Export] private Button _revert;
	[Export] private PackedScene _piece;
	[Export] public Camera2D Camera { get; set; }

    public List<BoardState> BoardHistory { get; set; }

	private Dictionary<int, string> _pieceDict = new();

	private int _boardHistoryIndex;

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
	}

	public void SetUp()
	{
		_boardHistoryIndex = BoardHistory.Count - 1;
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
	}

	private void Right()
	{
		_boardHistoryIndex++;
        _buttonLeft.Disabled = false;
		if (_boardHistoryIndex == BoardHistory.Count - 1)
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
		int[,] board = BoardHistory[_boardHistoryIndex].Board;
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
					piece.SetMeta("Player", cellSituation / 10);
					piece.SetMeta("Piece_Type", _pieceDict[cellSituation % 10]);
					piece.IsUI = true;
					_board.AddChild(piece);
					piece.Position = _board.MapToLocal(new Vector2I(i, j));
				}
			}
		}
	}

	private void Revert()
	{
		EmitSignal(SignalName.previousBoardSelected, _boardHistoryIndex);
	}
}
