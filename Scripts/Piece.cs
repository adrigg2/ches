using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches.Chess;
public partial class Piece : BasePiece
{
    private enum Direction
    {
        None,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        TopLeft
    }

    [Signal]
    public delegate void PieceSelectedEventHandler();

    [Signal]
    public delegate void PieceMovedEventHandler(Vector2 position, Vector2 oldPosition, int player, bool promotion);

    [Signal]
    public delegate void CheckUpdatedEventHandler();

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
    readonly int[] NotBlocked = Array.Empty<int>();
    readonly int[] Vertical = { 0, 4 };
    readonly int[] Horizontal = { 2, 6 };
    readonly int[] MainDiagonal = { 1, 5 };
    readonly int[] SecondaryDiagonal = { 3, 7 };

    [Export] private int[] _lockedDirection;
    [Export] private int _firstMovementBonus;
    [Export] private int[] _movementDirections; // 0 -> Up, 1 -> Up-Right, etc. Value indicates max number of cells
    [Export] private int[] _captureDirections;
    [Export] private int _castlingDistance;
    private static int _checkCount = 0;
    private static int _lastPieceID = 0;

    public int[] CaptureDirections { get => _captureDirections; }

    public static int Turn { get; set; } = 1;
    public int ID { get => id % 1000; }

    [Export] private Direction _seesKing;
    
    [Export] private PieceTextures _textures;

    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;
    [Export] private PackedScene _promotion;

    public static Board GameBoard { get; set; }

    private Vector2 _playerDirectionVector;

    [Export] private string _pieceType;

    private StringName _checkPiece;

    public StringName CheckPiece { get => _checkPiece; set => _checkPiece = value; }

    [Export] private bool _checkUpdatedCheck;
    private bool _firstMovement;
    [Export] private static bool _isInCheck = false;
    private bool _enPassant;
    [Export] private bool _canEnPassant;
    [Export] private bool _isKing;
    [Export] private bool _canCastle;
    [Export] private bool _canBeCastled;
    [Export] private bool _knightMovement;
    [Export] private bool _knightCapture;

    public bool CheckUpdatedCheck { get => _checkUpdatedCheck; }
    public bool IsKing { get => _isKing; }
    public bool CanBeCastled { get => _canBeCastled; }

    public void SetFields(int player)
    {
        this.player = player;
        _checkUpdatedCheck = false;
        _firstMovement = true;
        _enPassant = false;
    }

    public override void _Ready()
    {
        AddToGroup("pieces");
        AddToGroup("to_save");

        if (player == 1)
        {
            _playerDirectionVector = new Vector2(-1, -1);
            AddToGroup("white_pieces");
        }
        else
        {
            _playerDirectionVector = new Vector2(1, 1);
            AddToGroup("black_pieces");
        }

        id = player * 1000 + _lastPieceID;
        _lastPieceID++;

        if (player == 2)
        {
            GD.Print("Setting texture");
            Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.Texture = _textures.GetBlackTexture(_pieceType);
        }
        else if (player == 1)
        {
            GD.Print("Setting texture");
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

        SetInitialTurn();
    }

    public void SetInitialTurn()
    {
        GameBoard.SetBoardCells(Position, id);
    }

    protected override void Movement()
    {
        if (Turn != player)
        {
            return;
        }

        EmitSignal(SignalName.PieceSelected);

        GD.Print("Generating Movements");

        Vector2I[] directions =
        {
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(1, 0),
            new Vector2I(1, -1),
            new Vector2I(0, -1),
            new Vector2I(-1, -1),
            new Vector2I(-1, 0),
            new Vector2I(-1, 1)
        };

        for (int i = 0; i < directions.Length; i++)
        {
            if (_firstMovement && _movementDirections[i] > 0)
            {
                _movementDirections[i] += _firstMovementBonus;
            }

            List<Vector2> occupiedPositions = new();

            for (int j = 1; j <= GameBoard.Length || j <= GameBoard.Height; j++)
            {
                if (!_lockedDirection.IsEmpty() && !_lockedDirection.Contains(i))
                {
                    break;
                }

                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i] * _playerDirectionVector;
                GD.Print($"MovementPosition: {movePos}");

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print("OutOfBounds");
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;
                CellSituation positionSituation = GameBoard.CheckCheckCells(movePos);

                if (blockedPos == player)
                {
                    if (_canCastle)
                    {
                        Piece friendlyPiece = (Piece)Call(_checkPiece, moveCheck);
                        if (friendlyPiece.CanBeCastled)
                        {
                            GenerateCastling(directions[i], occupiedPositions);
                        }
                    }
                    break;
                }
                else if (j <= _captureDirections[i] && (blockedPos > 0 || blockedPos < 0 && _canEnPassant))
                {
                    bool kingCapture = _isKing && (positionSituation == CellSituation.NotProtected || positionSituation == CellSituation.NotProtectedAndSees);
                    bool normalCapture = !_isKing && (!_isInCheck || positionSituation == CellSituation.ProtectedAndSees || positionSituation == CellSituation.NotProtectedAndSees);

                    if (normalCapture || kingCapture) 
                    {
                        GD.Print("Capture is posible");
                        CharacterBody2D capture = (CharacterBody2D)_capture.Instantiate();
                        AddChild(capture);
                        capture.Position = movePos;
                        break;
                    }
                }
                else if (j <= _movementDirections[i] && blockedPos <= 0)
                {
                    bool kingMovement = _isKing && positionSituation == CellSituation.Free;
                    bool normalMovement = !_isKing && (!_isInCheck || positionSituation == CellSituation.SeesEnemyKing);

                    if (kingMovement || normalMovement)
                    {
                        GD.Print("Movement is posible");
                        CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                        occupiedPositions.Add(movePos);
                    }
                }
            }

            if (_firstMovement)
            {
                _movementDirections[i] -= _firstMovementBonus;
            }
        }

        if ((_knightMovement || _knightCapture) && !_lockedDirection.IsEmpty())
        {
            directions = new Vector2I[]
            {
            new Vector2I(-1, -2),
            new Vector2I(1, -2),
            new Vector2I(2, -1),
            new Vector2I(2, 1),
            new Vector2I(1, 2),
            new Vector2I(-1, 2),
            new Vector2I(-2, -1),
            new Vector2I(-2, 1)
            };

            foreach (Vector2I direction in directions)
            {
                Vector2 movePos = Position + new Vector2(CellPixels * direction[0], CellPixels * direction[1]);
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * GameBoard.Length && movePos.Y < CellPixels * GameBoard.Height;
                if (notOutOfBounds)
                {
                    int moveCheck = GameBoard.CheckBoardCells(movePos);
                    int blockedPos = moveCheck / 1000;
                    CellSituation positionSituation = GameBoard.CheckCheckCells(movePos);
                    bool canTakePiece = _knightCapture && (!_isKing && (!_isInCheck || positionSituation == CellSituation.ProtectedAndSees || positionSituation == CellSituation.NotProtectedAndSees)) || (_isKing && (positionSituation == CellSituation.NotProtected || positionSituation == CellSituation.NotProtectedAndSees));

                    if (blockedPos <= 0 && (!_isInCheck || (positionSituation == CellSituation.SeesEnemyKing && !_isKing) || (positionSituation == CellSituation.Free && _isKing)) && _knightMovement)
                    {
                        GD.Print("Movement is posible");
                        CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                    }
                    else if (blockedPos > 0 && blockedPos != player && canTakePiece)
                    {
                        GD.Print("Capture is posible");
                        CharacterBody2D capture = (CharacterBody2D)_capture.Instantiate();
                        AddChild(capture);
                        capture.Position = movePos;
                    }
                }
            } 
        }
    }

    private void GenerateCastling(Vector2I direction, Vector2[] occupiedPositions)
    {
        for (int i = 0, i < _castlingDistance, i++)
        {
            Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * direction * _playerDirectionVector;

            if (occupiedPositions.Contains(movePos))
            {
                continue;
            }
            else
            {
                CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                AddChild(movement);
                movement.Position = movePos;
            }
        }
    }

    public void MovementSelected(Vector2 newPosition)
    {
        Vector2 oldPos;
        oldPos = Position;
        Position = newPosition;

        _checkCount = 0;
        _firstMovement = false;

        EmitSignal(SignalName.ClearDynamicTiles);
        EmitSignal(SignalName.UpdateTiles, oldPos, new Vector2I(0, 1), Name);
        EmitSignal(SignalName.UpdateTiles, newPosition, new Vector2I(1, 1), Name);
        EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id, false);

        /*if (_firstMovement == true)
        {
            _firstMovement = false;
            if (_pieceType == "pawn" && Position == oldPos + new Vector2(0, 2 * CellPixels))
            {
                EmitSignal(SignalName.PieceMoved, newPosition - new Vector2(0, CellPixels), oldPos, -id, false);
                _enPassant = true;
            }
            else if (_pieceType == "pawn" && Position == oldPos + new Vector2(0, -2 * CellPixels))
            {
                EmitSignal(SignalName.PieceMoved, newPosition + new Vector2(0, CellPixels), oldPos, -id, false);
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

            if (player == 2)
            {
                promotionSelection.Scale = new Vector2(-1, -1);
                promotionSelection.Position = Position - new Vector2(CellPixels, 0);
            }
            else
            {
                promotionSelection.Position = Position + new Vector2(CellPixels, 0);
            }

            EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id, true);
        }
        else
        {
            EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id, false);
        }*/
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

        if (Turn == player && _pieceType == "pawn" && _enPassant)
        {
            EmitSignal(SignalName.ClearEnPassant, player);
            _enPassant = false;
        }

        if (player != Turn)
        {
            _seesKing = Direction.None;
            _lockedDirection = NotBlocked;
            UpdateCheck();
        }
    }

    public void Capture(Vector2 _capturePos, CharacterBody2D _capture)
    {
        if (_pieceType == "pawn" && _enPassant && (_capturePos == Position - new Vector2(0, CellPixels) || _capturePos == Position + new Vector2(0, CellPixels)))
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            EmitSignal(SignalName.ClearEnPassant, player);
            GameBoard.SetBoardCells(Position, 0);
            QueueFree();
        }
        else if (_capturePos == Position)
        {
            Connect("tree_exited", new Callable(_capture, "Captured"));
            EmitSignal(SignalName.ClearEnPassant, player);
            GameBoard.SetBoardCells(Position, 0);
            QueueFree();
        }
    }

    public virtual void UpdateCheck()
    {
        Vector2I[] directions =
        {
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(1, 0),
            new Vector2I(1, -1),
            new Vector2I(0, -1),
            new Vector2I(-1, -1),
            new Vector2I(-1, 0),
            new Vector2I(-1, 1)
        };

        for (int i = 0; i < directions.Length; i++)
        {
            List<Vector2> controlledPositions = new List<Vector2>();
            CellSituation situation = CellSituation.Path;
            for (int j = 1; j <= _captureDirections[i]; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos == player)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = CellSituation.SeesFriendlyPiece;
                    break;
                }
                else if (blockedPos != player && blockedPos != 0)
                {
                    Piece blockingPiece = (Piece)Call(_checkPiece, moveCheck);

                    if (blockingPiece.IsKing) 
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        situation = CellSituation.SeesEnemyKing;
                        break;
                    }
                    else
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        break;
                    }
                }
                else
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                }
            }

            UpdateCheckCells(situation, controlledPositions);
        }

        if (_knightCapture)
        {
            directions = new Vector2I[]
            {
            new Vector2I(-1, -2),
            new Vector2I(1, -2),
            new Vector2I(2, -1),
            new Vector2I(2, 1),
            new Vector2I(1, 2),
            new Vector2I(1, 2),
            new Vector2I(-2, -1),
            new Vector2I(-2, 1)
            };

            foreach (Vector2I direction in directions)
            {
                Vector2 movePos = Position + new Vector2(CellPixels * direction[0], CellPixels * direction[1]);
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * GameBoard.Length && movePos.Y < CellPixels * GameBoard.Height;

                if (!notOutOfBounds)
                {
                    continue;
                }

                List<Vector2> controlledPositions = new List<Vector2>();
                CellSituation situation = CellSituation.Path;
                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;
                int checkId = moveCheck % 10;

                if (blockedPos == player)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = CellSituation.SeesFriendlyPiece;
                }
                else if (blockedPos != player && blockedPos != 0 && checkId != 1)
                {
                    Piece blockingPiece = (Piece)Call(_checkPiece, moveCheck);

                    if (blockingPiece.IsKing) 
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        situation = CellSituation.SeesEnemyKing;
                    }
                    else
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    }
                }

                UpdateCheckCells(situation, controlledPositions);
            }
        }
    }

    public void CheckCheckState()
    {
        GD.Print($"Check check state {player} {Turn}");
        if (_pieceType == "king" && Turn == player)
        {
            GD.Print(player, " is checking wether he is on check");
            CellSituation check = GameBoard.CheckCheckCells(Position);

            if (check == CellSituation.KingInCheck)
            {
                _isInCheck = true;
                GD.Print(player, " is in check");
                EmitSignal(SignalName.UpdateTiles, Position, new Vector2I(1, 2), Name);
                EmitSignal(SignalName.PlayerInCheck, true);
            }
        }
    }

    public void SetCheck(bool inCheck)
    {
        _isInCheck = inCheck;
    }

    public bool CheckUnmovable()
    {
        if (_checkCount > 1)
        {
            return true;
        }

        Vector2I[] directions =
        {
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(1, 0),
            new Vector2I(1, -1),
            new Vector2I(0, -1),
            new Vector2I(-1, -1),
            new Vector2I(-1, 0),
            new Vector2I(-1, 1)
        };

        for (int i = 0; i < directions.Length; i++)
        {
            int movementAmount = _movementDirections[i];
            if (_firstMovement && movementAmount > 0)
            {
                movementAmount += _firstMovementBonus;
            }

            for (int j = 1; j <= movementAmount || j <= _captureDirections[i]; j++)
            {
                if (!_lockedDirection.IsEmpty() && !_lockedDirection.Contains(i))
                {
                    break;
                }

                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;
                CellSituation positionSituation = GameBoard.CheckCheckCells(movePos);

                if (blockedPos == player)
                {
                    break;
                }
                else if (j <= _captureDirections[i] && (blockedPos > 0 || blockedPos < 0 && _canEnPassant))
                {
                    bool kingCapture = _isKing && (positionSituation == CellSituation.NotProtected || positionSituation == CellSituation.NotProtectedAndSees);
                    bool normalCapture = !_isKing && (!_isInCheck || positionSituation == CellSituation.ProtectedAndSees || positionSituation == CellSituation.NotProtectedAndSees);

                    if (normalCapture || kingCapture) 
                    {
                        return false;
                    }
                }
                else if (j <= _movementDirections[i] && blockedPos <= 0)
                {
                    bool kingMovement = _isKing && positionSituation == CellSituation.Free;
                    bool normalMovement = !_isKing && (!_isInCheck || positionSituation == CellSituation.SeesEnemyKing);

                    if (kingMovement || normalMovement)
                    {
                        return false;
                    }
                }
            }
        }

        if (_knightMovement || _knightCapture)
        {
            directions = new Vector2I[]
            {
            new Vector2I(-1, -2),
            new Vector2I(1, -2),
            new Vector2I(2, -1),
            new Vector2I(2, 1),
            new Vector2I(1, 2),
            new Vector2I(1, 2),
            new Vector2I(-2, -1),
            new Vector2I(-2, 1)
            };

            foreach (Vector2I direction in directions)
            {
                Vector2 movePos = Position + new Vector2(CellPixels * direction[0], CellPixels * direction[1]);
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * GameBoard.Length && movePos.Y < CellPixels * GameBoard.Height;
                if (notOutOfBounds)
                {
                    int moveCheck = GameBoard.CheckBoardCells(movePos);
                    int blockedPos = moveCheck / 1000;
                    CellSituation positionSituation = GameBoard.CheckCheckCells(movePos);
                    bool canTakePiece = (!_isKing && (positionSituation == CellSituation.ProtectedAndSees || positionSituation == CellSituation.NotProtectedAndSees)) || (_isKing && (positionSituation == CellSituation.NotProtected || positionSituation == CellSituation.NotProtectedAndSees));

                    if (blockedPos <= 0 && (!_isInCheck || (positionSituation == CellSituation.SeesEnemyKing && !_isKing)) && _knightMovement)
                    {
                        return false;
                    }
                    else if (blockedPos > 0 && blockedPos != player && (!_isInCheck || canTakePiece) && _knightCapture)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
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
            EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id, false);
        }
    }

    public void CheckKingVissibility()
    {
        if (_isKing)
        {
            return;
        }

        Vector2I[] directions =
        {
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(1, 0),
            new Vector2I(1, -1),
            new Vector2I(0, -1),
            new Vector2I(-1, -1),
            new Vector2I(-1, 0),
            new Vector2I(-1, 1)
        };

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos == player)
                {
                    Piece blockingPiece = (Piece)Call(_checkPiece, moveCheck);
                    if (blockingPiece.IsKing)
                    {                        
                        _seesKing = (Direction)(i + 1);
                        CheckBlockedDirections();
                    }
                }
                else if (blockedPos > 0)
                {
                    break;
                }
            }
        }
    }

    public void CheckBlockedDirections()
    {
        Piece enemyPiece;
        Vector2 movePos;
        CellSituation positionSituation;
        bool outOfBounds;

        GD.Print($"{_pieceType} {player} checking locked direction");

        if (_seesKing == Direction.Top)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds bottom");
                    return;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction bottom");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction bottom");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[4] >= i)
                    {
                        _lockedDirection = Vertical;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.Bottom)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds top");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction top");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction top"); 
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[0] >= Math.Abs(i))
                    {
                        _lockedDirection = Vertical;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.Left)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(CellPixels, 0) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds right");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction right");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction right");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[2] >= i)
                    {
                        _lockedDirection = Horizontal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.Right)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(CellPixels, 0) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds left");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction left");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction left");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[6] >= Math.Abs(i))
                    {
                        _lockedDirection = Horizontal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.TopRight)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(CellPixels, -CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds bottom left");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction bottom left");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction bottom left");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[5] >= Math.Abs(i))
                    {
                        _lockedDirection = SecondaryDiagonal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.BottomLeft)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(CellPixels, -CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds top right");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction top right");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction top right");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[1] >= i)
                    {
                        _lockedDirection = SecondaryDiagonal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.TopLeft)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds bottom right");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction bottom right");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction bottom right");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[3] >= i)
                    {
                        _lockedDirection = MainDiagonal;
                    }
                    return;
                }
            }
        }
        else if (_seesKing == Direction.BottomRight)
        {
            for (int i = -1; i > -8; i--)
            {
                movePos = Position - i * new Vector2(CellPixels, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds top left");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == CellSituation.Free)
                {
                    GD.Print($"{_pieceType} {player} not locked direction top left");
                    return;
                }
                else if (positionSituation == CellSituation.Protected || positionSituation == CellSituation.NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction top left");
                    enemyPiece = (Piece)Call(_checkPiece, GameBoard.CheckBoardCells(movePos));

                    if (enemyPiece.CaptureDirections[7] >= Math.Abs(i))
                    {
                        _lockedDirection = MainDiagonal;
                    }
                    return;
                }
            }
        }
    }

    private void UpdateCheckCells(CellSituation situation, List<Vector2> controlledPositions)
    {
        CellSituation position = GameBoard.CheckCheckCells(Position);

        if (situation == CellSituation.SeesEnemyKing)
        {
            if (position == CellSituation.Protected)
            {
                GameBoard.SetCheckCells(Position, CellSituation.ProtectedAndSees);
            }
            else
            {
                GameBoard.SetCheckCells(Position, CellSituation.NotProtectedAndSees);
            }
        }
        else if (position != CellSituation.Protected && position != CellSituation.ProtectedAndSees)
        {
            GameBoard.SetCheckCells(Position, CellSituation.NotProtected);
        }

        foreach (Vector2 controlledPosition in controlledPositions)
        {
            if (situation == CellSituation.Path || (controlledPosition != controlledPositions.Last() && situation == CellSituation.SeesFriendlyPiece))
            {
                GameBoard.SetCheckCells(controlledPosition, CellSituation.Path);
            }
            else if (controlledPosition != controlledPositions.Last() && situation == CellSituation.SeesEnemyKing)
            {
                GameBoard.SetCheckCells(controlledPosition, CellSituation.SeesEnemyKing);
            }
            else if (controlledPosition == controlledPositions.Last())
            {
                if (situation == CellSituation.SeesEnemyKing)
                {
                    GameBoard.SetCheckCells(controlledPosition, CellSituation.KingInCheck);
                }
                else if (situation == CellSituation.SeesFriendlyPiece)
                {
                    CellSituation oldSituation = GameBoard.CheckCheckCells(controlledPosition);
                    if (oldSituation == CellSituation.NotProtected)
                    {
                        GameBoard.SetCheckCells(controlledPosition, CellSituation.Protected);
                    }
                    else if (oldSituation == CellSituation.NotProtectedAndSees)
                    {
                        GameBoard.SetCheckCells(controlledPosition, CellSituation.ProtectedAndSees);
                    }
                }
            }
        }
    }

    public void Delete()
    {
        foreach (StringName group in GetGroups())
        {
            RemoveFromGroup(group);
        }
        QueueFree();
    }

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
        {
            { "Filename", SceneFilePath },
            { "Parent", GetParent().GetPath() },
            { "PosX", Position.X },
            { "PosY", Position.Y },
            { "SeesKing", (int)_seesKing },
            { "LockedDirection", _lockedDirection },
            { "FirstMovementBonus", _firstMovementBonus },
            { "MovementDirections", _movementDirections },
            { "CaptureDirections", _captureDirections },
            { "PlayerDirectionX", _playerDirectionVector.X },
            { "PlayerDirectionY", _playerDirectionVector.Y },
            { "PieceType", _pieceType },
            { "CheckUpdatedCheck", _checkUpdatedCheck },
            { "FirstMovement", _firstMovement },
            { "EnPassant", _enPassant },
            { "CanEnPassant", _canEnPassant },
            { "IsKing", _isKing },
            { "CanCastle", _canCastle },
            { "CanBeCastled", _canBeCastled },
            { "CastlingDistance", _castlingDistance },
            { "KnightMovement", _knightMovement },
            { "KnightCapture", _knightCapture },
            { "Player", player },
            { "Id", id }
        };
    }

    public void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        Position = new Vector2((float)data["PosX"], (float)data["PosY"]);
        _seesKing = (Direction)(int)data["SeesKing"];
        _lockedDirection = (int[])data["LockedDirection"];
        _firstMovementBonus = (int)data["FirstMovementBonus"];
        _movementDirections = (int[])data["MovementDirections"];
        _captureDirections = (int[])data["CaptureDirections"];
        _castlingDistance = (int)data["CastlingDistance"];
        _playerDirectionVector = new Vector2((float)data["PlayerDirectionX"], (float)data["PlayerDirectionY"]);
        _pieceType = (string)data["PieceType"];
        _checkUpdatedCheck = (bool)data["CheckUpdatedCheck"];
        _firstMovement = (bool)data["Unmovable"];
        _enPassant = (bool)data["EnPassant"];
        _canEnPassant = (bool)data["CanEnPassant"];
        _isKing = (bool)data["IsKing"];
        _canCastle = (bool)data["CanCastle"];
        _canBeCastled = (bool)data["CanBeCastled"];
        _knightMovement = (bool)data["KnightMovement"];
        _knightCapture = (bool)data["KnightCapture"];
        player = (int)data["Player"];
        id = (int)data["Id"];
    }
}