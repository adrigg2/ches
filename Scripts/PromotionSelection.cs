using Godot;

namespace Ches.Chess;
public partial class PromotionSelection : Control
{
    [Signal]
    public delegate void PiecePromotedEventHandler();

    private Piece _pieceToPromote;

    private PackedScene _piece;

    public Piece PieceToPromote { get => _pieceToPromote; set => _pieceToPromote = value; }

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
        _pieceToPromote = (Piece)GetParent();

        GetParent().RemoveChild(this);
        newParent.AddChild(this);

        int player = (int)_pieceToPromote.Get("_player");

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
        int[] movementDirections = new int[8] { 8, 8, 8, 8, 8, 8, 8, 8 };
        int[] captureDirections = new int[8] { 8, 8, 8, 8, 8, 8, 8, 8 };
        Godot.Collections.Dictionary<int, Texture2D> textures = new()
        {
            { 1, (Texture2D)ResourceLoader.Load("res://assets/sprites/queen_w.png") },
            { 2, (Texture2D)ResourceLoader.Load("res://assets/sprites/queen_b.png") }
        };

        _pieceToPromote.PromotePiece(movementDirections, captureDirections, textures);
        _pieceToPromote.UpdateSprite();

        EmitSignal(SignalName.PiecePromoted);

        QueueFree();
    }

    private void RookPromotion()
    {
        int[] movementDirections = new int[8] { 8, 0, 8, 0, 8, 0, 8, 0 };
        int[] captureDirections = new int[8] { 8, 0, 8, 0, 8, 0, 8, 0 };
        Godot.Collections.Dictionary<int, Texture2D> textures = new()
        {
            { 1, (Texture2D)ResourceLoader.Load("res://assets/sprites/rook_w.png") },
            { 2, (Texture2D)ResourceLoader.Load("res://assets/sprites/rook_b.png") }
        };

        _pieceToPromote.PromotePiece(movementDirections, captureDirections, textures);
        _pieceToPromote.UpdateSprite();

        EmitSignal(SignalName.PiecePromoted);

        QueueFree();
    }

    private void BishopPromotion()
    {
        int[] movementDirections = new int[8] { 0, 8, 0, 8, 0, 8, 0, 8 };
        int[] captureDirections = new int[8] { 0, 8, 0, 8, 0, 8, 0, 8 };
        Godot.Collections.Dictionary<int, Texture2D> textures = new()
        {
            { 1, (Texture2D)ResourceLoader.Load("res://assets/sprites/bishop_w.png") },
            { 2, (Texture2D)ResourceLoader.Load("res://assets/sprites/bishop_b.png") }
        };

        _pieceToPromote.PromotePiece(movementDirections, captureDirections, textures);
        _pieceToPromote.UpdateSprite();

        EmitSignal(SignalName.PiecePromoted);

        QueueFree();
    }

    private void KnightPromotion()
    {
        int[] movementDirections = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
        int[] captureDirections = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
        Godot.Collections.Dictionary<int, Texture2D> textures = new()
        {
            { 1, (Texture2D)ResourceLoader.Load("res://assets/sprites/knight_w.png") },
            { 2, (Texture2D)ResourceLoader.Load("res://assets/sprites/knight_b.png") }
        };

        _pieceToPromote.PromotePiece(movementDirections, captureDirections, textures, knightMovement: true, knightCapture: true);
        _pieceToPromote.UpdateSprite();

        EmitSignal(SignalName.PiecePromoted);

        QueueFree();
    }
}
