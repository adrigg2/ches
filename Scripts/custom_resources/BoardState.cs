using Godot;

namespace Ches;
public partial class BoardState : Resource
{
    private int[,] _zoneOfControl;
    private int[,] _board;
    private int _turn;
    private int _timeLeft;
    private bool _castling;

    public int[,] ZoneOfControl { get => _zoneOfControl; }
    public int[,] Board { get => _board; }
    public int Turn { get => _turn; }
    public int TimeLeft { get => _timeLeft; }
    public bool Castling { get => _castling; }

    public BoardState(int[,] board, int[,] zoneOfControl, int turn, bool castling, int timeLeft = 0)
    {
        _zoneOfControl = zoneOfControl;
        _board = board;
        _turn = turn;
        _castling = castling;
        _timeLeft = timeLeft;
    }
}
