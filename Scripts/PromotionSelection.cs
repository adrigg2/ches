using Godot;

namespace Chess;
public partial class PromotionSelection : Control
{
    [Signal]
    public delegate void pawnPromotionEventHandler();

    private CharacterBody2D _pawn;

    private PackedScene _rookPiece;
    private PackedScene _knightPiece;
    private PackedScene _bishopPiece;
    private PackedScene _queenPiece;

    public override void _Ready()
	{
        Button queen = GetNode<Button>("SelectionContainer/Queen");
        Button rook = GetNode<Button>("SelectionContainer/Rook");
        Button bishop = GetNode<Button>("SelectionContainer/Bishop");
        Button knight = GetNode<Button>("SelectionContainer/Knight");
        queen.Pressed += QueenPromotion;
        rook.Pressed += RookPromotion;
        bishop.Pressed += BishopPromotion;
        knight.Pressed += KnightPromotion;

        _rookPiece = (PackedScene)ResourceLoader.Load("res://scenes/pieces/rook.tscn");
        _knightPiece = (PackedScene)ResourceLoader.Load("res://scenes/pieces/knight.tscn");
        _bishopPiece = (PackedScene)ResourceLoader.Load("res://scenes/pieces/bishop.tscn");
        _queenPiece = (PackedScene)ResourceLoader.Load("res://scenes/pieces/queen.tscn");

        Node2D newParent = GetNode<Node2D>("../..");
        Node2D master = GetNode<Node2D>("../../../..");
        _pawn = (CharacterBody2D)GetParent();

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        Connect("pawnPromotion", new Callable(newParent, "ConnectPromotedPiece"));
        Connect("pawnPromotion", new Callable(master, "ConnectPromotedPiece"));

        int player = (int)_pawn.Get("player");

        if (player == 2)
        {
            queen.Icon = (Texture2D)queen.GetMeta("Black_Texture");
            rook.Icon = (Texture2D)rook.GetMeta("Black_Texture");
            bishop.Icon = (Texture2D)bishop.GetMeta("Black_Texture");
            knight.Icon = (Texture2D)knight.GetMeta("Black_Texture");
        }
    }

    private void QueenPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        CharacterBody2D queen = (CharacterBody2D)_queenPiece.Instantiate();
        queen.SetMeta("Player", player);
        playerController.AddChild(queen);
        queen.Position = position;

        EmitSignal(SignalName.pawnPromotion, queen, player);

        QueueFree();
    }

    private void RookPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        CharacterBody2D rook = (CharacterBody2D)_rookPiece.Instantiate();
        rook.SetMeta("Player", player);
        playerController.AddChild(rook);
        rook.Position = position;

        EmitSignal(SignalName.pawnPromotion, rook, player);

        QueueFree();
    }

    private void BishopPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        CharacterBody2D bishop = (CharacterBody2D)_bishopPiece.Instantiate();
        bishop.SetMeta("Player", player);
        playerController.AddChild(bishop);
        bishop.Position = position;

        EmitSignal(SignalName.pawnPromotion, bishop, player);

        QueueFree();
    }

    private void KnightPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        CharacterBody2D knight = (CharacterBody2D)_knightPiece.Instantiate();
        knight.SetMeta("Player", player);
        playerController.AddChild(knight);
        knight.Position = position;

        EmitSignal(SignalName.pawnPromotion, knight, player);

        QueueFree();
    }
}
