using NetTopologySuite.Geometries;
using System;
using System.Diagnostics.CodeAnalysis;

namespace LasUtility.Common
{
    internal class RasterBounds : IRasterBounds
    {
        public int NumRows { get; }
        public int NumColumns { get; }
        public Envelope Extent { get; }
        public double CellWidth { get { return Extent.Width / NumColumns; } }
        public double CellHeight { get { return Extent.Height / NumRows; } }

        public RasterBounds(int nRows, int nCols, Envelope extent)
        {
            NumRows = nRows;
            NumColumns = nCols;
            Extent = extent;
        }

        public Coordinate CellBottomLeftToProj(int iRow, int jCol)
        {
            if (iRow < 0 || iRow > (NumRows - 1) || jCol < 0 || jCol > (NumColumns - 1))
            {
                throw new ArgumentException("Cell indexes are out of range.");
            }

            double x = Extent.MinX + jCol * CellWidth;
            double y = Extent.MinY + iRow * CellHeight;

            return new Coordinate(x, y);
        }

        public Coordinate CellTopRightToProj(int iRow, int jCol)
        {
            Coordinate c = CellBottomLeftToProj(iRow, jCol);

            c.X += CellWidth;
            c.Y += CellHeight;

            return c;
        }

        public RcIndex ProjToCell(Coordinate coordinate)
        {
            if (!Extent.Contains(coordinate))
            {
                return RcIndex.Empty;
            }

            double x = coordinate.X - Extent.MinX;
            double y = coordinate.Y - Extent.MinY;

            int iRow = (int)(y / CellHeight);
            int jCol = (int)(x / CellWidth);

            return new RcIndex(iRow, jCol);
        }
    }
}