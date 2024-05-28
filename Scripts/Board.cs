using Godot;

namespace Ches.Chess;
public partial class Board : TileMap
{
    [Signal]
    public delegate void PlayersSetEventHandler();

    [Signal]
    public delegate void TimersSetEventHandler(Timer timer, int player);

    private Vector2 _selectedPosition;
    private int[,] _squares;
    private int _length;
    private int _height;
    private SquareSituation[,] _checkSquares;

    public int[,] Squares { get => _squares; set => _squares = value; }
    public SquareSituation[,] CheckSquares { get => _checkSquares; set => _checkSquares = value; }
    public int Length { get => _length; }
    public int Height { get => _height; }

    public override void _Ready()
    {
        _selectedPosition = new Vector2(-1, -1);
        _length = 8;
        _height = 8;

        _squares = new int[_height, _length];
        _checkSquares = new SquareSituation[_height, _length];

        Piece.GameBoard = this;

        for (int i = 1; i < 3; i++)
        {
            Player player = new();
            player.SetFields(i, (ChessGame)GetParent());
            player.TimersSet += (timer, player) => EmitSignal(SignalName.TimersSet, timer, player);
            AddChild(player);
        }
        EmitSignal(SignalName.PlayersSet);

        foreach (Node player in GetChildren())
        {
            if (player is Player)
            {
                foreach (Node piece in player.GetChildren())
                {
                    if (piece is Piece piece1)
                    {
                        piece1.UpdateTiles += UpdateTiles;
                        piece1.ClearDynamicTiles += ClearDynamicTiles;
                    }
                }
            }
        }
    }

    public void UpdateTiles(Vector2 position, Vector2I cellAtlas, string piece)
    {
        Vector2I cellCoords = LocalToMap(position);
        GD.Print($"{cellCoords} {cellAtlas} {piece} update tiles");
        if (cellAtlas == new Vector2I(0, 3))
        {
            if (_selectedPosition != new Vector2(-1, -1))
            {
                EraseCell(1, LocalToMap(_selectedPosition));
            }

            _selectedPosition = position;
            SetCell(1, cellCoords, 0, cellAtlas);
        }
        else
        {
            SetCell(1, cellCoords, 0, cellAtlas);
        }
    }

    public void ClearDynamicTiles()
    {
        GD.Print("Clearing update tiles");
        _selectedPosition = new Vector2(-1, -1);
        ClearLayer(1);
    }

    public void Reset()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Player player)
            {
                player.Reset();
            }
        }

        EmitSignal(SignalName.PlayersSet);

        ClearDynamicTiles();
    }

    public int CheckBoardSquares(Vector2 position)
    {
        Vector2I square = LocalToMap(position);
        return _squares[square.X, square.Y];
    }

    public SquareSituation CheckCheckSquares(Vector2 position)
    {
        Vector2I square = LocalToMap(position);
        return _checkSquares[square.X, square.Y];
    }

    public void SetBoardSquares(Vector2 position, int value)
    {
        Vector2I square = LocalToMap(position);
        GD.Print($"Setting {square} to {value} in BoardCells");
        _squares[square.X, square.Y] = value;
    }

    public void SetCheckSquares(Vector2 position, SquareSituation value)
    {
        Vector2I square = LocalToMap(position);
        GD.Print($"Setting {square} to {value} in CheckCells");
        _checkSquares[square.X, square.Y] = value;
    }
}
