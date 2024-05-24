using Godot;
using System;

public enum SquareSituation
{
    Free,
    SeesFriendlyPiece,
    Path,
    Protected,
    SeesEnemyKing,
    ProtectedAndSees,
    NotProtectedAndSees,
    NotProtected,
    KingInCheck
}
