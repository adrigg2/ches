namespace Ches.Chess;
public enum CellSituation
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
