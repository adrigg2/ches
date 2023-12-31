using Godot;
using System;

namespace Ches;
public partial class Piece : CharacterBody2D
{
    [Signal]
    public delegate void PieceSelectedEventHandler();

    [Signal]
    public delegate void PieceMovedEventHandler(Vector2 position, Vector2 oldPosition, int player, bool promotion);

    [Signal]
    public delegate void CheckUpdatedEventHandler();

    [Signal]
    public delegate void ZoneOfControlCheckedEventHandler(Vector2I position, int checkSituation, bool pieceCell, bool protectedPiece);

    [Signal]
    public delegate void PlayerInCheckEventHandler(bool isInCheck);

    [Signal]
    public delegate void CheckmateCheckEventHandler();

    [Signal]
    public delegate void UpdateTilesEventHandler(Vector2 position, Vector2I cellAtlas, string piece);

    [Signal]
    public delegate void ClearDynamicTilesEventHandler();

    [Signal]
    public delegate void CastlingSetupEventHandler(Vector2 position);

    [Signal]
    public delegate void AllowCastlingEventHandler(bool castlingAllowed, Vector2 position);

    [Signal]
    public delegate void MoveRookEventHandler(Vector2 position);

    [Signal]
    public delegate void ClearEnPassantEventHandler(int player);

    const int CellPixels = 32;
    const int SeesFriendlyPiece = 1;
    const int Path = 2;
    const int Protected = 3;
    const int SeesEnemyKing = 4;
    const int ProtectedAndSees = 5;
    const int NotProtectedAndSees = 6;
    const int NotProtected = 7;
    const int KingInCheck = 8;
    const int Top = 1;
    const int TopRight = 2;
    const int Right = 3;
    const int BottomRight = 4;
    const int Bottom = 5;
    const int BottomLeft = 6;
    const int Left = 7;
    const int TopLeft = 8;
    const int Vertical = 1;
    const int Horizontal = 2;
    const int MainDiagonal = 3;
    const int SecondaryDiagonal = 4;
    [Export] private int _id;
    [Export] private int _player;
    [Export] private int _seesKing;
    [Export] private int _lockedDirection;
    private static int _checkCount;
    public static int Turn { get; set; } = 1;
    [Export] public int CheckCountPublic { get => _checkCount; set => _checkCount = value; }
    [Export] public int TurnPublic { get => Turn; set => Turn = value; }

    [Export] private PieceTextures _textures;

    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;
    [Export] private PackedScene _promotion;

    public static TileMap Board { get; set; }

    private Vector2 _playerDirectionVector;

    private string _pieceType;

    [Export] private bool _checkUpdatedCheck = false;
    [Export] private bool _unmovable = false;
    [Export] private bool _firstMovement = true;
    [Export] private static bool _isInCheck = false;
    private bool _enPassant = false;
    public bool Unmovable { get => _unmovable; }
    public bool CheckUpdatedCheck { get => _checkUpdatedCheck; }

    public static int[,] BoardCells { get; set; }
    public static int[,] BoardCellsCheck { get; set; }

    public override void _Ready()
    {
        AddToGroup("pieces");
        AddToGroup("to_save");

        _pieceType = (string)GetMeta("Piece_Type");
        _player = (int)GetMeta("Player");

        if (_player == 1)
        {
            _playerDirectionVector = new Vector2(-1, -1);
            AddToGroup("white_pieces");
        }
        else
        {
            _playerDirectionVector = new Vector2(1, 1);
            AddToGroup("black_pieces");
        }

        if (_pieceType == "pawn")
        {
            _id = _player * 10;
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

        if (Turn == 2)
        {
            Scale = new Vector2(-1, -1);
        }
        else if (Turn == 1)
        {
            Scale = new Vector2(1, 1);
        }
    }

    public void SetInitialTurn()
    {
        TileMap board = GetNode<TileMap>("../..");
        Vector2I cell = board.LocalToMap(Position);
        BoardCells[cell.X, cell.Y] = _id;
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("piece_interaction"))
        {
            if (Turn == _player && !_unmovable)
            {
                EmitSignal(SignalName.PieceSelected);
                EmitSignal(SignalName.UpdateTiles, Position, new Vector2I(0, 3), Name);

                if (_pieceType != "king")
                {
                    CheckKingVissibility();
                }

                if (_pieceType == "pawn" && _checkCount <= 1)
                {
                    PawnMovement();
                }
                else if (_pieceType == "knight" && _checkCount <= 1 && _lockedDirection == 0)
                {
                    KnightMovement();
                }
                else if (_pieceType == "king")
                {
                    KingMovement();
                }
                else
                {
                    if ((_pieceType == "queen" || _pieceType == "rook") && _checkCount <= 1)
                    {
                        StraightMove();
                    }

                    if ((_pieceType == "queen" || _pieceType == "bishop") && _checkCount <= 1)
                    {
                        DiagonalMove();
                    }
                }
            }
        }

        void PawnMovement()
        {
            Vector2 movePos;

            if (_firstMovement == true)
            {
                movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                if (_lockedDirection == 0 || _lockedDirection == MainDiagonal)
                {
                    CheckMovement();
                }

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
                if (_lockedDirection == 0 || _lockedDirection == SecondaryDiagonal)
                {
                    CheckMovement();
                }

                for (int i = 1; i <= 2; i++)
                {
                    int moveCheck;
                    int blockedPosition;
                    int positionSituation;


                    movePos = Position + i * new Vector2(0, CellPixels) * _playerDirectionVector;
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (!_isInCheck || positionSituation == SeesEnemyKing)
                    {
                        if (blockedPosition != 0 || (_lockedDirection != 0 && _lockedDirection != Vertical))
                        {
                            break;
                        }
                        Move(movePos);
                    }
                }
            }
            else
            {
                movePos = Position + new Vector2(0, CellPixels) * _playerDirectionVector;
                if (_lockedDirection == 0 || _lockedDirection == Vertical)
                {
                    CheckMovement();
                }

                movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                if (_lockedDirection == 0 || _lockedDirection == MainDiagonal)
                {
                    CheckMovement();
                }

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
                if (_lockedDirection == 0 || _lockedDirection == SecondaryDiagonal)
                {
                    CheckMovement();
                }
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
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (!_isInCheck || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        if (blockedPosition != 0 && Math.Abs(blockedPosition) != _player)
                        {
                            CapturePos(movePos);
                        }
                    }
                    else if (blockedPosition < 0)
                    {
                        positionSituation = CheckCheckCells(movePos + new Vector2(0, 32) * -_playerDirectionVector);

                        if ((positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees))
                        {
                            CapturePos(movePos);
                        }
                    }
                }
                else if (notOutOfBounds)
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

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
                    int moveCheck = CheckBoardCells(movePos);
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
                    int moveCheck = CheckBoardCells(movePos);
                    int blockedPosition = moveCheck / 10;
                    int positionSituation = CheckCheckCells(movePos);

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

                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;
                    positionSituation = CheckCheckCells(movePos);

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
                        EmitSignal(SignalName.CastlingSetup, movePos);
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

                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    checkId = moveCheck % 10;
                    positionSituation = CheckCheckCells(movePos);

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
                        EmitSignal(SignalName.CastlingSetup, movePos);
                    }
                }
            }

            void CheckMovement()
            {
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

                if (notOutOfBounds)
                {
                    int moveCheck = CheckBoardCells(movePos);
                    int blockedPosition = moveCheck / 10;
                    int checkPosition = CheckCheckCells(movePos);

                    if (blockedPosition <= 0 && checkPosition != SeesEnemyKing && checkPosition != Path)
                    {
                        Vector2 oppositePos = (movePos - Position) * new Vector2(-1, -1) + Position;
                        notOutOfBounds = oppositePos.X > 0 && oppositePos.Y > 0 && oppositePos.X < CellPixels * 8 && oppositePos.Y < CellPixels * 8;
                        if (notOutOfBounds)
                        {
                            GD.Print("Illegal move check in bounds");
                            checkPosition = CheckCheckCells(oppositePos);
                            if (checkPosition != SeesEnemyKing && checkPosition != Path)
                            {
                                GD.Print("Illegal move check successful");
                                Move(movePos);
                            }
                        }
                        else
                        {
                            GD.Print("Illegal move check out of bounds");
                            Move(movePos);
                        }
                    }
                    else if (blockedPosition > 0 && blockedPosition != _player && checkPosition != Protected && checkPosition != ProtectedAndSees)
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
                if (_lockedDirection != 0 && _lockedDirection != Vertical)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != Vertical)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != Horizontal)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != Horizontal)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != MainDiagonal)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != MainDiagonal)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != SecondaryDiagonal)
                {
                    break;
                }

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
                if (_lockedDirection != 0 && _lockedDirection != SecondaryDiagonal)
                {
                    break;
                }

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

            int moveCheck = CheckBoardCells(movePos);
            int blockedPos = moveCheck / 10;
            int positionSituation = CheckCheckCells(movePos);

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

        _checkCount = 0;

        EmitSignal(SignalName.ClearDynamicTiles);
        EmitSignal(SignalName.UpdateTiles, oldPos, new Vector2I(0, 1), Name);
        EmitSignal(SignalName.UpdateTiles, newPosition, new Vector2I(1, 1), Name);

        if (_firstMovement == true)
        {
            _firstMovement = false;
            if (_pieceType == "pawn" && Position == oldPos + new Vector2(0, 2 * CellPixels))
            {
                EmitSignal(SignalName.PieceMoved, newPosition - new Vector2(0, CellPixels), oldPos, -_id, false);
                _enPassant = true;
            }
            else if (_pieceType == "pawn" && Position == oldPos + new Vector2(0, -2 * CellPixels))
            {
                EmitSignal(SignalName.PieceMoved, newPosition + new Vector2(0, CellPixels), oldPos, -_id, false);
                _enPassant = true;
            }
        }

        if (_pieceType == "king")
        {
            if (oldPos == Position + new Vector2(-2 * CellPixels, 0) || oldPos == Position + new Vector2(2 * CellPixels, 0))
            {
                EmitSignal(SignalName.MoveRook, newPosition);
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

            EmitSignal(SignalName.PieceMoved, newPosition, oldPos, _id, true);
        }
        else
        {
            EmitSignal(SignalName.PieceMoved, newPosition, oldPos, _id, false);
        }        
    }

    public void ChangeTurn()
    {
        _checkUpdatedCheck = false;

        if (_isInCheck && _pieceType == "king")
        {
            _isInCheck = false;
            EmitSignal(SignalName.PlayerInCheck, false);
        }

        if (Turn == 2)
        {
            Scale = new Vector2(-1, -1);
        }
        else if (Turn == 1)
        {
            Scale = new Vector2(1, 1);
        }

        if (Turn == _player && _pieceType == "pawn" && _enPassant)
        {
            EmitSignal(SignalName.ClearEnPassant, _player);
            _enPassant = false;
        }

        if (_player != Turn)
        {
            _seesKing = 0;
            _lockedDirection = 0;
            UpdateCheck();
        }
        else
        {
            CheckMobility();
        }
    }

    public void Capture(Vector2 _capturePos, CharacterBody2D _capture)
    {
        TileMap board = GetNode<TileMap>("../..");
        Vector2I cell = board.LocalToMap(Position);

        if (_pieceType == "pawn" && _enPassant && (_capturePos == Position - new Vector2(0, CellPixels) || _capturePos == Position + new Vector2(0, CellPixels)))
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            EmitSignal(SignalName.ClearEnPassant, _player);
            BoardCells[cell.X, cell.Y] = 0;
            QueueFree();
        }
        else if (_capturePos == Position)
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            EmitSignal(SignalName.ClearEnPassant, _player);
            BoardCells[cell.X, cell.Y] = 0;
            QueueFree();
        }
    }

    public void UpdateCheck()
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

            movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if (notOutOfBounds)
            {
                moveCheck = CheckBoardCells(movePos);
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

            movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
            notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

            if (notOutOfBounds)
            {
                moveCheck = CheckBoardCells(movePos);
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
                    int moveCheck = CheckBoardCells(movePos);
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
                    int moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

                moveCheck = CheckBoardCells(movePos);
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

            EmitSignal(SignalName.ZoneOfControlChecked, arrPos, checkSituation, true, false);

            for (int i = 1; i <= maxIndex; i++)
            {
                arrPos = board.LocalToMap(capturePos[i]);

                if (checkSituation == SeesFriendlyPiece)
                {
                    if (i != maxIndex)
                    {
                        EmitSignal(SignalName.ZoneOfControlChecked, arrPos, checkSituation, false, false);
                    }
                    else
                    {
                        EmitSignal(SignalName.ZoneOfControlChecked, arrPos, checkSituation, true, true);
                    }
                }
                else if (checkSituation == SeesEnemyKing)
                {
                    if (i != maxIndex)
                    {
                        EmitSignal(SignalName.ZoneOfControlChecked, arrPos, checkSituation, false, false);
                    }
                    else
                    {
                        EmitSignal(SignalName.ZoneOfControlChecked, arrPos, KingInCheck, false, false);
                    }
                }
                else
                {
                    EmitSignal(SignalName.ZoneOfControlChecked, arrPos, checkSituation, false, false);
                }
            }

            if (checkSituation == SeesEnemyKing)
            {
                GD.Print("----------UPDATE TILES CHECK----------");
                EmitSignal(SignalName.UpdateTiles, Position, new Vector2I(0, 2), Name);
                _checkCount++;
            }
        }


        _checkUpdatedCheck = true;
        EmitSignal(SignalName.CheckUpdated);
    }

    public void CheckCheckState()
    {
        GD.Print($"Check check state {_player} {Turn}");
        if (_pieceType == "king" && Turn == _player)
        {
            GD.Print(_player, " is checking wether he is on check");
            int check = CheckCheckCells(Position);

            if (check == KingInCheck)
            {
                _isInCheck = true;
                GD.Print(_player, " is in check");
                EmitSignal(SignalName.UpdateTiles, Position, new Vector2I(1, 2), Name);
                EmitSignal(SignalName.PlayerInCheck, true);
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
        _unmovable = false;

        if (_pieceType != "king" && _checkCount > 1)
        {
            _unmovable = true;
        }
        else if (_pieceType == "pawn")
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
        else if (_pieceType == "rook")
        {
            StraightMateCheck();
        }
        else if (_pieceType == "bishop")
        {
            DiagonalMateCheck();
        }
        else if (_pieceType == "queen")
        {
            bool firstCheck;
            bool secondCheck;

            StraightMateCheck();
            firstCheck = _unmovable;

            DiagonalMateCheck();
            secondCheck = _unmovable;

            if (!firstCheck || !secondCheck)
            {
                _unmovable = false;
            }
        }

        EmitSignal(SignalName.CheckmateCheck);

        void PawnMateCheck()
        {
            Vector2 movePos;
            int positionSituation;
            int moveCheck;
            int blockedPosition;
            bool outOfBounds;

            if (_firstMovement == true)
            {
                for (int i = 1; i <= 2; i++)
                {
                    movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == MainDiagonal))
                    {
                        positionSituation = CheckCheckCells(movePos);
                        moveCheck = CheckBoardCells(movePos);
                        blockedPosition = moveCheck / 10;

                        if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                        {
                            return;
                        }
                        else if (blockedPosition < 0)
                        {
                            positionSituation = CheckCheckCells(movePos + new Vector2(0, 32) * -_playerDirectionVector);
                            if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                            {
                                return;
                            }
                        }
                    }

                    movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == SecondaryDiagonal))
                    {
                        positionSituation = CheckCheckCells(movePos);
                        moveCheck = CheckBoardCells(movePos);
                        blockedPosition = moveCheck / 10;

                        if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                        {
                            return;
                        }
                        else if (blockedPosition < 0)
                        {
                            positionSituation = CheckCheckCells(movePos + new Vector2(0, 32) * -_playerDirectionVector);
                            if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                            {
                                return;
                            }
                        }
                    }

                    movePos = Position + i * new Vector2(0, CellPixels) * _playerDirectionVector;
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (!outOfBounds && _lockedDirection == Vertical)
                    {
                        positionSituation = CheckCheckCells(movePos);

                        if (positionSituation == SeesEnemyKing)
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                movePos = Position + new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && _lockedDirection == Vertical)
                {
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing)
                    {
                        return;
                    }
                }

                movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == MainDiagonal))
                {
                    positionSituation = CheckCheckCells(movePos);
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;

                    if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition < 0)
                    {
                        positionSituation = CheckCheckCells(movePos + new Vector2(0, 32) * -_playerDirectionVector);
                        if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                        {
                            return;
                        }
                    }
                }

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == SecondaryDiagonal))
                {
                    positionSituation = CheckCheckCells(movePos);
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;

                    if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition < 0)
                    {
                        positionSituation = CheckCheckCells(movePos + new Vector2(0, 32) * -_playerDirectionVector);
                        if (positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                        {
                            return;
                        }
                    }                    
                }
            }

            _unmovable = true;
        }

        void KnightMateCheck()
        {
            Vector2 movePos;
            int positionSituation;
            bool outOfBounds;

            if (_lockedDirection != 0)
            {
                _unmovable = true;
                return;
            }

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                {
                    return;
                }
            }

            _unmovable = true;
        }

        void KingMateCheck()
        {
            Vector2 movePos;

            movePos = Position - new Vector2(0, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(0, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, 0);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, 0);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, -CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, -CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            GD.Print($"King unmovable {_player} {DateTime.Now} check");
            _unmovable = true;

            bool checkPossibleMovement()
            {
                Vector2 oppositePos;
                int moveCheck;
                int blockedPosition;
                int positionSituation;
                int oppositePositionSituation;
                bool notOutOfBounds;
                bool oppositeNotOutOfBounds;

                oppositePos = (movePos - Position) * new Vector2(-1, -1) + Position;
                notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                oppositeNotOutOfBounds = oppositePos.X > 0 && oppositePos.Y > 0 && oppositePos.X < CellPixels * 8 && oppositePos.Y < CellPixels * 8;

                if (notOutOfBounds && oppositeNotOutOfBounds)
                {
                    GD.Print($"{movePos} {oppositePos} not out of bounds movecheck");

                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);
                    oppositePositionSituation = CheckCheckCells(oppositePos);

                    if ((positionSituation == 0 && oppositePositionSituation == 0 && blockedPosition <= 0) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
                    {
                        GD.Print($"{movePos} {oppositePos} available movecheck");
                        return true;
                    }
                }
                else if (notOutOfBounds)
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    GD.Print($"{oppositePos} out of bounds, {oppositePos} not out of bounds movecheck");
                    GD.Print($"{movePos} not out of bounds movecheck");

                    if ((positionSituation == 0 && blockedPosition <= 0) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
                    {
                        GD.Print($"{movePos} available movecheck");
                        return true;
                    }
                }

                GD.Print("Both out of bounds movecheck");
                return false;
            }
        }

        void StraightMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;
            bool outOfBounds;

            _unmovable = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Vertical || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Vertical || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, 0);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Horizontal || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, 0);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Horizontal || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            _unmovable = true;
        }

        void DiagonalMateCheck()
        {
            Vector2 movePos;
            int moveCheck;
            int blockedPosition;
            int positionSituation;
            bool outOfBounds;

            _unmovable = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == MainDiagonal || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == MainDiagonal || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == SecondaryDiagonal || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == SecondaryDiagonal || _lockedDirection == 0))
                {
                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if (positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            _unmovable = true;
        }
    }

    public void CheckMobility()
    {
        _unmovable = false;

        if (_pieceType != "king" && _checkCount > 1)
        {
            _unmovable = true;
        }
        else if (_pieceType == "pawn")
        {
            PawnMoveCheck();
        }
        else if (_pieceType == "knight")
        {
            KnightMoveCheck();
        }
        else if (_pieceType == "king")
        {
            KingMoveCheck();
        }
        else if (_pieceType == "rook")
        {
            StraightMoveCheck();
        }
        else if (_pieceType == "bishop")
        {
            DiagonalMoveCheck();
        }
        else if (_pieceType == "queen")
        {
            bool firstCheck;
            bool secondCheck;

            StraightMoveCheck();
            firstCheck = _unmovable;

            DiagonalMoveCheck();
            secondCheck = _unmovable;

            if (!firstCheck || !secondCheck)
            {
                _unmovable = false;
            }
        }

        EmitSignal(SignalName.CheckmateCheck);

        void PawnMoveCheck()
        {
            Vector2 movePos;
            int blockedPosition;
            bool outOfBounds;

            if (_firstMovement == true)
            {
                for (int i = 1; i <= 2; i++)
                {
                    movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == MainDiagonal))
                    {
                        blockedPosition = CheckBoardCells(movePos) / 10;

                        if (blockedPosition != 0 && blockedPosition != _player)
                        {
                            return;
                        }
                    }

                    movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == SecondaryDiagonal))
                    {
                        blockedPosition = CheckBoardCells(movePos) / 10;

                        if (blockedPosition != 0 && blockedPosition != _player)
                        {
                            return;
                        }
                    }

                    movePos = Position + i * new Vector2(0, CellPixels) * _playerDirectionVector;
                    outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                    if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == Vertical))
                    {
                        blockedPosition = CheckBoardCells(movePos) / 10;

                        if (blockedPosition == 0)
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                movePos = Position + new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == Vertical))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition == 0)
                    {
                        return;
                    }
                }

                movePos = Position + new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == MainDiagonal))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != 0 && blockedPosition != _player)
                    {
                        return;
                    }
                }

                movePos = Position + new Vector2(-CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == 0 || _lockedDirection == SecondaryDiagonal))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != 0 && blockedPosition != _player)
                    {
                        return;
                    }
                }
            }

            _unmovable = true;
        }

        void KnightMoveCheck()
        {
            Vector2 movePos;
            int blockedPosition;
            bool outOfBounds;

            if (_lockedDirection != 0)
            {
                _unmovable = true;
                return;
            }

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, -(2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2((2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position - new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(CellPixels, 0) + new Vector2(0, (2 * CellPixels));
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position - new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            movePos = Position + new Vector2(0, CellPixels) + new Vector2(-(2 * CellPixels), 0);
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (!outOfBounds)
            {
                blockedPosition = CheckBoardCells(movePos) / 10;

                if (blockedPosition != _player)
                {
                    return;
                }
            }

            _unmovable = true;
        }

        void KingMoveCheck()
        {
            Vector2 movePos;

            movePos = Position - new Vector2(0, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(0, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, 0);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, 0);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position - new Vector2(CellPixels, -CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            movePos = Position + new Vector2(CellPixels, -CellPixels);
            if (checkPossibleMovement())
            {
                return;
            }

            GD.Print($"King unmovable {_player} {DateTime.Now} move");
            _unmovable = true;

            bool checkPossibleMovement()
            {
                Vector2 oppositePos;
                int moveCheck;
                int blockedPosition;
                int positionSituation;
                int oppositePositionSituation;
                bool notOutOfBounds;
                bool oppositeNotOutOfBounds;

                oppositePos = (movePos - Position) * new Vector2(-1, -1) + Position;
                notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                oppositeNotOutOfBounds = oppositePos.X > 0 && oppositePos.Y > 0 && oppositePos.X < CellPixels * 8 && oppositePos.Y < CellPixels * 8;

                if (notOutOfBounds && oppositeNotOutOfBounds)
                {
                    GD.Print($"{movePos} {oppositePos} not out of bounds movecheck");

                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);
                    oppositePositionSituation = CheckCheckCells(oppositePos);

                    if ((positionSituation == 0 && oppositePositionSituation == 0 && blockedPosition <= 0) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
                    {
                        GD.Print($"{movePos} {oppositePos} available movecheck");

                        return true;
                    }
                }
                else if (notOutOfBounds)
                {
                    GD.Print($"{oppositePos} out of bounds, {oppositePos} not out of bounds movecheck");
                    GD.Print($"{movePos} not out of bounds movecheck");

                    moveCheck = CheckBoardCells(movePos);
                    blockedPosition = moveCheck / 10;
                    positionSituation = CheckCheckCells(movePos);

                    if ((positionSituation == 0 && blockedPosition <= 0) || positionSituation == NotProtectedAndSees || positionSituation == NotProtected)
                    {
                        GD.Print($"{movePos} available movecheck");
                        return true;
                    }
                }

                GD.Print("Both out of bounds movecheck");
                return false;
            }
        }

        void StraightMoveCheck()
        {
            Vector2 movePos;
            int blockedPosition;
            bool outOfBounds;

            _unmovable = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Vertical || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(0, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Vertical || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, 0);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Horizontal || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, 0);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == Horizontal || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            _unmovable = true;
        }

        void DiagonalMoveCheck()
        {
            Vector2 movePos;
            int blockedPosition;
            bool outOfBounds;

            _unmovable = false;

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == MainDiagonal || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == MainDiagonal || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = -1; i > -8; i--)
            {
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == SecondaryDiagonal || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < 8; i++)
            {
                movePos = Position + i * new Vector2(CellPixels, -CellPixels);
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (!outOfBounds && (_lockedDirection == SecondaryDiagonal || _lockedDirection == 0))
                {
                    blockedPosition = CheckBoardCells(movePos) / 10;

                    if (blockedPosition != _player)
                    {
                        return;
                    }
                    else if (blockedPosition == _player)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            _unmovable = true;
        }
    }

    public void FirstMovementCheck(Vector2 position)
    {
        if (position == Position)
        {
            if (_firstMovement)
            {
                GD.Print("castling");
                EmitSignal(SignalName.AllowCastling, true, Position);
            }
            else
            {
                GD.Print("not castling");
                EmitSignal(SignalName.AllowCastling, false, Position);
            }
        }
    }

    public void Castling(bool castlingAllowed, Vector2 rookPosition)
    {
        TileMap board = GetNode<TileMap>("../..");
        Vector2 movePos;

        if (_pieceType == "king" && castlingAllowed)
        {
            Vector2I cell = board.LocalToMap(rookPosition);
            if (cell.X == 0)
            {
                GD.Print("Long castling");
                movePos = Position + new Vector2(-2 * CellPixels, 0);
                CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                AddChild(movement);
                movement.Position = movePos;
            }
            else if (cell.X == 7)
            {
                GD.Print("Short castling");
                movePos = Position + new Vector2(2 * CellPixels, 0);
                CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                AddChild(movement);
                movement.Position = movePos;
            }
        }
    }

    public void Castle(Vector2 position, Vector2 newPosition)
    {
        if (position == Position)
        {
            Vector2 oldPos = Position;
            Position = newPosition;
            EmitSignal(SignalName.PieceMoved, newPosition, oldPos, _id, false);
        }
    }

    public void CheckKingVissibility()
    {
        Vector2 movePos;
        int moveCheck;
        int blockedPosition;
        int checkId;
        bool outOfBounds;

        for (int i = -1; i > -8; i--)
        {
            movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king top");
                _seesKing = Top;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = 1; i < 8; i++)
        {
            movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king bottom");
                _seesKing = Bottom;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = -1; i > -8; i--)
        {
            movePos = Position - i * new Vector2(CellPixels, 0) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king left");
                _seesKing = Left;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = 1; i < 8; i++)
        {
            movePos = Position - i * new Vector2(CellPixels, 0) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king right");
                _seesKing = Right;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = -1; i > -8; i--)
        {
            movePos = Position - i * new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king top left");
                _seesKing = TopLeft;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = 1; i < 8; i++)
        {
            movePos = Position - i * new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king bottom right");
                _seesKing = BottomRight;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = -1; i > -8; i--)
        {
            movePos = Position - i * new Vector2(CellPixels, -CellPixels) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king bottom left");
                _seesKing = BottomLeft;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }

        for (int i = 1; i < 8; i++)
        {
            movePos = Position - i * new Vector2(CellPixels, -CellPixels) * _playerDirectionVector;
            outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

            if (outOfBounds)
            {
                break;
            }

            moveCheck = CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == _player && checkId == 1)
            {
                GD.Print($"{_pieceType} {_player} sees king top rights");
                _seesKing = TopRight;
                CheckBlockedDirections();
                return;
            }
            else if (blockedPosition != 0)
            {
                break;
            }
        }
    }

    public void CheckBlockedDirections()
    {
        Vector2 movePos;
        int positionSituation;
        int checkId;
        bool outOfBounds;

        GD.Print($"{_pieceType} {_player} checking locked direction");

        if (_seesKing == Top)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds bottom");
                    return;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction bottom");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction bottom");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 3)
                    {
                        _lockedDirection = Vertical;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Bottom)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds top");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction top");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction top"); 
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 3)
                    {
                        _lockedDirection = Vertical;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Left)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(CellPixels, 0) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds right");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction right");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction right");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 3)
                    {
                        _lockedDirection = Horizontal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Right)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(CellPixels, 0) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds left");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction left");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction left");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 3)
                    {
                        _lockedDirection = Horizontal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == TopRight)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(CellPixels, -CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds bottom left");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction bottom left");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction bottom left");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 4)
                    {
                        _lockedDirection = SecondaryDiagonal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == BottomLeft)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(CellPixels, -CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds top right");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction top right");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction top right");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 4)
                    {
                        _lockedDirection = SecondaryDiagonal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == TopLeft)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds bottom right");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction bottom right");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction bottom right");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 4)
                    {
                        _lockedDirection = MainDiagonal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == BottomRight)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {_player} locked direction out of bounds top left");
                    break;
                }

                positionSituation = CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {_player} not locked direction top left");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {_player} locked direction top left");
                    checkId = CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 4)
                    {
                        _lockedDirection = MainDiagonal;
                    }
                    return;
                }
            }
        }
    }

    private int CheckBoardCells(Vector2 position)
    {
        Vector2I cell = Board.LocalToMap(position);
        return BoardCells[cell.X, cell.Y];
    }

    private int CheckCheckCells(Vector2 position)
    {
        Vector2I cell = Board.LocalToMap(position);
        return BoardCellsCheck[cell.X, cell.Y];
    }

    public void Delete()
    {
        foreach (StringName group in GetGroups())
        {
            RemoveFromGroup(group);
        }
        QueueFree();
    }
}