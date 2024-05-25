using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ches.Chess;
public partial class Piece : BasePiece, ISaveable
{
    [Signal]
    public delegate void PieceSelectedEventHandler();

    [Signal]
    public delegate void PieceMovedEventHandler(Vector2 position, Vector2 oldPosition, int player);

    [Signal]
    public delegate void TurnFinishedEventHandler(int turn);

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
    private static int _checkCount = 0;
    private static int _lastPieceID = 0;

    public int[] CaptureDirections { get => (int[])_captureDirections.Clone(); }
    public int ID { get => id; }

    [Export] private Direction _seesKing;

    [Export] private Godot.Collections.Dictionary<int, Texture2D> _textures;

    [Export] private PackedScene _movement;
    [Export] private PackedScene _capture;
    [Export] private PackedScene _promotion;

    public static Board GameBoard { get; set; }

    private Vector2 _playerDirectionVector;

    private Callable _checkPiece;

    public Callable CheckPiece { get => _checkPiece; set => _checkPiece = value; }

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

    public bool IsKing { get => _isKing; }
    public bool CanBeCastled { get => _canBeCastled; }

    public void SetFields(int player)
    {
        this.player = player;
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

        if (turn == 2)
        {
            Scale = new Vector2(-1, -1);
        }
        else if (turn == 1)
        {
            Scale = new Vector2(1, 1);
        }

        OriginalScale = Scale;
    }

    public void SetInitialTurn(int turn)
    {
        GameBoard.SetBoardSquares(Position, id);
        this.turn = turn;
    }

    protected override void Movement()
    {
        if (turn != player)
        {
            return;
        }

        EmitSignal(SignalName.PieceSelected);

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

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardSquares(movePos);
                int blockedPos = moveCheck / 1000;
                SquareSituation positionSituation = GameBoard.CheckCheckSquares(movePos);

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
                    bool kingCapture = _isKing && (positionSituation == SquareSituation.NotProtected || positionSituation == SquareSituation.NotProtectedAndSees);
                    bool normalCapture = !_isKing && (!_isInCheck || positionSituation == SquareSituation.ProtectedAndSees || positionSituation == SquareSituation.NotProtectedAndSees);

                    if (normalCapture || kingCapture) 
                    {
                        Movement capture = (Movement)_capture.Instantiate();
                        AddChild(capture);
                        capture.Position = movePos;
                        capture.MoveSelected += MovementSelected;
                        capture.SetCapture((Piece)_checkPiece.Call(moveCheck));
                        break;
                    }
                }
                else if (j <= _movementDirections[i] && blockedPos <= 0)
                {
                    bool kingMovement = _isKing && positionSituation == SquareSituation.Free;
                    bool normalMovement = !_isKing && (!_isInCheck || positionSituation == SquareSituation.SeesEnemyKing);

                    if (kingMovement || normalMovement)
                    {
                        Movement movement = (Movement)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                        movement.MoveSelected += MovementSelected;

                        if (_canEnPassant)
                        {
                            List<Vector2> enPassantPos = new List<Vector2>(occupiedPositions.Keys);
                            movement.SetEnPassant(enPassantPos, player);
                            movement.EnPassantGenerated += SetEnPassant;
                        }

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
                    int moveCheck = GameBoard.CheckBoardSquares(movePos);
                    int blockedPos = moveCheck / 1000;
                    SquareSituation positionSituation = GameBoard.CheckCheckSquares(movePos);
                    bool canTakePiece = _knightCapture && (!_isKing && (!_isInCheck || positionSituation == SquareSituation.ProtectedAndSees || positionSituation == SquareSituation.NotProtectedAndSees)) || (_isKing && (positionSituation == SquareSituation.NotProtected || positionSituation == SquareSituation.NotProtectedAndSees));

                    if (blockedPos <= 0 && (!_isInCheck || (positionSituation == SquareSituation.SeesEnemyKing && !_isKing) || (positionSituation == SquareSituation.Free && _isKing)) && _knightMovement)
                    {
                        Movement movement = (Movement)_movement.Instantiate();
                        AddChild(movement);
                        movement.Position = movePos;
                        movement.MoveSelected += MovementSelected;
                    }
                    else if (blockedPos > 0 && blockedPos != player && canTakePiece)
                    {
                        Movement capture = (Movement)_capture.Instantiate();
                        AddChild(capture);
                        capture.Position = movePos;
                        capture.SetCapture((Piece)_checkPiece.Call(moveCheck));
                        capture.MoveSelected += MovementSelected;
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
        movement.MoveSelected += MovementSelected;
        movement.SetCastling(target, movePos - new Vector2(CellPixels, CellPixels) * direction * _playerDirectionVector);

        if (occupiedPositions.ContainsKey(movement.Position))
        {
            occupiedPositions[movePos].QueueFree();
        }
    }

    public async void MovementSelected(Vector2 newPosition)
    {
        EmitSignal(SignalName.PieceSelected);

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
        EmitSignal(SignalName.TurnFinished, turn);
    }

    public void SetEnPassant(Vector2 pos)
    {
        _enPassant = true;
        EmitSignal(SignalName.PieceMoved, pos, new Vector2(-1, -1), -id);
    }

    public override void ChangeTurn(int turn)
    {
        base.ChangeTurn(turn);

        this.turn = turn;

        CheckKingVissibility();

        if (_isInCheck && _isKing && player == this.turn)
        {
            _isInCheck = false;
            EmitSignal(SignalName.PlayerInCheck, false);
        }
        else
        {
            CheckCheckState();
        }

        if (this.turn == 2)
        {
            Scale = new Vector2(-1, -1);
            OriginalScale = Scale;
        }
        else if (this.turn == 1)
        {
            Scale = new Vector2(1, 1);
            OriginalScale = Scale;
        }

        if (this.turn == player && _enPassant)
        {
            EmitSignal(SignalName.ClearEnPassant, player);
            _enPassant = false;
        }

        if (player != this.turn)
        {
            _seesKing = Direction.None;
            _lockedDirection = Array.Empty<int>();
        }
    }

    public void Capture()
    {
        GD.PrintRich($"[color=red]Capturing {this}[/color]");
        EmitSignal(SignalName.ClearEnPassant, player);
        GameBoard.SetBoardSquares(Position, 0);
        Delete();
    }

    public void UpdateCheck()
    {
        if (player != turn)
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
            SquareSituation situation = SquareSituation.Path;
            for (int j = 1; j <= _captureDirections[i]; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i] * _playerDirectionVector;

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardSquares(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos == player)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = SquareSituation.SeesFriendlyPiece;
                    break;
                }
                else if (blockedPos != player && blockedPos > 0)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);

                    if (blockingPiece.IsKing) 
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        situation = SquareSituation.SeesEnemyKing;
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

            UpdateCheckSquares(situation, controlledPositions);
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
                new Vector2I(-1, 2),
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
                SquareSituation situation = SquareSituation.Path;
                int moveCheck = GameBoard.CheckBoardSquares(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos == player)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                    situation = SquareSituation.SeesFriendlyPiece;
                }
                else if (blockedPos != player && blockedPos > 0)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);

                    if (blockingPiece.IsKing) 
                    {
                        controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                        situation = SquareSituation.SeesEnemyKing;
                    }
                }
                else if (blockedPos <= 0)
                {
                    controlledPositions.Add(new Vector2(movePos.X, movePos.Y));
                }

                UpdateCheckSquares(situation, controlledPositions);
            }
        }
    }

    public void CheckCheckState()
    {
        SquareSituation check = GameBoard.CheckCheckSquares(Position);

        GD.Print($"{player} is checking check");

        if (check == SquareSituation.KingInCheck)
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

                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i] * _playerDirectionVector;

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardSquares(movePos);
                int blockedPos = moveCheck / 1000;
                SquareSituation positionSituation = GameBoard.CheckCheckSquares(movePos);

                if (blockedPos == player)
                {
                    break;
                }
                else if (j <= _captureDirections[i] && (blockedPos > 0 || blockedPos < 0 && _canEnPassant))
                {
                    bool kingCapture = _isKing && (positionSituation == SquareSituation.NotProtected || positionSituation == SquareSituation.NotProtectedAndSees);
                    bool normalCapture = !_isKing && (!_isInCheck || positionSituation == SquareSituation.ProtectedAndSees || positionSituation == SquareSituation.NotProtectedAndSees);

                    if (normalCapture || kingCapture) 
                    {
                        GD.PrintRich($"[color=green]{Name} can capture[/color]");
                        return false;
                    }
                }
                else if (j <= _movementDirections[i] && blockedPos <= 0)
                {
                    bool kingMovement = _isKing && positionSituation == SquareSituation.Free;
                    bool normalMovement = !_isKing && (!_isInCheck || positionSituation == SquareSituation.SeesEnemyKing);

                    if (kingMovement || normalMovement)
                    {
                        GD.PrintRich($"[color=green]{Name} can move[/color]");
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
                    int moveCheck = GameBoard.CheckBoardSquares(movePos);
                    int blockedPos = moveCheck / 1000;
                    SquareSituation positionSituation = GameBoard.CheckCheckSquares(movePos);
                    bool canTakePiece = (!_isKing && (positionSituation == SquareSituation.ProtectedAndSees || positionSituation == SquareSituation.NotProtectedAndSees)) || (_isKing && (positionSituation == SquareSituation.NotProtected || positionSituation == SquareSituation.NotProtectedAndSees));

                    if (blockedPos <= 0 && (!_isInCheck || (positionSituation == SquareSituation.SeesEnemyKing && !_isKing)) && _knightMovement)
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

    public async void Castle(Vector2 newPosition)
    {
        Tween tween = CreateTween();

        Vector2 oldPos;
        oldPos = Position;

        tween.TweenProperty(this, "position", newPosition, .33f);
        await ToSignal(tween, Tween.SignalName.Finished);
        tween.Kill();

        EmitSignal(SignalName.PieceMoved, newPosition, oldPos, id);
    }

    private void CheckKingVissibility()
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
            for (int j = 1; j < 8; j++)
            {
                Vector2 movePos = Position + j * new Vector2(CellPixels, CellPixels) * directions[i];

                bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * GameBoard.Length || movePos.Y > CellPixels * GameBoard.Height;

                if (outOfBounds)
                {
                    break;
                }

                int moveCheck = GameBoard.CheckBoardSquares(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos == player)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);
                    if (blockingPiece.IsKing)
                    {
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

                int moveCheck = GameBoard.CheckBoardSquares(movePos);
                int blockedPos = moveCheck / 1000;

                if (blockedPos != player && blockedPos > 0)
                {
                    Piece blockingPiece = (Piece)_checkPiece.Call(moveCheck);
                    if (blockingPiece.CaptureDirections[i] >= j)
                    {
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

    private void UpdateCheckSquares(SquareSituation situation, List<Vector2> controlledPositions)
    {
        SquareSituation position = GameBoard.CheckCheckSquares(Position);

        if (situation == SquareSituation.SeesEnemyKing)
        {
            if (position == SquareSituation.Protected)
            {
                GameBoard.SetCheckSquares(Position, SquareSituation.ProtectedAndSees);
            }
            else
            {
                GameBoard.SetCheckSquares(Position, SquareSituation.NotProtectedAndSees);
            }
        }
        else if (position != SquareSituation.Protected && position != SquareSituation.ProtectedAndSees && position != SquareSituation.NotProtectedAndSees)
        {
            GameBoard.SetCheckSquares(Position, SquareSituation.NotProtected);
        }

        foreach (Vector2 controlledPosition in controlledPositions)
        {
            if (situation == SquareSituation.Path || (controlledPosition != controlledPositions.Last() && situation == SquareSituation.SeesFriendlyPiece))
            {
                GameBoard.SetCheckSquares(controlledPosition, SquareSituation.Path);
            }
            else if (controlledPosition != controlledPositions.Last() && situation == SquareSituation.SeesEnemyKing)
            {
                GameBoard.SetCheckSquares(controlledPosition, SquareSituation.SeesEnemyKing);
            }
            else if (situation == SquareSituation.SeesEnemyKing)
            {
                GameBoard.SetCheckSquares(controlledPosition, SquareSituation.KingInCheck);
            }
            else if (situation == SquareSituation.SeesFriendlyPiece)
            {
                SquareSituation oldSituation = GameBoard.CheckCheckSquares(controlledPosition);
                if (oldSituation == SquareSituation.NotProtected || oldSituation == SquareSituation.Free)
                {
                    GameBoard.SetCheckSquares(controlledPosition, SquareSituation.Protected);
                }
                else if (oldSituation == SquareSituation.NotProtectedAndSees)
                {
                    GameBoard.SetCheckSquares(controlledPosition, SquareSituation.ProtectedAndSees);
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
        }; // Save textures, checkPiece, isInCheck? 
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
        _firstMovement = (bool)data["FirstMovement"];
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
        Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = _textures[player];
    }

    public override string ToString()
    {
        return $"{Name} at ({(int)(Position.X / 32)}, {(int)(Position.Y / 32)})";
    }
}
