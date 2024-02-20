using Godot;

namespace Ches.Chess;
public partial class PromotionSelection : Control
{
    [Signal]
    public delegate void PawnPromotionEventHandler();

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

        Connect("PawnPromotion", new Callable(master, "PromotionComplete"));
        Connect("PawnPromotion", new Callable(newParent, "ConnectToPromotedPiece"));

        int player = (int)_pawn.Get("_player");

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
        int player = (int)_pawn.Get("_player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        Piece queen = (Piece)_piece.Instantiate();
        queen.SetMeta("Player", player);
        queen.SetMeta("Piece_Type", "queen");
        playerController.AddChild(queen);
        queen.Position = position;

        EmitSignal(SignalName.PawnPromotion, queen, player);

        QueueFree();
    }

    private void RookPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("_player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        Piece rook = (Piece)_piece.Instantiate();
        rook.SetMeta("Player", player);
        rook.SetMeta("Piece_Type", "rook");
        playerController.AddChild(rook);
        rook.Position = position;

        EmitSignal(SignalName.PawnPromotion, rook, player);

        QueueFree();
    }

    private void BishopPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("_player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        Piece bishop = (Piece)_piece.Instantiate();
        bishop.SetMeta("Player", player);
        bishop.SetMeta("Piece_Type", "bishop");
        playerController.AddChild(bishop);
        bishop.Position = position;

        EmitSignal(SignalName.PawnPromotion, bishop, player);

        QueueFree();
    }

    private void KnightPromotion()
    {
        Vector2 position = _pawn.Position;
        int player = (int)_pawn.Get("_player");
        Node2D playerController = GetNode<Node2D>("..");

        _pawn.QueueFree();

        Piece knight = (Piece)_piece.Instantiate();
        knight.SetMeta("Player", player);
        knight.SetMeta("Piece_Type", "knight");
        playerController.AddChild(knight);
        knight.Position = position;

        EmitSignal(SignalName.PawnPromotion, knight, player);

        QueueFree();
    }
}
