using Godot;
using System;

namespace Chess;

[GlobalClass]
public partial class StraightMovement : Node2D
{
	[Export] private bool _up;
	[Export] private bool _right;
	[Export] private bool _down;
	[Export] private bool _left;

    [Export] private int _moveLimit;

    //void Move()
    //{
    //    Vector2 movePos;
    //    int moveSituation;

    //    for (int i = -1; i > -8; i--)
    //    {
    //        movePos = Position + i * new Vector2(0, CellPixels);
    //        moveSituation = MoveDiagonalStraight(movePos);

    //        if (moveSituation == 0)
    //        {
    //            break;
    //        }
    //        else if (moveSituation == 1)
    //        {
    //            Move(movePos);
    //        }
    //        else if (moveSituation == 2)
    //        {
    //            CapturePos(movePos);
    //            break;
    //        }
    //    }

    //    for (int i = 1; i < 8; i++)
    //    {
    //        movePos = Position + i * new Vector2(0, CellPixels);
    //        moveSituation = MoveDiagonalStraight(movePos);

    //        if (moveSituation == 0)
    //        {
    //            break;
    //        }
    //        else if (moveSituation == 1)
    //        {
    //            Move(movePos);
    //        }
    //        else if (moveSituation == 2)
    //        {
    //            CapturePos(movePos);
    //            break;
    //        }
    //    }

    //    for (int i = -1; i > -8; i--)
    //    {
    //        movePos = Position + i * new Vector2(CellPixels, 0);
    //        moveSituation = MoveDiagonalStraight(movePos);

    //        if (moveSituation == 0)
    //        {
    //            break;
    //        }
    //        else if (moveSituation == 1)
    //        {
    //            Move(movePos);
    //        }
    //        else if (moveSituation == 2)
    //        {
    //            CapturePos(movePos);
    //            break;
    //        }
    //    }

    //    for (int i = 1; i < 8; i++)
    //    {
    //        movePos = Position + i * new Vector2(CellPixels, 0);
    //        moveSituation = MoveDiagonalStraight(movePos);

    //        if (moveSituation == 0)
    //        {
    //            break;
    //        }
    //        else if (moveSituation == 1)
    //        {
    //            Move(movePos);
    //        }
    //        else if (moveSituation == 2)
    //        {
    //            CapturePos(movePos);
    //            break;
    //        }
    //    }
    //}

    //int MoveDiagonalStraight(Vector2 movePos)
    //{
    //    bool outOfBounds = movePos.X < 0 || movePos.Y < 0 || movePos.X > CellPixels * 8 || movePos.Y > CellPixels * 8;

    //    if (outOfBounds)
    //    {
    //        return 0;
    //    }

    //    int moveCheck = (int)MovementCheck.Call(movePos);
    //    int blockedPos = moveCheck / 10;
    //    int positionSituation = (int)CheckArrayCheck.Call(movePos);

    //    if (!_isInCheck || positionSituation == SeesEnemyKing || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees || blockedPos == _player)
    //    {
    //        if (blockedPos == _player)
    //        {
    //            return 0;
    //        }
    //        else if ((!_isInCheck && blockedPos > 0) || positionSituation == ProtectedAndSees || positionSituation == NotProtectedAndSees)
    //        {
    //            return 2;
    //        }
    //        else if (!_isInCheck || positionSituation == SeesEnemyKing)
    //        {
    //            return 1;
    //        }
    //    }

    //    return 0;
    //}
}
