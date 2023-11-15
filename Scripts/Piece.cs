using Godot;
using System;

namespace Ches;

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
    public delegate void clearDynamicTilesEventHandler();

    [Signal]
    public delegate void castlingSetupEventHandler();

    [Signal]
    public delegate void allowCastlingEventHandler();

    [Signal]
    public delegate void moveRookEventHandler();

    [Signal]
    public delegate void clearEnPassantEventHandler();


    [Export]
    private int _turn = 1;

    [Export]
    private int _id;

    [Export]
    private bool _checkUpdatedCheck = false;

    [Export]
    private bool _checkmate = false;

    [Export]
    private bool _firstMovement = true;

    [Export]
    private int _player;

    [Export]
    private bool _isInCheck = false;

    [Export]
    private PieceTextures _textures;

    const int CellPixels = 32;
    const int SeesFriendlyPiece = 1;
    const int Path = 2;
    const int Potected = 3;
    const int SeesEnemyKing = 4;
    const int ProtectedAndSees = 5;
    const int NotProtectedAndSees = 6;
    const int NotProtected = 7;
    const int KingInCheck = 8;

    private PackedScene _movement;
    private PackedScene _capture;
    private PackedScene _promotion;
    private Vector2 _pawnVector;
    private string _pieceType;
    private bool _enPassant = false;

    public static Callable MovementCheck { get; set; }
    public static Callable CheckCheck { get; set; }
    public static Callable CheckArrayCheck { get; set; }

    public override void _Ready()
    {
        _pieceType = (string)GetMeta("Piece_Type");
        _player = (int)GetMeta("Player");

        if (_pieceType == "pawn")
        {
            _id = _player * 10;
            if (_player == 1)
            {
                _pawnVector = new Vector2(1, -1);
            }
            else
            {
                _pawnVector = new Vector2(1, 1);
            }
        }
        else if (_pieceType == "king")
        {
            _id = _player * 10 + 1;
        }
        else if (_pieceType == "queen")
        {
            _id = _player * 10 + 2;
        }
        else if (_pieceType == "rook")
        {
            _id = _player * 10 + 3;
        }
        else if (_pieceType == "bishop")
        {
            _id = _player * 10 + 4;
        }
        else if (_pieceType == "knight")
        {
            _id = _player * 10 + 5;
        }

        if (_player == 2)
        {
            Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.Texture = _textures.GetBlackTexture(_pieceType);
        }
        else if (_player == 1)
        {
            Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.Texture = _textures.GetWhiteTexture(_pieceType);
        }

        _movement = (PackedScene)ResourceLoader.Load("res://scenes/scenery/movement.tscn");
        _capture = (PackedScene)ResourceLoader.Load("res://scenes/scenery/capture.tscn");
        _promotion = (PackedScene)ResourceLoader.Load("res://scenes/promotion_selection.tscn");

        TileMap board = GetNode<TileMap>("../..");
        Node2D master = GetNode<Node2D>("../../..");
        Node2D playerController = GetNode<Node2D>("..");

        Connect("pieceSelected", new Callable(master, "DisableMovement"));
        Connect("pieceMoved", new Callable(master, "UpdateBoard"));
        Connect("updateCheck", new Callable(master, "Check"));
        Connect("clearEnPassant", new Callable(master, "ClearEnPassant"));
        Connect("updateTiles", new Callable(board, "UpdateTiles"));
        Connect("clearDynamicTiles", new Callable(board, "ClearDynamicTiles"));
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
            if (_turn == _player)
            {
                EmitSignal(SignalName.pieceSelected);
                EmitSignal(SignalName.updateTiles, Position, new Vector2I(0, 3), Name);
                if (_pieceType == "pawn")
                {
                    PawnMovement();
                }
                else if (_pieceType == "knight")
                {
                    KnightMovement();
                }
                else if (_pieceType == "king")
                {
                    KingMovement();
                }
                else
                {
                    if (_pieceType == "queen" || _pieceType == "rook")
                    {
                        StraightMove();
                    }

                    if (_pieceType == "queen" || _pieceType == "bishop")
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

            if (_firstMovement == true)
            {
                movePos = Position + new Vector2(CellPixels, CellPixels) * _pawnVector;
                CheckMovement();

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _pawnVector;
                CheckMovement();

                for (int i = 1; i <= 2; i++)
                {
                    int moveCheck;
                    int blockedPosition;
                    int positionSituation;


                    movePos = Position + i * new Vector2(0, CellPixels) * _pawnVector;
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);

                    if (!_isInCheck || positionSituation == SeesEnemyKing)
                    {
                        if (blockedPosition != 0)
                        {
                            break;
                        }
                        Move(movePos);
                    }
                }
            }
            else
            {                
                movePos = Position + new Vector2(0, CellPixels) * _pawnVector;
                CheckMovement();

                movePos = Position + new Vector2(CellPixels, CellPixels) * _pawnVector;
                CheckMovement();

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _pawnVector;
                CheckMovement();
            }

            void CheckMovement()
            {
                bool notOutOfBounds;
                int moveCheck;
                int blockedPosition;
                int positionSituation;
                notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

                if (movePos.X != Position.X && notOutOfBounds)
                {
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);

                    if (!_isInCheck || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        if (blockedPosition != 0 && Math.Abs(blockedPosition) != _player)
                        {
                            CapturePos(movePos);
                        }
                    }
                    else if (blockedPosition < 0)
                    {
                        positionSituation = (int)CheckArrayCheck.Call(movePos + new Vector2(0, 32) * -_pawnVector);

                        if ((positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees))
                        {
                            CapturePos(movePos);
                        }
                    }
                }
                else if (notOutOfBounds)
                {
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);

                    if (!_isInCheck || positionSituation == SeesEnemyKing)
                    {
                        if (blockedPosition <= 0)
                        {
                            Move(movePos);
                        }
                    }
                }
            }
        }

        void KnightMovement()
        {
            Vector2 movePos;

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            if (!_isInCheck)
            {
                CheckMovement();
            }
            else
            {
                MovementInCheck();
            }

            void CheckMovement()
            {
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                if (notOutOfBounds)
                {
                    int moveCheck = (int)MovementCheck.Call(movePos);
                    int blockedPosition = moveCheck / 10;

                    if (blockedPosition <= 0)
                    {
                        Move(movePos);
                    }
                    else if (blockedPosition > 0 && blockedPosition != _player)
                    {
                        CapturePos(movePos);
                    }
                }
            }

            void MovementInCheck()
            {
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                if (notOutOfBounds)
                {
                    int moveCheck = (int)MovementCheck.Call(movePos);
                    int blockedPosition = moveCheck / 10;
                    int positionSituation = (int)CheckArrayCheck.Call(movePos);

                    bool canTakeAttackingPiece = positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees;

                    if (blockedPosition <= 0 && positionSituation == SeesEnemyKing)
                    {
                        Move(movePos);
                    }
                    else if (blockedPosition > 0 && blockedPosition != _player && canTakeAttackingPiece)
                    {
                        CapturePos(movePos);
                    }
                }
            }
        }

        void KingMovement()
        {
            Vector2 movePos;

            movePos = Position - new Vector2(0, CellPixels);
            CheckMovement();

            movePos = Position + new Vector2(0, CellPixels);
            CheckMovement();

            movePos = Position - new Vector2(CellPixels, 0);
            CheckMovement();

            movePos = Position + new Vector2(CellPixels, 0);
            CheckMovement();

            movePos = Position - new Vector2(CellPixels, CellPixels);
            CheckMovement();

            movePos = Position + new Vector2(CellPixels, CellPixels);
            CheckMovement();

            movePos = Position - new Vector2(CellPixels, -CellPixels);
            CheckMovement();

            movePos = Position + new Vector2(CellPixels, -CellPixels);
            CheckMovement();

            if (_firstMovement && !_isInCheck)
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
                    movePos = Position + i * new Vector2(CellPixels, 0);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);

                    notRook = blockedPosition == _player && checkId != 3;
                    enemyPiece = blockedPosition != 0 && blockedPosition != _player;

                    if (notRook || enemyPiece || positionSituation == Path)
                    {
                        GD.Print($"Castling not initiated, {blockedPosition}, {checkId}, {movePos}, {positionSituation}");
                        break;
                    }
                    else if (blockedPosition == _player && checkId == 3)
                    {
                        GD.Print("Castling initiated");
                        EmitSignal(SignalName.castlingSetup, movePos);
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CellPixels, 0);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds) 
                    { 
                        break; 
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);

                    notRook = blockedPosition == _player && checkId != 3;
                    enemyPiece = blockedPosition != 0 && blockedPosition != _player;

                    if (notRook || enemyPiece || positionSituation == Path)
                    {
                        GD.Print($"Castling not initiated, {blockedPosition}, {checkId}, {movePos}, {positionSituation}");
                        break;
                    }
                    else if (blockedPosition == _player && checkId == 3)
                    {
                        GD.Print("Castling initiated");
                        EmitSignal(SignalName.castlingSetup, movePos);
                    }
                }
            }

            void CheckMovement()
            {
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                
                if (notOutOfBounds)
                {
                    int moveCheck = (int)MovementCheck.Call(movePos);
                    int blockedPosition = moveCheck / 10;
                    int checkPosition = (int)CheckArrayCheck.Call(movePos);

                    if (blockedPosition <= 0 && checkPosition != SeesEnemyKing && checkPosition != Path)
                    {
                        Move(movePos);
                    }
                    else if (blockedPosition > 0 && blockedPosition != _player && checkPosition != Potected && checkPosition != ProtectedAndSees)
                    {
                        CapturePos(movePos);
                    }
                }
            }
        }

        void StraightMove()
        {
            Vector2 movePos;
            int moveSituation;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
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
                movePos = Position + i * new Vector2(0, CellPixels);
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
                movePos = Position + i * new Vector2(CellPixels, 0);
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
                movePos = Position + i * new Vector2(CellPixels, 0);
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
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
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
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
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
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
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
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
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

        int MoveDiagonalStraight(Vector2 movePos)
        {
            bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                return 0;
            }

            int moveCheck = (int)MovementCheck.Call(movePos);
            int blockedPos = moveCheck / 10;
            int positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (!_isInCheck || positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees || blockedPos == _player)
            {
                if (blockedPos == _player)
                {
                    return 0;
                }
                else if ((!_isInCheck && blockedPos > 0) || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return 2;
                }
                else if (!_isInCheck || positionSituation == SeesEnemyKing)
                {
                    return 1;
                }
            }

            return 0;
        }

        void Move(Vector2 movePos)
        {
            CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
            AddChild(movement);
            GD.Print(movePos);
            movement.Position = movePos;
        }

        void CapturePos(Vector2 movePos)
        {
            CharacterBody2D capture = (CharacterBody2D)_capture.Instantiate();
            AddChild(capture);
            capture.Position = movePos;
        }
    }

    public void MovementSelected(Vector2 newPosition)
    {
        Vector2 oldPos;
        oldPos = Position;
        Position = newPosition;

        EmitSignal(SignalName.clearDynamicTiles);

        if (_firstMovement == true)
        {
            _firstMovement = false;
            if (_pieceType == "pawn" && (Position == oldPos + new Vector2(0, 2 * CellPixels) || Position == oldPos + new Vector2(0, -2 * CellPixels)))
            {
                EmitSignal(SignalName.pieceMoved, newPosition - new Vector2(0, CellPixels), oldPos, -_id, false);
                _enPassant = true;
            }
        }

        if (_pieceType == "king")
        {
            if (oldPos == Position + new Vector2(-2 * CellPixels, 0) || oldPos == Position + new Vector2(2 * CellPixels, 0))
            {
                EmitSignal(SignalName.moveRook, newPosition);
            }
        }

        if (_pieceType == "pawn" && (newPosition.Y < CellPixels || newPosition.Y > CellPixels * 7))
        {
            Control promotionSelection = (Control)_promotion.Instantiate();
            AddChild(promotionSelection);

            if (_player == 2)
            {
                promotionSelection.Scale = new Vector2(-1, -1);
                promotionSelection.Position = Position - new Vector2(CellPixels, 0);
            }
            else
            {
                promotionSelection.Position = Position + new Vector2(CellPixels, 0);
            }
            
            EmitSignal(SignalName.pieceMoved, newPosition, oldPos, _id, true);
        }
        else
        {
            EmitSignal(SignalName.pieceMoved, newPosition, oldPos, _id, false);
        }

        EmitSignal(SignalName.updateTiles, oldPos, new Vector2I(0, 1), Name);
        EmitSignal(SignalName.updateTiles, newPosition, new Vector2I(1, 1), Name);
    }

    public void ChangeTurn(int newTurn)
    {
        _turn = newTurn;
        _checkUpdatedCheck = false;
        _checkmate = false;

        if (_isInCheck && _pieceType == "king")
        {
            _isInCheck = false;
            EmitSignal(SignalName.playerInCheck, false);
        }

        if (_turn == 2)
        {
            Scale = new Vector2(-1, -1);
        } 
        else
        {
            Scale = new Vector2(1, 1);
        }

        if (_turn == _player && _pieceType == "pawn" && _enPassant)
        {
            EmitSignal(SignalName.clearEnPassant, _player);
            _enPassant = false;
        }

        UpdateCheck();
    }

    public void Capture(Vector2 _capturePos, CharacterBody2D _capture)
    {
        if (_pieceType == "pawn" && _enPassant && (_capturePos == Position - new Vector2 (0, CellPixels) || _capturePos == Position + new Vector2(0, CellPixels)))
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            EmitSignal(SignalName.clearEnPassant, _player);
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
        if (_player != _turn)
        {
            Vector2[] checkPosArray = new Vector2[8];
            if (_pieceType == "pawn")
            {
                GD.Print(_player, " ", _pieceType, " is check");
                PawnCheck();
            }
            else if (_pieceType == "knight")
            {
                GD.Print(_player, " ", _pieceType, " is check");
                KnightCheck();
            }
            else if (_pieceType == "king")
            {
                GD.Print(_player, " ", _pieceType, " is check");
                KingCheck();
            }
            else
            {
                if (_pieceType == "queen" || _pieceType == "rook")
                {
                    GD.Print(_player, " ", _pieceType, " is check");
                    StraightCheck();
                }

                if (_pieceType == "queen" || _pieceType == "bishop")
                {
                    GD.Print(_player, " ", _pieceType, " is check");
                    DiagonalCheck();
                }
            }

            void PawnCheck()
            {
                Vector2 movePos;
                int moveCheck;
                int blockedPosition;
                int checkId;
                bool notOutOfBounds;
                
                movePos = Position + new Vector2(CellPixels, CellPixels) * _pawnVector;
                notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

                if (notOutOfBounds)
                {
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SeesEnemyKing);
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SeesFriendlyPiece);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, Path);
                    }
                }

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _pawnVector;
                notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

                if (notOutOfBounds)
                {
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SeesEnemyKing);
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, SeesFriendlyPiece);
                    }
                    else
                    {
                        checkPosArray[1] = movePos;
                        CaptureCheck(checkPosArray, Position, 1, Path);
                    }
                }
            }

            void KnightCheck()
            {
                Vector2 movePos;

                movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
                MovePossibilityCheck();

                movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
                MovePossibilityCheck();

                movePos = Position - new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
                MovePossibilityCheck();

                movePos = Position + new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
                MovePossibilityCheck();

                movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
                MovePossibilityCheck();

                movePos = Position - new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
                MovePossibilityCheck();

                movePos = Position + new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
                MovePossibilityCheck();

                void MovePossibilityCheck()
                {
                    bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                    
                    if (notOutOfBounds)
                    {
                        int moveCheck = (int)MovementCheck.Call(movePos);
                        int blockedPosition = moveCheck / 10;
                        int checkId = moveCheck % 10;

                        if (blockedPosition != _player && checkId == 1)
                        {
                            checkPosArray[1] = movePos;
                            CaptureCheck(checkPosArray, Position, 1, SeesEnemyKing);
                        }
                        else if (blockedPosition == _player)
                        {
                            checkPosArray[1] = movePos;
                            CaptureCheck(checkPosArray, Position, 1, SeesFriendlyPiece);
                        }
                        else
                        {
                            checkPosArray[1] = movePos;
                            CaptureCheck(checkPosArray, Position, 1, Path);
                        }
                    }
                }
            }

            void KingCheck()
            {
                Vector2 movePos;

                movePos = Position - new Vector2(0, CellPixels);
                MovePossibilityCheck();

                movePos = Position + new Vector2(0, CellPixels);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CellPixels, 0);
                MovePossibilityCheck();

                movePos = Position + new Vector2(CellPixels, 0);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CellPixels, CellPixels);
                MovePossibilityCheck();

                movePos = Position + new Vector2(CellPixels, CellPixels);
                MovePossibilityCheck();

                movePos = Position - new Vector2(CellPixels, -CellPixels);
                MovePossibilityCheck();

                movePos = Position + new Vector2(CellPixels, -CellPixels);
                MovePossibilityCheck();

                void MovePossibilityCheck()
                {
                    bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                    
                    if (notOutOfBounds)
                    {
                        int moveCheck = (int)MovementCheck.Call(movePos);
                        int blockedPosition = moveCheck / 10;

                        if (blockedPosition == _player)
                        {
                            checkPosArray[1] = movePos;
                            CaptureCheck(checkPosArray, Position, 1, SeesFriendlyPiece);
                        }
                        else
                        {
                            checkPosArray[1] = movePos;
                            CaptureCheck(checkPosArray, Position, 1, Path);
                        }
                    }
                }
            }

            void StraightCheck()
            {
                Vector2 movePos;
                int moveCheck;
                int blockedPosition;
                int checkId;
                bool outOfBounds;

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(0, CellPixels); 
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;
                    
                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(0, CellPixels);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CellPixels, 0);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CellPixels, 0);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
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
                bool outOfBounds;

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CellPixels, CellPixels); 
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CellPixels, CellPixels);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = -1; i > -8; i--)
                {
                    movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
                        break;
                    }
                    else
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                    }
                }

                for (int i = 1; i < 8; i++)
                {
                    movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (outOfBounds)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }

                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;

                    if (blockedPosition > 0 && blockedPosition != _player && checkId != 1)
                    {
                        CaptureCheck(checkPosArray, Position, Math.Abs(i) - 1, Path);
                        break;
                    }
                    else if (blockedPosition != 0 && blockedPosition != _player && checkId == 1)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesEnemyKing);
                        break;
                    }
                    else if (blockedPosition == _player)
                    {
                        checkPosArray[Math.Abs(i)] = movePos;
                        CaptureCheck(checkPosArray, Position, Math.Abs(i), SeesFriendlyPiece);
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
                        GD.Print($"TEST check {Name} {_player} {i}");
                    }

                    if (checkSituation == SeesFriendlyPiece)
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
                    else if (checkSituation == SeesEnemyKing)
                    {
                        EmitSignal(SignalName.updateTiles, Position, new Vector2I(0, 2), Name);
                        if (i != maxIndex)
                        {
                            EmitSignal(SignalName.updateCheck, arrPos, checkSituation, false, false);
                        }
                        else
                        {
                            EmitSignal(SignalName.updateCheck, arrPos, KingInCheck, false, false);
                        }
                    }
                    else
                    {
                        EmitSignal(SignalName.updateCheck, arrPos, checkSituation, false, false);
                    }
                }
            }
        }

        _checkUpdatedCheck = true;
        EmitSignal(SignalName.checkUpdated);
    }

    public void CheckCheckState()
    {
        if (_pieceType == "king" && _turn == _player)
        {
            GD.Print(_player, " is checking wether he is on check");
            int check = (int)CheckCheck.Call(Position);

            if (check == KingInCheck)
            {
                _isInCheck = true;
                GD.Print(_player, " is in check");
                EmitSignal(SignalName.updateTiles, Position, new Vector2I(1, 2), Name);
                EmitSignal(SignalName.playerInCheck, true);
            }
        }
    }

    public void SetCheck(bool inCheck)
    {
        _isInCheck = inCheck;
        if (_isInCheck)
        {
            CheckCheckmate();
        }
    }

    public void CheckCheckmate()
    {
        if (_pieceType == "pawn")
        {
            PawnMateCheck();
        }
        else if (_pieceType == "knight")
        {
            KnightMateCheck();
        }
        else if (_pieceType == "king")
        {
            KingMateCheck();
        }
        else
        {
            if (_pieceType == "queen" || _pieceType == "rook")
            {
                StraightMateCheck();
            }

            if (_pieceType == "queen" || _pieceType == "bishop")
            {
                DiagonalMateCheck();
            }
        }

        EmitSignal(SignalName.checkmateCheck);

        void PawnMateCheck()
        {
            Vector2 movePos;
            int positionSituation;
            int moveCheck;
            int blockedPosition;

            if (_firstMovement == true)
            {
                for (int i = 1; i <= 2; i++)
                {
                    movePos = Position + new Vector2(CellPixels, CellPixels) * _pawnVector;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;

                    if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition < 0)
                    {
                        positionSituation = (int)CheckArrayCheck.Call(movePos + new Vector2(0, 32) * -_pawnVector);
                        if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                        {
                            return;
                        }
                    }

                    movePos = Position + new Vector2(-CellPixels, CellPixels) * _pawnVector;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);
                    moveCheck = (int)MovementCheck.Call(movePos);
                    blockedPosition = moveCheck / 10;

                    if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition < 0)
                    {
                        positionSituation = (int)CheckArrayCheck.Call(movePos + new Vector2(0, 32) * -_pawnVector);
                        if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                        {
                            return;
                        }
                    }

                    movePos = Position + i * new Vector2(0, CellPixels) * _pawnVector;
                    positionSituation = (int)CheckArrayCheck.Call(movePos);

                    if (positionSituation == SeesEnemyKing)
                    {
                        return;
                    }
                }
            }
            else
            {
                movePos = Position + new Vector2(0, CellPixels) * _pawnVector;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing)
                {
                    return;
                }

                movePos = Position + new Vector2(CellPixels, CellPixels) * _pawnVector;
                positionSituation = (int)CheckArrayCheck.Call(movePos);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;

                if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (blockedPosition < 0)
                {
                    positionSituation = (int)CheckArrayCheck.Call(movePos + new Vector2(0, 32) * -_pawnVector);
                    if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                }

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _pawnVector;
                positionSituation = (int)CheckArrayCheck.Call(movePos);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;

                if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (blockedPosition < 0)
                {
                    positionSituation = (int)CheckArrayCheck.Call(movePos + new Vector2(0, 32) * -_pawnVector);
                    if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                }
            }

            _checkmate = true;
        }

        void KnightMateCheck()
        {
            Vector2 movePos;
            int positionSituation;

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            positionSituation = (int)CheckArrayCheck.Call(movePos);

            if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
            {
                return;
            }

            _checkmate = true;
        }

        void KingMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;
            bool notOutOfBounds;

            movePos = Position - new Vector2(0, CellPixels);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position + new Vector2(0, CellPixels);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, 0);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, 0);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, CellPixels);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, CellPixels);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, -CellPixels);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, -CellPixels);
            moveCheck = (int)MovementCheck.Call(movePos);
            blockedPosition = moveCheck / 10;
            positionSituation = (int)CheckArrayCheck.Call(movePos);
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if ((positionSituation == 0 && blockedPosition <= 0 && notOutOfBounds) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
            {
                return;
            }

            _checkmate = true;
        }

        void StraightMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;

            _checkmate = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, 0);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, 0);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            _checkmate = true;
        }

        void DiagonalMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;

            _checkmate = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                moveCheck = (int)MovementCheck.Call(movePos);
                blockedPosition = moveCheck / 10;
                positionSituation = (int)CheckArrayCheck.Call(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
                else if (movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8 || blockedPosition == _player)
                {
                    break;
                }
            }

            _checkmate = true;
        }
    }

    public void FirstMovementCheck(Vector2 position)
    {
        GD.Print("Check castling");
        if (position == Position)
        {
            if (_firstMovement)
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

        GD.Print($"Castling {castlingAllowed} {_pieceType}");
        if (_pieceType == "king" && castlingAllowed)
        {
            Vector2I cell = board.LocalToMap(rookPosition);
            GD.Print($"Castling cell {cell}");
            if (cell.X == 0)
            {
                GD.Print("Long castling");
                movePos = Position + new Vector2(-2 * CellPixels, 0);
                CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                AddChild(movement);
                GD.Print(movePos);
                movement.Position = movePos;
            }
            else if (cell.X == 7)
            {
                GD.Print("Short castling");
                movePos = Position + new Vector2(2 * CellPixels, 0);
                CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                AddChild(movement);
                GD.Print(movePos);
                movement.Position = movePos;
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
            EmitSignal(SignalName.pieceMoved, newPosition, oldPos, _id, false);
        }
    }
}