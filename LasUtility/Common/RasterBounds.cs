using MessagePack;
using NetTopologySuite.Geometries;
using System;
using System.Diagnostics.CodeAnalysis;

namespace LasUtility.Common
{
    [MessagePackObject]
    public class RasterBounds : IRasterBounds
    {
        [Key(0)]
        public int RowCount { get; }
        [Key(1)]
        public int ColumnCount { get; }

        [Key(2)]
        public double MinX { get; }
        [Key(3)]
        public double MinY { get; }
        [Key(4)]
        public double MaxX { get; }
        [Key(5)]
        public double MaxY { get; }

        [IgnoreMember]
        public double Width => MaxX - MinX;

        [IgnoreMember]
        public double Height => MaxY - MinY;

        [IgnoreMember]
        public double CellWidth { get { return Width / ColumnCount; } }
        [IgnoreMember]
        public double CellHeight { get { return Height / RowCount; } }

        /// <summary>
        /// Initializes a new raster
        /// </summary>
        /// <param name="iRowCount"> Number of rows in the raster. </param>
        /// <param name="iColCount"> Number of columns in the raster.</param>
        /// <param name="extent"> Coordinate bounds of the raster. Note that upper limits are not included in the bounds, i.e. [MinX, MaxX[ and [MinY, MaxY[ </param>
        public RasterBounds(int iRowCount, int iColCount, Envelope extent)
        {
            RowCount = iRowCount;
            ColumnCount = iColCount;
            MinX = extent.MinX;
            MinY = extent.MinY;
            MaxX = extent.MaxX;
            MaxY = extent.MaxY;
        }

        /// <summary>
        /// Initializes a new raster
        /// </summary>
        /// <param name="iRowCount"> Number of rows in the raster. </param>
        /// <param name="iColCount"> Number of columns in the raster. </param>
        /// <param name="dMinX"> Lower left x coordinate. This is included in the bounds, i.e. [MinX, MaxX[ </param>
        /// <param name="dMinY"> Lower left y coordinate. This is included in the bounds, i.e. [MinY, MaxY[ </param>
        /// <param name="dMaxX"> Upper right x coordinate. This is included in the bounds, i.e. [MinX, MaxX[ </param>
        /// <param name="dMaxY"> Upper right y coordinate. This is included in the bounds, i.e. [MinY, MaxY[ </param>
        public RasterBounds(int iRowCount, int iColCount, double dMinX, double dMinY, double dMaxX, double dMaxY)
        {
            RowCount = iRowCount;
            ColumnCount = iColCount;
            MinX = dMinX;
            MinY = dMinY;
            MaxX = dMaxX;
            MaxY = dMaxY;
        }

        public Coordinate CellBottomLeftToProj(int iRow, int jCol)
        {
            if (iRow < 0 || iRow > (RowCount - 1) || jCol < 0 || jCol > (ColumnCount - 1))
            {
                throw new ArgumentException("Cell indexes are out of range.");
            }

            double x = MinX + jCol * CellWidth;
            double y = MinY + iRow * CellHeight;

            return new Coordinate(x, y);
        }

        public Coordinate CellTopRightToProj(int iRow, int jCol)
        {
            Coordinate c = CellBottomLeftToProj(iRow, jCol);

            c.X += CellWidth;
            c.Y += CellHeight;

            return c;
        }

        /// <summary>
        /// Returns row and column index of that raster cell where the input coordinate is situated.
        /// </summary>
        /// <param name="c"> Coordinate </param>
        /// <returns> RowColumn index for the coordinate or RcIndex.Empty if outside bounds </returns>
        public RcIndex ProjToCell(Coordinate c)
        {
            // Validate input
            if (c.X < MinX || c.X >= MaxX || c.Y < MinY || c.Y >= MaxY)
            {
                return RcIndex.Empty;
            }

            double x = c.X - MinX;
            double y = c.Y - MinY;

            int iRow = (int)(y / CellHeight);
            int jCol = (int)(x / CellWidth);

            return new RcIndex(iRow, jCol);
        }
    }
}