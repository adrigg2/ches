using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches.Chess;
public partial class Piece : BasePiece
{
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
    const int SeesFriendlyPiece = 1;
    const int Path = 2;
    const int Protected = 3;
    const int SeesEnemyKing = 4;
    const int ProtectedAndSees = 5;
    const int NotProtectedAndSees = 6;
    const int NotProtected = 7;
    const int KingInCheck = 8;
    const int Top = 0;
    const int TopRight = 1;
    const int Right = 2;
    const int BottomRight = 3;
    const int Bottom = 4;
    const int BottomLeft = 5;
    const int Left = 6;
    const int TopLeft = 7;
    readonly int[] NotBlocked = Array.Empty<int>();
    readonly int[] Vertical = { 0, 4 };
    readonly int[] Horizontal = { 2, 6 };
    readonly int[] MainDiagonal = { 1, 5 };
    readonly int[] SecondaryDiagonal = { 3, 7 };

    [Export] private int _seesKing;
    [Export] private int[] _lockedDirection;
    private int _firstMovementBonus;
    private int[] _movementDirections; // 0 -> Up, 1 -> Up-Right, etc. Value indicates max number of cells
    private int[] _captureDirections;
    private static int _checkCount;

    public static int Turn { get; set; } = 1;

    [Export] private PieceTextures _textures;

    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;
    [Export] private PackedScene _promotion;

    public static Board GameBoard { get; set; }

    private Vector2 _playerDirectionVector;

    [Export]private string _pieceType;

    [Export] private bool _checkUpdatedCheck;
    [Export] private bool _unmovable;
    [Export] private bool _firstMovement;
    [Export] private static bool _isInCheck = false;
    private bool _enPassant;
    private bool _knightMovement;
    private bool _knightCapture;
    public bool Unmovable { get => _unmovable; }
    public bool CheckUpdatedCheck { get => _checkUpdatedCheck; }

    public void SetFields(int player, int[] movementDirections, int[] captureDirections, string pieceType, bool knightMovement = false, bool knightCapture = false, int firstMovementBonus = 0)
    {
        this.player = player;
        _seesKing = 0;
        _lockedDirection = NotBlocked;
        _firstMovementBonus = firstMovementBonus;
        _movementDirections = movementDirections;
        _captureDirections = captureDirections;
        _pieceType = pieceType;
        _checkUpdatedCheck = false;
        _unmovable = false;
        _firstMovement = true;
        _enPassant = false;
        _knightMovement = knightMovement;
        _knightCapture = knightCapture;
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

        if (_pieceType == "pawn")
        {
            id = player * 10;
            if (player == 1)
            {
                _movementDirections = new int[] { 0, 0, 0, 0, 1, 0, 0, 0 };
                _captureDirections = new int[] { 0, 0, 0, 1, 0, 1, 0, 0 };
            }
            else
            {
                _movementDirections = new int[] { 1, 0, 0, 0, 0, 0, 0, 0 };
                _captureDirections = new int[] { 0, 1, 0, 0, 0, 0, 0, 1 };
            }
            _firstMovementBonus = 1;
        }
        else if (_pieceType == "king")
        {
            id = player * 10 + 1;
        }
        else if (_pieceType == "queen")
        {
            id = player * 10 + 2;
        }
        else if (_pieceType == "rook")
        {
            id = player * 10 + 3;
        }
        else if (_pieceType == "bishop")
        {
            id = player * 10 + 4;
        }
        else if (_pieceType == "knight")
        {
            id = player * 10 + 5;
        }

        if (player == 2)
        {
            GD.Print("SEtting texture");
            Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.Texture = _textures.GetBlackTexture(_pieceType);
        }
        else if (player == 1)
        {
            GD.Print("SEtting texture");
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
        if (Turn != player || _unmovable)
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

            for (int j = 1; j <= _movementDirections[i] || j <= _captureDirections[i]; j++)
            {
                if (!_lockedDirection.IsEmpty() && !_lockedDirection.Contains(i))
                {
                    break;
                }

                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];
                GD.Print($"MovementPosition: {movePos}");

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print("OutOfBounds");
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 10;
                int positionSituation = GameBoard.CheckCheckCells(movePos);

                if (!_isInCheck || positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees || blockedPos == player)
                {
                    if (blockedPos == player)
                    {
                        GD.Print("Position blocked by friendly piece");
                        break;
                    }
                    else if (((!_isInCheck && blockedPos > 0) || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees) && j <= _captureDirections[i])
                    {
                        GD.Print("Capture is posible");
                        CharacterBody2D capture = (CharacterBody2D)_capture.Instantiate();
                        AddChild(capture);
                        capture.Position = movePos;
                        break;
                    }
                    else if ((!_isInCheck || positionSituation == SeesEnemyKing) && j <= _movementDirections[i])
                    {
                        GD.Print("Movement is posible");
                        CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                    }
                }
            }

            if (_firstMovement)
            {
                _movementDirections[i] -= _firstMovementBonus;
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
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                if (notOutOfBounds)
                {
                    int moveCheck = GameBoard.CheckBoardCells(movePos);
                    int blockedPos = moveCheck / 10;
                    int positionSituation = GameBoard.CheckCheckCells(movePos);
                    bool canTakePiece = positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees;

                    if (blockedPos <= 0 && (!_isInCheck || positionSituation == SeesEnemyKing) && _knightMovement)
                    {
                        GD.Print("Movement is posible");
                        CharacterBody2D movement = (CharacterBody2D)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                    }
                    else if (blockedPos > 0 && blockedPos != player && (!_isInCheck || canTakePiece) && _knightCapture)
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

        if (Turn == player && _pieceType == "pawn" && _enPassant)
        {
            EmitSignal(SignalName.ClearEnPassant, player);
            _enPassant = false;
        }

        if (player != Turn)
        {
            _seesKing = 0;
            _lockedDirection = NotBlocked;
            UpdateCheck();
        }
        else
        {
            _unmovable = CheckUnmovable();
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

    public void UpdateCheck()
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
            int situation = Path;
            for (int j = 1; j <= _captureDirections[i]; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 10;
                int checkId = moveCheck % 10;

                if (blockedPos == player)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = SeesFriendlyPiece;
                    break;
                }
                else if (blockedPos != player && checkId == 1)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = SeesEnemyKing;
                    break;
                }
                else if (blockedPos != player && blockedPos != 0 && checkId != 1)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    break;
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
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;

                if (!notOutOfBounds)
                {
                    continue;
                }

                List<Vector2> controlledPositions = new List<Vector2>();
                int situation = Path;
                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 10;
                int checkId = moveCheck % 10;

                if (blockedPos == player)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = SeesFriendlyPiece;
                }
                else if (blockedPos != player && checkId == 1)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = SeesEnemyKing;
                }
                else if (blockedPos != player && blockedPos != 0 && checkId != 1)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
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
            int check = GameBoard.CheckCheckCells(Position);

            if (check == KingInCheck)
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
        if (_isInCheck)
        {
            _unmovable = CheckUnmovable();
        }
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
            if (_firstMovement && _movementDirections[i] > 0)
            {
                _movementDirections[i] += _firstMovementBonus;
            }

            for (int j = 1; j <= _movementDirections[i] || j <= _captureDirections[i]; j++)
            {
                if (!_lockedDirection.IsEmpty() && !_lockedDirection.Contains(i))
                {
                    break;
                }

                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 10;
                int positionSituation = GameBoard.CheckCheckCells(movePos);

                if (!_isInCheck || positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees || blockedPos == player)
                {
                    if (blockedPos == player)
                    {
                        break;
                    }
                    else if (((!_isInCheck && blockedPos > 0) || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees) && j <= _captureDirections[i])
                    {
                        return false;
                    }
                    else if ((!_isInCheck || positionSituation == SeesEnemyKing) && j <= _movementDirections[i])
                    {
                        return false;
                    }
                }
            }

            if (_firstMovement)
            {
                _movementDirections[i] -= _firstMovementBonus;
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
                bool notOutOfBounds = movePos.X > 0 && movePos.Y > 0 && movePos.X < CellPixels * 8 && movePos.Y < CellPixels * 8;
                if (notOutOfBounds)
                {
                    int moveCheck = GameBoard.CheckBoardCells(movePos);
                    int blockedPos = moveCheck / 10;
                    int positionSituation = GameBoard.CheckCheckCells(movePos);
                    bool canTakePiece = positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees;

                    if (blockedPos <= 0 && (!_isInCheck || positionSituation == SeesEnemyKing) && _knightMovement)
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king top");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king bottom");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king left");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king right");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king top left");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king bottom right");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king bottom left");
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

            moveCheck = GameBoard.CheckBoardCells(movePos);
            blockedPosition = moveCheck / 10;
            checkId = moveCheck % 10;

            if (blockedPosition == player && checkId == 1)
            {
                GD.Print($"{_pieceType} {player} sees king top rights");
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

        GD.Print($"{_pieceType} {player} checking locked direction");

        if (_seesKing == Top)
        {
            for (int i = 1; i < 8; i++)
            {
                movePos = Position - i * new Vector2(0, CellPixels) * _playerDirectionVector;
                outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

                if (outOfBounds)
                {
                    GD.Print($"{_pieceType} {player} locked direction out of bounds bottom");
                    return;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction bottom");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction bottom");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds top");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction top");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction top"); 
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds right");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction right");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction right");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds left");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction left");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction left");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds bottom left");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction bottom left");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction bottom left");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds top right");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction top right");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction top right");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds bottom right");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction bottom right");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction bottom right");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

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
                    GD.Print($"{_pieceType} {player} locked direction out of bounds top left");
                    break;
                }

                positionSituation = GameBoard.CheckCheckCells(movePos);

                if (positionSituation == 0)
                {
                    GD.Print($"{_pieceType} {player} not locked direction top left");
                    return;
                }
                else if (positionSituation == Protected || positionSituation == NotProtected)
                {
                    GD.Print($"{_pieceType} {player} locked direction top left");
                    checkId = GameBoard.CheckBoardCells(movePos) % 10;

                    if (checkId == 2 || checkId == 4)
                    {
                        _lockedDirection = MainDiagonal;
                    }
                    return;
                }
            }
        }
    }

    private void UpdateCheckCells(int situation, List<Vector2> controlledPositions)
    {
        int position = GameBoard.CheckCheckCells(Position);

        if (situation == SeesEnemyKing)
        {
            if (position == Protected)
            {
                GameBoard.SetCheckCells(Position, ProtectedAndSees);
            }
            else
            {
                GameBoard.SetCheckCells(Position, NotProtectedAndSees);
            }
        }
        else if (position != Protected && position != ProtectedAndSees)
        {
            GameBoard.SetCheckCells(Position, NotProtected);
        }

        foreach (Vector2 controlledPosition in controlledPositions)
        {
            if (situation == Path || (controlledPosition != controlledPositions.Last() && situation == SeesFriendlyPiece))
            {
                GameBoard.SetCheckCells(controlledPosition, Path);
            }
            else if (controlledPosition != controlledPositions.Last() && situation == SeesEnemyKing)
            {
                GameBoard.SetCheckCells(controlledPosition, SeesEnemyKing);
            }
            else if (controlledPosition == controlledPositions.Last())
            {
                if (situation == SeesEnemyKing)
                {
                    GameBoard.SetCheckCells(controlledPosition, KingInCheck);
                }
                else if (situation == SeesFriendlyPiece)
                {
                    int oldSituation = GameBoard.CheckCheckCells(controlledPosition);
                    if (oldSituation == NotProtected)
                    {
                        GameBoard.SetCheckCells(controlledPosition, Protected);
                    }
                    else if (oldSituation == NotProtectedAndSees)
                    {
                        GameBoard.SetCheckCells(controlledPosition, ProtectedAndSees);
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
            { "SeesKing", _seesKing },
            { "LockedDirection", _lockedDirection },
            { "FirstMovementBonus", _firstMovementBonus },
            { "MovementDirections", _movementDirections },
            { "CaptureDirections", _captureDirections },
            { "PlayerDirectionX", _playerDirectionVector.X },
            { "PlayerDirectionY", _playerDirectionVector.Y },
            { "PieceType", _pieceType },
            { "CheckUpdatedCheck", _checkUpdatedCheck },
            { "Unmovable", _unmovable },
            { "FirstMovement", _firstMovement },
            { "EnPassant", _enPassant },
            { "KnightMovement", _knightMovement },
            { "KnightCapture", _knightCapture },
        };
    }

    public void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        Position = new Vector2((float)data["PosX"], (float)data["PosY"]);
        _seesKing = (int)data["SeesKing"];
        _lockedDirection = (int[])data["LockedDirection"];
        _firstMovementBonus = (int)data["FirstMovementBonus"];
        _movementDirections = (int[])data["MovementDirections"];
        _captureDirections = (int[])data["CaptureDirections"];
        _playerDirectionVector = new Vector2((float)data["PlayerDirectionX"], (float)data["PlayerDirectionY"]);
        _pieceType = (string)data["PieceType"];
        _checkUpdatedCheck = (bool)data["CheckUpdatedCheck"];
        _unmovable = (bool)data["Unmovable"];
        _firstMovement = (bool)data["Unmovable"];
        _enPassant = (bool)data["EnPassant"];
        _knightMovement = (bool)data["KnightMovement"];
        _knightCapture = (bool)data["KnightCapture"];
    }
}