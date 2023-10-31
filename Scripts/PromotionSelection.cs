using Godot;
using System;

public partial class PromotionSelection : Control
{
    [Signal]
    public delegate void pawnPromotionEventHandler();

    CharacterBody2D pawn;

    PackedScene rookPiece;
    PackedScene knightPiece;
    PackedScene bishopPiece;
    PackedScene queenPiece;

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

        rookPiece = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/rook.tscn");
        knightPiece = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/knight.tscn");
        bishopPiece = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/bishop.tscn");
        queenPiece = (PackedScene)ResourceLoader.Load("res://Scenes/Pieces/queen.tscn");

        Node2D newParent = GetNode<Node2D>("../..");
        Node2D master = GetNode<Node2D>("../../../..");
        pawn = (CharacterBody2D)GetParent();

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        Callable playerController = new Callable(newParent, "ConnectPromotedPiece");
        Callable master1 = new Callable(master, "ConnectPromotedPiece");
        Connect("pawnPromotion", playerController);
        Connect("pawnPromotion", master1);

        int player = (int)pawn.Get("player");

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
        Vector2 position = pawn.Position;
        int player = (int)pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        pawn.QueueFree();

        CharacterBody2D queen = (CharacterBody2D)queenPiece.Instantiate();
        queen.SetMeta("Player", player);
        playerController.AddChild(queen);
        queen.Position = position;

        EmitSignal(SignalName.pawnPromotion, queen, player);

        QueueFree();
    }

    private void RookPromotion()
    {
        Vector2 position = pawn.Position;
        int player = (int)pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        pawn.QueueFree();

        CharacterBody2D rook = (CharacterBody2D)rookPiece.Instantiate();
        rook.SetMeta("Player", player);
        playerController.AddChild(rook);
        rook.Position = position;

        EmitSignal(SignalName.pawnPromotion, rook, player);

        QueueFree();
    }

    private void BishopPromotion()
    {
        Vector2 position = pawn.Position;
        int player = (int)pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        pawn.QueueFree();

        CharacterBody2D bishop = (CharacterBody2D)bishopPiece.Instantiate();
        bishop.SetMeta("Player", player);
        playerController.AddChild(bishop);
        bishop.Position = position;

        EmitSignal(SignalName.pawnPromotion, bishop, player);

        QueueFree();
    }

    private void KnightPromotion()
    {
        Vector2 position = pawn.Position;
        int player = (int)pawn.Get("player");
        Node2D playerController = GetNode<Node2D>("..");

        pawn.QueueFree();

        CharacterBody2D knight = (CharacterBody2D)knightPiece.Instantiate();
        knight.SetMeta("Player", player);
        playerController.AddChild(knight);
        knight.Position = position;

        EmitSignal(SignalName.pawnPromotion, knight, player);

        QueueFree();
    }
}
