using NetTopologySuite.Geometries;

namespace LasUtility.Common
{
    [MessagePack.Union(0, typeof(RasterBounds))]
    public interface IRasterBounds
    {
        int ColumnCount { get; }
        int RowCount { get; }
        double CellWidth { get; }
        double CellHeight { get; }
        double MinX { get; }
        double MaxX { get; }
        double MinY { get; }
        double MaxY { get; }

        RcIndex ProjToCell(Coordinate coordinate);
        Coordinate CellBottomLeftToProj(int iRow, int jCol);
        Coordinate CellTopRightToProj(int iRow, int jCol);
        //Coordinate CellCenterToProj(int iRow, int jCol);
    }
}