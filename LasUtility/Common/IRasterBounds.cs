using NetTopologySuite.Geometries;

namespace LasUtility.Common
{
    internal interface IRasterBounds
    {
        int NumColumns { get; }
        int NumRows { get; }
        double CellWidth { get; }
        double CellHeight { get; }
        Envelope Extent { get; }

        RcIndex ProjToCell(Coordinate coordinate);
        Coordinate CellBottomLeftToProj(int iRow, int jCol);
        Coordinate CellTopRightToProj(int iRow, int jCol);
        //Coordinate CellCenterToProj(int iRow, int jCol);
    }
}