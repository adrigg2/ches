using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches.Chess;
public partial class Piece : BasePiece
{
    private enum Direction
    {
        None = -1,
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
    public delegate void PieceMovedEventHandler(Vector2 position, Vector2 oldPosition, int player);

    [Signal]
    public delegate void TurnFinishedEventHandler(int turn);

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
    public delegate void ClearEnPassantEventHandler(int player);

    const int CellPixels = 32;

    [Export] private int[] _lockedDirection;
    [Export] private int _firstMovementBonus;
    [Export] private int[] _movementDirections; // 0 -> Up, 1 -> Up-Right, etc. Value indicates max number of cells
    [Export] private int[] _captureDirections;
    [Export] private int _castlingDistance;
    private int _turn;
    private static int _checkCount = 0;
    private static int _lastPieceID = 0;

    public int[] CaptureDirections { get => (int[])_captureDirections.Clone(); }
    public int ID { get => id % 1000; }

    [Export] private Direction _seesKing;

    [Export] private Godot.Collections.Dictionary<int, Texture2D> _textures;

    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;
    [Export] private PackedScene _promotion;

    private Tween _scaleTween;

    public static Board GameBoard { get; set; }

    private Vector2 _playerDirectionVector;
    private Vector2 _originalScale;

    private Callable _checkPiece;

    public Callable CheckPiece { get => _checkPiece; set => _checkPiece = value; }

    [Export] private bool _checkUpdatedCheck;
    private bool _firstMovement;
    [Export] private static bool _isInCheck = false;
    private bool _enPassant;
    [Export] private bool _canEnPassant;
    [Export] private bool _canBePromoted;
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

    public void PromotePiece(int[] movementDirections, int[] captureDirections, Godot.Collections.Dictionary<int, Texture2D> textures,
        bool knightMovement = false, bool knightCapture = false, bool canEnPassant = false)
    {
        _canBePromoted = false;
        _movementDirections = movementDirections;
        _captureDirections = captureDirections;
        _textures = textures;
        _canEnPassant = canEnPassant;
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

        id = player * 1000 + _lastPieceID;
        _lastPieceID++;

        UpdateSprite();

        if (_turn == 2)
        {
            Scale = new Vector2(-1, -1);
        }
        else if (_turn == 1)
        {
            Scale = new Vector2(1, 1);
        }

        _originalScale = Scale;
    }

    public void SetInitialTurn(int turn)
    {
        GameBoard.SetBoardCells(Position, id);
        _turn = turn;
    }

    public override void _MouseEnter()
    {
        if (player != _turn)
        {
            return;
        }

        Scale = _originalScale;

        if (_scaleTween != null)
        {
            _scaleTween.Kill();
        }
        _scaleTween = CreateTween();

        _scaleTween.TweenProperty(this, "scale", Scale * new Vector2(1.25f, 1.25f), .33f);
    }

    public override void _MouseExit()
    {
        if (player != _turn)
        {
            return;
        }

        if (_scaleTween != null)
        {
            _scaleTween.Kill();
        }
        _scaleTween = CreateTween();

        _scaleTween.TweenProperty(this, "scale", _originalScale, .33f);
    }

    protected override void Movement()
    {
        if (_turn != player)
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

            Dictionary<Vector2, Movement> occupiedPositions = new();

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
                        Piece friendlyPiece = (Piece)_checkPiece.Call(moveCheck);
                        if (friendlyPiece.CanBeCastled)
                        {
                            GenerateCastling(directions[i], occupiedPositions, friendlyPiece);
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
                        Movement movement = (Movement)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                        occupiedPositions.Add(movePos, movement);
                    }
                }
            }

            if (_firstMovement)
            {
                _movementDirections[i] -= _firstMovementBonus;
            }
        }

        if ((_knightMovement || _knightCapture) && _lockedDirection.IsEmpty())
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

    private void GenerateCastling(Vector2I direction, Dictionary <Vector2, Movement> occupiedPositions, Piece target)
    {
        Vector2 movePos = Position + _castlingDistance * new Vector2(CellPixels, CellPixels) * direction * _playerDirectionVector;
        Movement movement = (Movement)_movement.Instantiate();
        AddChild(movement);
        movement.Position = movePos;
        movement.SetCastling(target, movePos - new Vector2(CellPixels, CellPixels) * direction * _playerDirectionVector);

        if (occupiedPositions.Keys.Contains(movement.Position))
        {
            occupiedPositions[movePos].QueueFree();
        }
    }

    public async void MovementSelected(Vector2 newPosition)
    {
        Tween tween = CreateTween();

        Vector2 oldPos;
        oldPos = Position;

        tween.TweenProperty(this, "position", newPosition, .33f);
        await ToSignal(tween, Tween.SignalName.Finished);
        tween.Kill();


        _checkCount = 0;
        _firstMovement = false;

        if (_canBePromoted && (newPosition.Y < CellPixels || newPosition.Y > CellPixels * 7))
        {
            PromotionSelection promotionSelection = (PromotionSelection)_promotion.Instantiate();
            promotionSelection.PieceToPromote = this;
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

            await ToSignal(promotionSelection, PromotionSelection.SignalName.PiecePromoted);
        }

        EmitSignal(SignalName.ClearDynamicTiles);
        EmitSignal(SignalName.UpdateTiles, oldPos, new Vector2I(0, 1), Name);
        EmitSignal(SignalName.UpdateTiles, newPosition, new Vector2I(1, 1), Name);
        EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id);
        EmitSignal(SignalName.TurnFinished, _turn);

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

    public void ChangeTurn(int turn)
    {
        if (_scaleTween != null)
        {
            _scaleTween.Kill();
        }

        _turn = turn;
        _checkUpdatedCheck = false;

        CheckKingVissibility();

        if (_isInCheck && _isKing && player == _turn)
        {
            _isInCheck = false;
            EmitSignal(SignalName.PlayerInCheck, false);
        }
        else
        {
            CheckCheckState();
        }

        if (_turn == 2)
        {
            Scale = new Vector2(-1, -1);
            _originalScale = Scale;
        }
        else if (_turn == 1)
        {
            Scale = new Vector2(1, 1);
            _originalScale = Scale;
        }

        if (_turn == player && _enPassant)
        {
            EmitSignal(SignalName.ClearEnPassant, player);
            _enPassant = false;
        }

        if (player != _turn)
        {
            _seesKing = Direction.None;
            _lockedDirection = Array.Empty<int>();
        }
    }

    public void Capture(Vector2 _capturePos, CharacterBody2D _capture)
    {
        if (_enPassant && (_capturePos == Position - new Vector2(0, CellPixels) || _capturePos == Position + new Vector2(0, CellPixels)))
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
        if (player != _turn)
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

        for (int i = 0; i < directions.Length; i++)
        {
            List<Vector2> controlledPositions = new List<Vector2>();
            CellSituation situation = CellSituation.Path;
            for (int j = 1; j <= _captureDirections[i]; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i] * _playerDirectionVector;

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos == player)
                {
                    GD.Print($"{Name} is setting {new Vector2I((int)(movePos.X / 32), (int)(movePos.Y / 32))} 540");
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = CellSituation.SeesFriendlyPiece;
                    break;
                }
                else if (blockedPos != player && blockedPos != 0)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);

                    if (blockingPiece.IsKing) 
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        situation = CellSituation.SeesEnemyKing;
                        break;
                    }
                    else
                    {
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
                    GD.Print($"{Name} is setting {new Vector2I((int)(movePos.X / 32), (int)(movePos.Y / 32))} 601");
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = CellSituation.SeesFriendlyPiece;
                }
                else if (blockedPos != player && blockedPos != 0 && checkId != 1)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);

                    if (blockingPiece.IsKing) 
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        situation = CellSituation.SeesEnemyKing;
                    }
                }

                UpdateCheckCells(situation, controlledPositions);
            }
        }
    }

    public void CheckCheckState()
    {
        CellSituation check = GameBoard.CheckCheckCells(Position);

        GD.Print($"{player} is checking check");

        if (check == CellSituation.KingInCheck)
        {
            GD.Print($"{player} is in check");
            _isInCheck = true;
            EmitSignal(SignalName.UpdateTiles, Position, new Vector2I(1, 2), Name);
            EmitSignal(SignalName.PlayerInCheck, true);
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

    public void Castle(Vector2 newPosition)
    {
        Vector2 oldPos = Position;
        Position = newPosition;
        EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id);
    }

    private void CheckKingVissibility()
    {
        GD.Print($"Piece {Name} is checking king vissibility");

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
            for (int j = 1; j < 8; j++)
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
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);
                    if (blockingPiece.IsKing)
                    {
                        GD.Print($"Piece {Name} sees king at {(Direction)(i + 1)} (Position = {Position}; i = {i}; j = {j}; Direction = {directions[i]}; movePos = {movePos})");
                        _seesKing = (Direction)i;
                        CheckBlockedDirections();
                    }
                    break;
                }
                else if (blockedPos > 0)
                {
                    break;
                }
            }
        }
    }

    private void CheckBlockedDirections()
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

        for (int i = 0; i < 8; i++)
        {
            if (!(_seesKing == (Direction)i ||  _seesKing == (Direction)((i + 4) % 8)))
            {
                continue;
            }

            for (int j = 1; j < 8; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardCells(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos != player && blockedPos > 0)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);
                    if (blockingPiece.CaptureDirections[i] >= j)
                    {
                        GD.Print($"Piece {Name} is locked at {(Direction)i}");
                        _lockedDirection = new int[] { i, (i + 4) % 8 };
                    }
                    break;
                }
                else if (blockedPos > 0)
                {
                    break;
                }
            }
        }
    }

    private void UpdateCheckCells(CellSituation situation, List<Vector2> controlledPositions)
    {
        GD.Print($"{Name} is setting {situation} 873");

        CellSituation position = GameBoard.CheckCheckCells(Position);

        if (situation == CellSituation.SeesEnemyKing)
        {
            if (position == CellSituation.Protected)
            {
                GameBoard.SetCheckCells(Position, CellSituation.ProtectedAndSees, Name);
            }
            else
            {
                GameBoard.SetCheckCells(Position, CellSituation.NotProtectedAndSees, Name);
            }
        }
        else if (position != CellSituation.Protected && position != CellSituation.ProtectedAndSees && position != CellSituation.NotProtectedAndSees)
        {
            GameBoard.SetCheckCells(Position, CellSituation.NotProtected, Name);
        }

        foreach (Vector2 controlledPosition in controlledPositions)
        {
            GD.Print($"{Name} is setting {controlledPosition} with {situation} 895");
            if (situation == CellSituation.Path || (controlledPosition != controlledPositions.Last() && situation == CellSituation.SeesFriendlyPiece))
            {
                GameBoard.SetCheckCells(controlledPosition, CellSituation.Path, Name);
            }
            else if (controlledPosition != controlledPositions.Last() && situation == CellSituation.SeesEnemyKing)
            {
                GameBoard.SetCheckCells(controlledPosition, CellSituation.SeesEnemyKing, Name);
            }
            else
            {
                if (situation == CellSituation.SeesEnemyKing)
                {
                    GameBoard.SetCheckCells(controlledPosition, CellSituation.KingInCheck, Name);
                }
                else if (situation == CellSituation.SeesFriendlyPiece)
                {
                    CellSituation oldSituation = GameBoard.CheckCheckCells(controlledPosition);
                    if (oldSituation == CellSituation.NotProtected || oldSituation == CellSituation.Free)
                    {
                        GameBoard.SetCheckCells(controlledPosition, CellSituation.Protected, Name);
                    }
                    else if (oldSituation == CellSituation.NotProtectedAndSees)
                    {
                        GameBoard.SetCheckCells(controlledPosition, CellSituation.ProtectedAndSees, Name);
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
            { "CheckUpdatedCheck", _checkUpdatedCheck },
            { "FirstMovement", _firstMovement },
            { "EnPassant", _enPassant },
            { "CanEnPassant", _canEnPassant },
            { "CanBePromoted", _canBePromoted },
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
        _checkUpdatedCheck = (bool)data["CheckUpdatedCheck"];
        _firstMovement = (bool)data["Unmovable"];
        _enPassant = (bool)data["EnPassant"];
        _canEnPassant = (bool)data["CanEnPassant"];
        _canBePromoted = (bool)data["CanBePromoted"];
        _isKing = (bool)data["IsKing"];
        _canCastle = (bool)data["CanCastle"];
        _canBeCastled = (bool)data["CanBeCastled"];
        _knightMovement = (bool)data["KnightMovement"];
        _knightCapture = (bool)data["KnightCapture"];
        player = (int)data["Player"];
        id = (int)data["Id"];
    }

    public void UpdateSprite()
    {
        GD.Print("Setting texture");
        Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = _textures[player];
    }
}
