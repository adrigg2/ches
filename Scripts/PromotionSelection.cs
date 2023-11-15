using Godot;

namespace Ches;
public partial class PromotionSelection : Control
{
    [Signal]
    public delegate void pawnPromotionEventHandler();

    private CharacterBody2D _pawn;

    private PackedScene _piece;

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

        _piece = (PackedScene)ResourceLoader.Load("res://scenes/piece.tscn");

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

        CharacterBody2D queen = (CharacterBody2D)_piece.Instantiate();
        queen.SetMeta("Player", player);
        queen.SetMeta("Piece_Type", "queen");
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

        CharacterBody2D rook = (CharacterBody2D)_piece.Instantiate();
        rook.SetMeta("Player", player);
        rook.SetMeta("Piece_Type", "rook");
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

        CharacterBody2D bishop = (CharacterBody2D)_piece.Instantiate();
        bishop.SetMeta("Player", player);
        bishop.SetMeta("Piece_Type", "bishop");
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

        CharacterBody2D knight = (CharacterBody2D)_piece.Instantiate();
        knight.SetMeta("Player", player);
        knight.SetMeta("Piece_Type", "knight");
        playerController.AddChild(knight);
        knight.Position = position;

        EmitSignal(SignalName.pawnPromotion, knight, player);

        QueueFree();
    }
}
