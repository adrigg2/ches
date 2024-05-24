namespace Ches.Chess;
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
