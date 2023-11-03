using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Piece : CharacterBody2D
{
    [Signal]
    public delegate void pieceSelectedEventHandler();

    [Signal]
    public delegate void pieceMovedEventHandler();

    [Signal]
    public delegate void checkUpdatedEventHandler();

    [Signal]
    public delegate void updateCheckEventHandler();

    [Signal]
    public delegate void playerInCheckEventHandler();

    [Signal]
    public delegate void checkmateCheckEventHandler();

    [Signal]
    public delegate void updateTilesEventHandler();

    [Signal]
    public delegate void storeOldPositionsEventHandler();

    [Signal]
    public delegate void castlingSetupEventHandler();

    [Signal]
    public delegate void allowCastlingEventHandler();

    [Signal]
    public delegate void moveRookEventHandler();

    [Signal]
    public delegate void clearEnPassantEventHandler();


    [Export]
    int turn = 1;

    [Export]
    int id;

    [Export]
    bool checkUpdatedCheck = false;

    [Export]
    bool checkmate = false;

    [Export]
    bool firstMovement = true;

    [Export]
    int player;

    const int CELL_PIXELS = 32;
    const int SEES_FRIENDLY_PIECE = 1;
    const int PATH = 2;
    const int PROTECTED = 3;
    const int SEES_ENEMY_KING = 4;
    const int PROTECTED_AND_SEES = 5;
    const int NOT_PROTECTED_AND_SEES = 6;
    const int NOT_PROTECTED = 7;
    const int KING_IN_CHECK = 8;

    public Callable movementCheck;
    public Callable checkCheck;
    public Callable checkArrayCheck;

    PackedScene movement;
    PackedScene capture;
    PackedScene promotion;
    Vector2 oldPos1 = new Vector2();
    Vector2 newPos = new Vector2();
    Vector2 checkPos;
    string pieceType;
    bool isInCheck = false;
    bool enPassant = false;

    public override void _Ready()
    {
        pieceType = (string)GetMeta("Piece_Type");
        player = (int)GetMeta("Player");

        if (pieceType == "pawn")
        {
            id = player * 10;
        }
        else if (pieceType == "king")
        {
            id = player * 10 + 1;
        }
        else if (pieceType == "queen")
        {
            id = player * 10 + 2;
        }
        else if (pieceType == "rook")
        {
            id = player * 10 + 3;
        }
        else if (pieceType == "bishop")
        {
            id = player * 10 + 4;
        }
        else if (pieceType == "knight")
        {
            id = player * 10 + 5;
        }

        if (player == 2)
        {
            Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            Texture2D blackTexture = (Texture2D)GetMeta("Black_Texture");
            sprite.Texture = blackTexture;
        }

        movement = (PackedScene)ResourceLoader.Load("res://scenes/scenery/movement.tscn");
        capture = (PackedScene)ResourceLoader.Load("res://scenes/scenery/capture.tscn");
        promotion = (PackedScene)ResourceLoader.Load("res://scenes/promotion_selection.tscn");

        TileMap board = GetNode<TileMap>("../..");
        Node2D master = GetNode<Node2D>("../../..");
        Node2D playerController = GetNode<Node2D>("..");

        Connect("pieceSelected", new Callable(master, "DisableMovement"));
        Connect("pieceMoved", new Callable(master, "UpdateBoard"));
        Connect("updateCheck", new Callable(master, "Check"));
        Connect("clearEnPassant", new Callable(master, "ClearEnPassant"));
        Connect("updateTiles", new Callable(board, "UpdateTiles"));
        Connect("storeOldPositions", new Callable(board, "StorePos"));
        Connect("checkUpdated", new Callable(playerController, "CheckUpdate"));
        Connect("playerInCheck", new Callable(playerController, "PlayerInCheck"));
        Connect("checkmateCheck", new Callable(playerController, "CheckmateCheck"));
        Connect("castlingSetup", new Callable(playerController, "CastlingSetup"));
        Connect("allowCastling", new Callable(playerController, "AllowCastling"));
        Connect("moveRook", new Callable(playerController, "Castle"));
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            if (turn == player)
            {
                EmitSignal(SignalName.pieceSelected);
                if (pieceType == "pawn")
                {
                    PawnMovement();
                }
                else if (pieceType == "knight")
                {
                    KnightMovement();
                }
                else if (pieceType == "king")
                {
                    KingMovement();
                }
                else
                {
                    if (pieceType == "queen" || pieceType == "rook")
                    {
                        StraightMove();
                    }

                    if (pieceType == "queen" || pieceType == "bishop")
                    {
                        DiagonalMove();
                    }
                }

                GD.Print("movement choices generated");

                GD.Print("piece " + Position + " selected!");
            }
        }

        void PawnMovement()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;

            if (firstMovement == true)
            {
                GD.Print("first movement");
                for (int i = 1; i <= 2; i++)
                {
                    if (player == 1)
                    {
                        movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
                        moveCheck = (int)movementCheck.Call(movePos);
                        blockedPosition = moveCheck / 10;
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                            {
                                CapturePos(movePos);
                            }
                        }

                        movePos = Position + new Vector2(-CELL_PIXELS, -CELL_PIXELS);
                        moveCheck = (int)movementCheck.Call(movePos);
                        blockedPosition = moveCheck / 10;
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                            {
                                CapturePos(movePos);
                            }
                        }

                        movePos = Position + i * new Vector2(0, -CELL_PIXELS);
                        moveCheck = (int)movementCheck.Call(movePos);
                        blockedPosition = moveCheck / 10;
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (!isInCheck || positionSituation == SEES_ENEMY_KING)
                        {
                            if (blockedPosition != 0)
                            {
                                break;
                            }
                            Move(movePos);
                        }
                    }
                    else
                    {
                        movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
                        moveCheck = (int)movementCheck.Call(movePos);
                        blockedPosition = moveCheck / 10;
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                            {
                                CapturePos(movePos);
                            }
                        }

                        movePos = Position + new Vector2(-CELL_PIXELS, CELL_PIXELS);
                        moveCheck = (int)movementCheck.Call(movePos);
                        blockedPosition = moveCheck / 10;
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                            {
                                CapturePos(movePos);
                            }
                        }

                        movePos = Position + i * new Vector2(0, CELL_PIXELS);
                        moveCheck = (int)movementCheck.Call(movePos);
                        blockedPosition = moveCheck / 10;
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (!isInCheck || positionSituation == SEES_ENEMY_KING)
                        {
                            if (blockedPosition != 0)
                            {
                                break;
                            }
                            Move(movePos);
                        }
                    }
                }
            }
            else
            {
                if (player == 1)
                {
                    movePos = Position + new Vector2(0, -CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (!isInCheck || positionSituation == SEES_ENEMY_KING)
                    {
                        if (blockedPosition <= 0)
                        {
                            Move(movePos);
                        }
                    }

                    movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                        {
                            CapturePos(movePos);
                        }
                    }

                    movePos = Position + new Vector2(-CELL_PIXELS, -CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                        {
                            CapturePos(movePos);
                        }
                    }
                }
                else
                {
                    movePos = Position + new Vector2(0, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (!isInCheck || positionSituation == SEES_ENEMY_KING)
                    {
                        if (blockedPosition <= 0)
                        {
                            Move(movePos);
                        }
                    }

                    movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                        {
                            CapturePos(movePos);
                        }
                    }

                    movePos = Position + new Vector2(-CELL_PIXELS, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (!isInCheck || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != 0 && Math.Abs(blockedPosition) != player)
                        {
                            CapturePos(movePos);
                        }
                    }
                }
            }
        }

        void KnightMovement()
        {
            Vector2 movePos;

            movePos = Position - new Vector2(CELL_PIXELS, 0) + new Vector2(0, -(2 * CELL_PIXELS));
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(CELL_PIXELS, 0) + new Vector2(0, -(2 * CELL_PIXELS));
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position - new Vector2(0, CELL_PIXELS) + new Vector2((2 * CELL_PIXELS), 0);
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(0, CELL_PIXELS) + new Vector2((2 * CELL_PIXELS), 0);
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position - new Vector2(CELL_PIXELS, 0) + new Vector2(0, (2 * CELL_PIXELS));
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(CELL_PIXELS, 0) + new Vector2(0, (2 * CELL_PIXELS));
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position - new Vector2(0, CELL_PIXELS) + new Vector2(-(2 * CELL_PIXELS), 0);
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(0, CELL_PIXELS) + new Vector2(-(2 * CELL_PIXELS), 0);
            if (!isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            void CheckMovement()
            {
                int moveCheck = (int)movementCheck.Call(movePos);
                int blockedPosition = moveCheck / 10;

                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8;

                if (notOutOfBounds && blockedPosition <= 0)
                {
                    Move(movePos);
                }
                else if (notOutOfBounds && blockedPosition > 0 && blockedPosition != player)
                {
                    CapturePos(movePos);
                }
            }

            void MovementInCheck()
            {
                int moveCheck = (int)movementCheck.Call(movePos);
                int blockedPosition = moveCheck / 10;
                int positionSituation = (int)checkArrayCheck.Call(movePos);

                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8;
                bool canTakeAttackingPiece = positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES;

                if (notOutOfBounds && blockedPosition <= 0 && positionSituation == SEES_ENEMY_KING)
                {
                    Move(movePos);
                }
                else if (notOutOfBounds && blockedPosition > 0 && blockedPosition != player && canTakeAttackingPiece)
                {
                    CapturePos(movePos);
                }
            }
        }

        void KingMovement()
        {
            Vector2 movePos;

            movePos = Position - new Vector2(0, CELL_PIXELS);
            CheckMovement();

            movePos = Position + new Vector2(0, CELL_PIXELS);
            CheckMovement();

            movePos = Position - new Vector2(CELL_PIXELS, 0);
            CheckMovement();

            movePos = Position + new Vector2(CELL_PIXELS, 0);
            CheckMovement();

            movePos = Position - new Vector2(CELL_PIXELS, CELL_PIXELS);
            CheckMovement();

            movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
            CheckMovement();

            movePos = Position - new Vector2(CELL_PIXELS, -CELL_PIXELS);
            CheckMovement();

            movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
            CheckMovement();

            if (firstMovement && !isInCheck)
            {
                int moveCheck;
                int blockedPosition;
                int checkId;
                int positionSituation;
                bool outOfBounds;
                bool notRook;
                bool enemyPiece;

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, 0);

                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8;
                    notRook = blockedPosition == player && checkId != 3;
                    enemyPiece = blockedPosition != 0 && blockedPosition != player;

                    if (outOfBounds || notRook || enemyPiece || positionSituation == PATH)
                    {
                        GD.Print($"Castling not initiated, {blockedPosition}, {checkId}, {movePos}, {positionSituation}");
                        break;
                    }
                    else if (blockedPosition == player && checkId == 3)
                    {
                        GD.Print("Castling initiated");
                        EmitSignal(SignalName.castlingSetup, movePos);
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, 0);

                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8;
                    notRook = blockedPosition == player && checkId != 3;
                    enemyPiece = blockedPosition != 0 && blockedPosition != player;

                    if (outOfBounds || notRook || enemyPiece || positionSituation == PATH)
                    {
                        GD.Print($"Castling not initiated, {blockedPosition}, {checkId}, {movePos}, {positionSituation}");
                        break;
                    }
                    else if (blockedPosition == player && checkId == 3)
                    {
                        GD.Print("Castling initiated");
                        EmitSignal(SignalName.castlingSetup, movePos);
                    }
                }
            }

            void CheckMovement()
            {
                int moveCheck = (int)movementCheck.Call(movePos);
                int blockedPosition = moveCheck / 10;
                int checkPosition = (int)checkArrayCheck.Call(movePos);

                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8;


                if (notOutOfBounds && blockedPosition <= 0 && checkPosition != SEES_ENEMY_KING && checkPosition != PATH)
                {
                    Move(movePos);
                }
                else if (notOutOfBounds && blockedPosition > 0 && blockedPosition != player && checkPosition != PROTECTED && checkPosition != PROTECTED_AND_SEES)
                {
                    CapturePos(movePos);
                }
            }
        }

        void StraightMove()
        {
            Vector2 movePos;
            int moveSituation;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(0, CELL_PIXELS);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(0, CELL_PIXELS);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, 0);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, 0);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }
        }

        void DiagonalMove()
        {
            Vector2 movePos;
            int moveSituation;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, CELL_PIXELS);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, CELL_PIXELS);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, -CELL_PIXELS);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, -CELL_PIXELS);
                moveSituation = MoveDiagonalStraight(movePos);

                if (moveSituation == 0)
                {
                    break;
                }
                else if (moveSituation == 1)
                {
                    Move(movePos);
                }
                else if (moveSituation == 2)
                {
                    CapturePos(movePos);
                    break;
                }
            }
        }

        int MoveDiagonalStraight(Vector2 _movePos)
        {
            bool outOfBounds = _movePos.X < 0 || _movePos.Y < 0 || _movePos.X > CELL_PIXELS * 8 || _movePos.Y > CELL_PIXELS * 8;
            int moveCheck = (int)movementCheck.Call(_movePos);
            int blockedPos = moveCheck / 10;
            int positionSituation = (int)checkArrayCheck.Call(_movePos);

            if (!isInCheck || positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES || blockedPos == player)
            {
                if (outOfBounds || blockedPos == player)
                {
                    return 0;
                }
                else if ((!isInCheck && blockedPos > 0) || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return 2;
                }
                else if (!isInCheck || positionSituation == SEES_ENEMY_KING)
                {
                    return 1;
                }
            }

            return 0;
        }

        void Move(Vector2 _movePos)
        {
            CharacterBody2D _movement = (CharacterBody2D)movement.Instantiate();
            AddChild(_movement);
            GD.Print(_movePos);
            _movement.Position = _movePos;
        }

        void CapturePos(Vector2 _movePos)
        {
            CharacterBody2D _capture = (CharacterBody2D)capture.Instantiate();
            AddChild(_capture);
            _capture.Position = _movePos;
        }
    }

    public void MovementSelected(Vector2 newPosition)
    {
        Vector2 oldPos;
        oldPos = Position;
        Position = newPosition;
        if (firstMovement == true)
        {
            firstMovement = false;
            if (pieceType == "pawn" && Position == oldPos + new Vector2(0, 2 * CELL_PIXELS))
            {
                EmitSignal(SignalName.pieceMoved, newPosition - new Vector2(0, CELL_PIXELS), oldPos, -id, false);
                enPassant = true;
            }
            else if (pieceType == "pawn" && Position == oldPos + new Vector2(0, -2 * CELL_PIXELS))
            {
                EmitSignal(SignalName.pieceMoved, newPosition + new Vector2(0, CELL_PIXELS), oldPos, -id, false);
                enPassant = true;
            }
        }
        if (pieceType == "king")
        {
            if (oldPos == Position + new Vector2(-2 * CELL_PIXELS, 0) || oldPos == Position + new Vector2(2 * CELL_PIXELS, 0))
            {
                EmitSignal(SignalName.moveRook, newPosition);
            }
        }
        if (pieceType == "pawn" && (newPosition.Y < CELL_PIXELS || newPosition.Y > CELL_PIXELS * 7))
        {
            Control promotionSelection = (Control)promotion.Instantiate();
            AddChild(promotionSelection);

            if (player == 2)
            {
                promotionSelection.Scale = new Vector2(-1, -1);
                promotionSelection.Position = Position - new Vector2(CELL_PIXELS, 0);
            }
            else
            {
                promotionSelection.Position = Position + new Vector2(CELL_PIXELS, 0);
            }
            
            EmitSignal(SignalName.pieceMoved, newPosition, oldPos, id, true);
        }
        else
        {
            EmitSignal(SignalName.pieceMoved, newPosition, oldPos, id, false);
        }

        Vector2[] oldPositions = { oldPos, newPosition };
        EmitSignal(SignalName.storeOldPositions, oldPositions);
        EmitSignal(SignalName.updateTiles, oldPos, new Vector2I(0, 1));
        EmitSignal(SignalName.updateTiles, newPosition, new Vector2I(1, 1));
    }

    public void ChangeTurn(int newTurn)
    {
        turn = newTurn;
        checkUpdatedCheck = false;
        checkmate = false;

        if (isInCheck && pieceType == "king")
        {
            isInCheck = false;
            EmitSignal(SignalName.updateTiles, checkPos, new Vector2I(0, 0));
            EmitSignal(SignalName.playerInCheck, false);
        }

        if (turn == 2)
        {
            Scale = new Vector2(-1, -1);
        } 
        else
        {
            Scale = new Vector2(1, 1);
        }

        if (turn == player && pieceType == "pawn" && enPassant)
        {
            EmitSignal(SignalName.clearEnPassant, player);
            enPassant = false;
        }

        UpdateCheck();
    }

    public void Capture(Vector2 _capturePos, CharacterBody2D _capture)
    {
        if (pieceType == "pawn" && enPassant && (_capturePos == Position - new Vector2 (0, CELL_PIXELS) || _capturePos == Position + new Vector2(0, CELL_PIXELS)))
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            EmitSignal(SignalName.clearEnPassant, player);
            QueueFree();
        }
        else if (_capturePos == Position)
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            QueueFree();
        }
    }

    public void UpdateCheck()
    {
        if (player != turn)
        {
            Vector2[] checkPosArray = new Vector2[8];
            if (pieceType == "pawn")
            {
                GD.Print(player, " ", pieceType, " is check");
                PawnCheck();
            }
            else if (pieceType == "knight")
            {
                GD.Print(player, " ", pieceType, " is check");
                KnightCheck();
            }
            else if (pieceType == "king")
            {
                GD.Print(player, " ", pieceType, " is check");
                KingCheck();
            }
            else
            {
                if (pieceType == "queen" || pieceType == "rook")
                {
                    GD.Print(player, " ", pieceType, " is check");
                    StraightCheck();
                }

                if (pieceType == "queen" || pieceType == "bishop")
                {
                    GD.Print(player, " ", pieceType, " is check");
                    DiagonalCheck();
                }
            }

            void PawnCheck()
            {
                Vector2 movePos;
                int moveCheck;
                int blockedPosition;
                int checkId;

                if (player == 1)
                {
                    movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS); 
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_ENEMY_KING);
                    }
                    else if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition == player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_FRIENDLY_PIECE);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, PATH);
                    }

                    movePos = Position + new Vector2(-CELL_PIXELS, -CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_ENEMY_KING);
                    }
                    else if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition == player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_FRIENDLY_PIECE);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, PATH);
                    }
                }
                else
                {
                    movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_ENEMY_KING);
                    }
                    else if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition == player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_FRIENDLY_PIECE);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, PATH);
                    }

                    movePos = Position + new Vector2(-CELL_PIXELS, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_ENEMY_KING);
                    }
                    else if (movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8 && blockedPosition == player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_FRIENDLY_PIECE);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, PATH);
                    }
                }
            }

            void KnightCheck()
            {
                Vector2 movePos;

                movePos = Position - new Vector2(CELL_PIXELS, 0) + new Vector2(0, -(2 * CELL_PIXELS));
                MovePossibilityCheck();

                movePos = Position + new Vector2(CELL_PIXELS, 0) + new Vector2(0, -(2 * CELL_PIXELS));
                MovePossibilityCheck();

                movePos = Position - new Vector2(0, CELL_PIXELS) + new Vector2((2 * CELL_PIXELS), 0);
                MovePossibilityCheck();

                movePos = Position + new Vector2(0, CELL_PIXELS) + new Vector2((2 * CELL_PIXELS), 0);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CELL_PIXELS, 0) + new Vector2(0, (2 * CELL_PIXELS));
                MovePossibilityCheck();

                movePos = Position + new Vector2(CELL_PIXELS, 0) + new Vector2(0, (2 * CELL_PIXELS));
                MovePossibilityCheck();

                movePos = Position - new Vector2(0, CELL_PIXELS) + new Vector2(-(2 * CELL_PIXELS), 0);
                MovePossibilityCheck();

                movePos = Position + new Vector2(0, CELL_PIXELS) + new Vector2(-(2 * CELL_PIXELS), 0);
                MovePossibilityCheck();

                void MovePossibilityCheck()
                {
                    int moveCheck = (int)movementCheck.Call(movePos);
                    int blockedPosition = moveCheck / 10;
                    int checkId = moveCheck % 10;

                    bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8;

                    if (notOutOfBounds && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_ENEMY_KING);
                    }
                    else if (notOutOfBounds && blockedPosition == player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_FRIENDLY_PIECE);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, PATH);
                    }
                }
            }

            void KingCheck()
            {
                Vector2 movePos;

                movePos = Position - new Vector2(0, CELL_PIXELS);
                MovePossibilityCheck();

                movePos = Position + new Vector2(0, CELL_PIXELS);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CELL_PIXELS, 0);
                MovePossibilityCheck();

                movePos = Position + new Vector2(CELL_PIXELS, 0);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CELL_PIXELS, CELL_PIXELS);
                MovePossibilityCheck();

                movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CELL_PIXELS, -CELL_PIXELS);
                MovePossibilityCheck();

                movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
                MovePossibilityCheck();

                void MovePossibilityCheck()
                {
                    int moveCheck = (int)movementCheck.Call(movePos);
                    int blockedPosition = moveCheck / 10;

                    bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8;

                    if (notOutOfBounds && blockedPosition == player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SEES_FRIENDLY_PIECE);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, PATH);
                    }
                }
            }

            void StraightCheck()
            {
                Vector2 movePos;
                int moveCheck;
                int blockedPosition;
                int checkId;

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(0, CELL_PIXELS); 
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(0, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, 0);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, 0);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }
            }

            void DiagonalCheck()
            {
                Vector2 movePos;
                int moveCheck;
                int blockedPosition;
                int checkId;

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, CELL_PIXELS); 
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, -CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CELL_PIXELS, -CELL_PIXELS);
                    moveCheck = (int)movementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || (blockedPosition != 0 && blockedPosition != player && checkId != 1))
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, PATH);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_ENEMY_KING);
                        break;
                    }
                    else if (blockedPosition == player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SEES_FRIENDLY_PIECE);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }
            }

            void CaptureCheck(Vector2[] capturePos, Vector2 checkPiece, int maxIndex, int checkSituation)
            {
                Vector2I arrPos;
                TileMap board = GetNode<TileMap>("../..");

                arrPos = board.LocalToMap(checkPiece);

                EmitSignal(SignalName.updateCheck, arrPos, checkSituation, true, false);

                for (int i = 1; i <= maxIndex; i++)
                {
                    arrPos = board.LocalToMap(capturePos[i]);
                    GD.Print($"{arrPos} {Name} check array set");

                    if (capturePos[i] == new Vector2(0, 0) || arrPos == new Vector2I(0, 0))
                    {
                        GD.Print($"TEST check {Name} {player} {i}");
                    }

                    if (checkSituation == SEES_FRIENDLY_PIECE)
                    {
                        if (i != maxIndex)
                        {
                            EmitSignal(SignalName.updateCheck, arrPos, checkSituation, false, false);
                        }
                        else
                        {
                            EmitSignal(SignalName.updateCheck, arrPos, checkSituation, true, true);
                        }
                    }
                    else if (checkSituation == SEES_ENEMY_KING)
                    {
                        if (i != maxIndex)
                        {
                            EmitSignal(SignalName.updateCheck, arrPos, checkSituation, false, false);
                        }
                        else
                        {
                            EmitSignal(SignalName.updateCheck, arrPos, KING_IN_CHECK, false, false);
                        }
                    }
                    else
                    {
                        EmitSignal(SignalName.updateCheck, arrPos, checkSituation, false, false);
                    }
                }
            }
        }

        checkUpdatedCheck = true;
        EmitSignal(SignalName.checkUpdated);
    }

    public void CheckCheckState()
    {
        if (pieceType == "king" && turn == player)
        {
            GD.Print(player, " is checking wether he is on check");
            int check = (int)checkCheck.Call(Position);

            if (check == KING_IN_CHECK)
            {
                isInCheck = true;
                GD.Print(player, " is in check");
                checkPos = Position;
                EmitSignal(SignalName.updateTiles, Position, new Vector2I(1, 2));
                EmitSignal(SignalName.playerInCheck, true);
            }
        }
    }

    public void SetCheck(bool inCheck)
    {
        isInCheck = inCheck;
        if (isInCheck)
        {
            CheckCheckmate();
        }
    }

    public void CheckCheckmate()
    {
        if (pieceType == "pawn")
        {
            PawnMateCheck();
        }
        else if (pieceType == "knight")
        {
            KnightMateCheck();
        }
        else if (pieceType == "king")
        {
            KingMateCheck();
        }
        else
        {
            if (pieceType == "queen" || pieceType == "rook")
            {
                StraightMateCheck();
            }

            if (pieceType == "queen" || pieceType == "bishop")
            {
                DiagonalMateCheck();
            }
        }

        EmitSignal(SignalName.checkmateCheck);

        void PawnMateCheck()
        {
            Vector2 movePos;
            int positionSituation;

            if (firstMovement == true)
            {
                for (int i = 1; i <= 2; i++)
                {
                    if (player == 1)
                    {
                        movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            return;
                        }

                        movePos = Position + new Vector2(-CELL_PIXELS, -CELL_PIXELS);
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            return;
                        }
                        movePos = Position + i * new Vector2(0, -CELL_PIXELS);
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (positionSituation == SEES_ENEMY_KING)
                        {
                            return;
                        }
                    }
                    else
                    {
                        movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            return;
                        }
                        movePos = Position + new Vector2(-CELL_PIXELS, CELL_PIXELS);
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                        {
                            return;
                        }
                        movePos = Position + i * new Vector2(0, CELL_PIXELS);
                        positionSituation = (int)checkArrayCheck.Call(movePos);

                        if (positionSituation == SEES_ENEMY_KING)
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                if (player == 1)
                {
                    movePos = Position + new Vector2(0, -CELL_PIXELS);
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (positionSituation == SEES_ENEMY_KING)
                    {
                        return;
                    }
                    movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        return;
                    }
                    movePos = Position + new Vector2(-CELL_PIXELS, -CELL_PIXELS);
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        return;
                    }

                }
                else
                {
                    movePos = Position + new Vector2(0, CELL_PIXELS);
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (positionSituation == SEES_ENEMY_KING)
                    {
                        return;
                    }
                    movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        return;
                    }
                    movePos = Position + new Vector2(-CELL_PIXELS, CELL_PIXELS);
                    positionSituation = (int)checkArrayCheck.Call(movePos);

                    if (positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                    {
                        return;
                    }
                }
            }

            checkmate = true;
        }

        void KnightMateCheck()
        {
            Vector2 movePos;
            int positionSituation;

            movePos = Position - new Vector2(CELL_PIXELS, 0) + new Vector2(0, -(2 * CELL_PIXELS));
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position + new Vector2(CELL_PIXELS, 0) + new Vector2(0, -(2 * CELL_PIXELS));
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position - new Vector2(0, CELL_PIXELS) + new Vector2((2 * CELL_PIXELS), 0);
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position + new Vector2(0, CELL_PIXELS) + new Vector2((2 * CELL_PIXELS), 0);
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position - new Vector2(CELL_PIXELS, 0) + new Vector2(0, (2 * CELL_PIXELS));
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position + new Vector2(CELL_PIXELS, 0) + new Vector2(0, (2 * CELL_PIXELS));
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position - new Vector2(0, CELL_PIXELS) + new Vector2(-(2 * CELL_PIXELS), 0);
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            movePos = Position + new Vector2(0, CELL_PIXELS) + new Vector2(-(2 * CELL_PIXELS), 0);
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
            {
                return;
            }

            checkmate = true;
        }

        void KingMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;

            movePos = Position - new Vector2(0, CELL_PIXELS);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position + new Vector2(0, CELL_PIXELS);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position - new Vector2(CELL_PIXELS, 0);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position + new Vector2(CELL_PIXELS, 0);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position - new Vector2(CELL_PIXELS, CELL_PIXELS);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position + new Vector2(CELL_PIXELS, CELL_PIXELS);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position - new Vector2(CELL_PIXELS, -CELL_PIXELS);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            movePos = Position + new Vector2(CELL_PIXELS, -CELL_PIXELS);
            moveCheck = (int)movementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)checkArrayCheck.Call(movePos);

            if ((positionSituation == 0 && blockedPosition == 0 && movePos.X > 0 && movePos.Y > 0 && movePos.X < CELL_PIXELS * 8 && movePos.Y < CELL_PIXELS * 8) || positionSituation == NOT_PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED)
            {
                return;
            }

            checkmate = true;
        }

        void StraightMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;

            checkmate = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(0, CELL_PIXELS);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(0, CELL_PIXELS);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, 0);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, 0);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            checkmate = true;
        }

        void DiagonalMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;

            checkmate = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, CELL_PIXELS);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, CELL_PIXELS);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, -CELL_PIXELS);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CELL_PIXELS, -CELL_PIXELS);
                moveCheck = (int)movementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)checkArrayCheck.Call(movePos);

                if (positionSituation == SEES_ENEMY_KING || positionSituation == PROTECTED_AND_SEES || positionSituation == NOT_PROTECTED_AND_SEES)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CELL_PIXELS * 8 || movePos.Y > CELL_PIXELS * 8 || blockedPosition == player)
                {
                    break;
                }
            }

            checkmate = true;
        }
    }

    public void FirstMovementCheck(Vector2 position)
    {
        GD.Print("Check castling");
        if (position == Position)
        {
            if (firstMovement)
            {
                GD.Print("castling");
                EmitSignal(SignalName.allowCastling, true, Position);
            }
            else
            {
                GD.Print("not castling");
                EmitSignal(SignalName.allowCastling, false, Position);
            }
        }
    }

    public void Castling(bool castlingAllowed, Vector2 rookPosition)
    {
        TileMap board = GetNode<TileMap>("../..");
        Vector2 movePos;

        GD.Print($"Castling {castlingAllowed} {pieceType}");
        if (pieceType == "king" && castlingAllowed)
        {
            Vector2I cell = board.LocalToMap(rookPosition);
            GD.Print($"Castling cell {cell}");
            if (cell.X == 0)
            {
                GD.Print("Long castling");
                movePos = Position + new Vector2(-2 * CELL_PIXELS, 0);
                CharacterBody2D _movement = (CharacterBody2D)movement.Instantiate();
                AddChild(_movement);
                GD.Print(movePos);
                _movement.Position = movePos;
            }
            else if (cell.X == 7)
            {
                GD.Print("Short castling");
                movePos = Position + new Vector2(2 * CELL_PIXELS, 0);
                CharacterBody2D _movement = (CharacterBody2D)movement.Instantiate();
                AddChild(_movement);
                GD.Print(movePos);
                _movement.Position = movePos;
            }
        }
    }

    public void Castle(Vector2 position, Vector2 newPosition)
    {
        GD.Print("Finish castling 2");
        if (position == Position)
        {
            Vector2 oldPos = Position;
            Position = newPosition;
            EmitSignal(SignalName.pieceMoved, newPosition, oldPos, id, false);
        }
    }
}