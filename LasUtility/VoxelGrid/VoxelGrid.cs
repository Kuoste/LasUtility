using DotSpatial.Data;
using DotSpatial.Topology;
using LasUtility.DEM;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;

namespace LasUtility.VoxelGrid
{
    public class VoxelGrid : IHeightMap
    {
        IRasterBounds _bounds;
        public Bin[][] _grid;

        public int nRows { get; private set; }
        public int nCols { get; private set; }

        public static VoxelGrid CreateGrid(int nRows, int nCols, double minX, double minY, double maxX, double maxY)
        {
            Extent extent = new Extent(minX, minY, maxX, maxY);
            VoxelGrid voxelGrid = new VoxelGrid();

            voxelGrid.nRows = nRows;
            voxelGrid.nCols = nCols;
            voxelGrid._bounds= new RasterBounds(nRows, nCols, extent);

            Bin[][] grid = new Bin[nRows][];

            //Console.WriteLine("Lowes index center coords are x "
            //    + voxelGrid._bounds.CellCenter_ToProj(0, 0).X + " y "
            //    + voxelGrid._bounds.CellCenter_ToProj(0, 0).Y);

            //Console.WriteLine("Lowest index bottomleft coords are x "
            //    + voxelGrid._bounds.CellBottomLeft_ToProj(0, 0).X + " y "
            //    + voxelGrid._bounds.CellBottomLeft_ToProj(0, 0).Y);

            //Console.WriteLine("Lowest index topright coords are x "
            //    + voxelGrid._bounds.CellTopRight_ToProj(0, 0).X + " y "
            //    + voxelGrid._bounds.CellTopRight_ToProj(0, 0).Y);

            //Console.WriteLine("Highest index center coords are x "
            //    + voxelGrid._bounds.CellCenter_ToProj(nRows - 1, nCols - 1).X + " y "
            //    + voxelGrid._bounds.CellCenter_ToProj(nRows - 1, nCols - 1).Y);

            //Console.WriteLine("Highest index bottomleft coords are x "
            //    + voxelGrid._bounds.CellBottomLeft_ToProj(nRows - 1, nCols - 1).X + " y "
            //    + voxelGrid._bounds.CellBottomLeft_ToProj(nRows - 1, nCols - 1).Y);

            //Console.WriteLine("Highest index topright coords are x "
            //    + voxelGrid._bounds.CellTopRight_ToProj(nRows - 1, nCols - 1).X + " y "
            //    + voxelGrid._bounds.CellTopRight_ToProj(nRows - 1, nCols - 1).Y);

            for (int iRow = 0; iRow < grid.Count(); iRow++)
            {
                grid[iRow] = new Bin[nCols];

                for (int jCol = 0; jCol < nCols; jCol++)
                    grid[iRow][jCol] = new Bin();
            }        

            voxelGrid._grid = grid;

            return voxelGrid;
        }

        private bool GetGridIndexes(double x, double y, out int iRow, out int jCol)
        {
            RcIndex rc = _bounds.ProjToCell(new Coordinate(x, y));
            iRow = rc.Row;
            jCol = rc.Column;

            if ((jCol >= 0 && jCol < nCols && iRow >= 0 && iRow < nRows))
                return true;

            return false;
        }

        public void GetGridCoordinates(int iRow, int jCol, out double x, out double y)
        {
            Coordinate c = _bounds.CellBottomLeft_ToProj(iRow, jCol);

            x = c.X;
            y = c.Y;
        }

        public bool AddPoint(double x, double y, double z, byte classification, bool IsGroundPoint)
        {
            int iRow, jCol;
            bool IsAdded = false;

            if (GetGridIndexes(x, y, out iRow, out jCol))
            {
                if (z < 0)
                    z = 0;

                _grid[iRow][jCol].AddPoint(z, classification, IsGroundPoint);
                IsAdded = true;
            }

            return IsAdded;
        }

        private void SetReferenceHeights(SurfaceTriangulation tri, bool isGround, 
            double minX, double minY, double maxX, double maxY, 
            out int nMissingBefore, out int nMissingAfter)
        {
            nMissingBefore = 0;
            nMissingAfter = 0;
            byte classification;

            RcIndex rcMin = _bounds.ProjToCell(new Coordinate(minX, maxY));
            RcIndex rcMax = _bounds.ProjToCell(new Coordinate(maxX, minY));

            if (rcMin == RcIndex.Empty || rcMax == RcIndex.Empty)
                throw new Exception();

            for (int iRow = rcMin.Row; iRow < rcMax.Row; iRow++)
            {
                for (int jCol = rcMin.Column; jCol < rcMax.Column; jCol++)
                {
                    double median = GetGroundMedian(iRow, jCol);

                    if (double.IsNaN(median))
                    {
                        nMissingBefore++;
                        Coordinate center = _bounds.CellCenter_ToProj(iRow, jCol);
                        
                        median = tri.GetHeightAndClass(center.X, center.Y, out classification);

                        if (double.IsNaN(median))
                        {
                            nMissingAfter++;
                            median = 0;
                        }
                        else
                        {
                            BinPoint p = new BinPoint() { Z = median, Class = classification };

                            if (isGround)
                                _grid[iRow][jCol].GroundReference = p;
                            else
                                _grid[iRow][jCol].SurfaceReference = p;
                        }
                    }
                }
            }
        }

        public void SetGroundReferenceHeights(SurfaceTriangulation tri, double minX, double minY, double maxX, double maxY,
            out int nMissingBefore, out int nMissingAfter)
        {
            SetReferenceHeights(tri, true, minX, minY, maxX, maxY, out nMissingBefore, out nMissingAfter);
        }

        public void SetSurfaceReferenceHeights(SurfaceTriangulation tri, double minX, double minY, double maxX, double maxY,
            out int nMissingBefore, out int nMissingAfter)
        {
            SetReferenceHeights(tri, false, minX, minY, maxX, maxY, out nMissingBefore, out nMissingAfter);
        }

        public void SaveAsAscHighestInClassRange(string outputFileName, int lowestClass, int highestClass, double noDataValue = -9999)
        {
            using (StreamWriter file = new StreamWriter(outputFileName))
            {
                WriteAscHeader(noDataValue, file);

                foreach (Bin[] row in _grid)
                {
                    List<double> heights = new List<double>();
                    foreach (Bin b in row)
                    {
                        double h = double.NaN;

                        foreach (BinPoint p in b.OtherPoints)
                        {
                            if (p.Class >= lowestClass && p.Class <= highestClass)
                            {
                                h = p.Z;
                                break;
                            }
                        }
                        
                        if (double.IsNaN(h))
                        {
                            if (b.SurfaceReference == null)
                                h = GetGroundMedianOrRerefence(b);
                            else
                                h = b.SurfaceReference.Z;
                        }

                        if (double.IsNaN(h))
                            h = noDataValue;

                        heights.Add(h);
                    }

                    file.WriteLine(String.Join(" ", heights));
                }
            }
        }

        public void SaveAsAscGroundMedian(string outputFileName, double noDataValue = -9999)
        {
            using (StreamWriter file = new StreamWriter(outputFileName))
            {
                WriteAscHeader(noDataValue, file);

                foreach (Bin[] row in _grid)
                {
                    List<double> heights = new List<double>();
                    foreach (Bin b in row)
                    {
                        double median = GetGroundMedianOrRerefence(b);

                        if (double.IsNaN(median))
                            heights.Add(noDataValue);
                        else
                            heights.Add(median);
                    }

                    file.WriteLine(String.Join(" ", heights));
                }
            }
        }

        private void WriteAscHeader(double noDataValue, StreamWriter file)
        {
            file.WriteLine("ncols         " + nCols);
            file.WriteLine("nrows         " + nRows);
            file.WriteLine("xllcorner     " + _bounds.BottomLeft().X);
            file.WriteLine("yllcorner     " + _bounds.BottomLeft().Y);
            file.WriteLine("cellsize      " + _bounds.CellWidth);
            file.WriteLine("NODATA_value  " + noDataValue);
        }

        public void SortFromHighestToLowest()
        {
            foreach (Bin[] row in _grid)
            {
                foreach (Bin b in row)
                {
                    b.OrderPointsFromHighestToLowest();
                }
            }
        }

        public List<BinPoint> GetOtherPoints(int iRow, int jCol)
        {
            return _grid[iRow][jCol].OtherPoints;
        }

        public List<double> GetOtherPointsByClassRange(int iRow, int jCol, int lowestClass, int highestClass)
        {
            List<double> heights = new List<double>();

            foreach (BinPoint p in _grid[iRow][jCol].OtherPoints)
            {
                if (p.Class >= lowestClass && p.Class <= highestClass)
                    heights.Add(p.Z);
            }

            return heights;
        }

        public double GetGroundMedianOrRerefence(int iRow, int jCol)
        {
            return GetGroundMedianOrRerefence(_grid[iRow][jCol]);
        }

        public BinPoint GetGroundMedianOrRerefencePoint(int iRow, int jCol)
        {
            BinPoint p = _grid[iRow][jCol].GetGroundMedianPoint();

            if (p == null)
                p = _grid[iRow][jCol].GroundReference;

            return p;
        }

        private double GetGroundMedianOrRerefence(Bin b)
        {
            double median = b.GetGroundMedian();
            if (double.IsNaN(median))
            {
                if (b.GroundReference != null)
                    median = b.GroundReference.Z;
            }
            return median;
        }

        public BinPoint GetHighestGroundPointInClassRange(int iRow, int jCol, int lowestClass, int highestClass)
        {
            foreach (BinPoint bp in _grid[iRow][jCol].GroundPoints)
            {
                if (bp.Class >= lowestClass && bp.Class <= highestClass)
                    return bp;
            }

            return null;
        }

        public bool IsHighestBinInNeighborhood(int iRowCenter, int jColCenter, int radius, int lowestClass, int highestClass)
        {
            int iRowMin = iRowCenter - radius;
            int jColMin = jColCenter - radius;
            int iRowMax = iRowCenter + radius;
            int jColMax = jColCenter + radius;

            if (iRowMin < 0)
                iRowMin = 0;
            if (jColMin < 0)
                jColMin = 0;
            if (iRowMax > nRows - 1)
                iRowMax = nRows - 1;
            if (jColMax > nCols - 1)
                jColMax = nCols - 1;

            double centerHeight = GetHighestPointInClassRange(iRowCenter, jColCenter, lowestClass, highestClass).Z;

            for (int iRow = iRowMin; iRow <= iRowMax; iRow++)
            {
                for (int jCol = jColMin; jCol <= jColMax; jCol++)
                {
                    if (iRow == iRowCenter && jCol == jColCenter)
                        continue;

                    BinPoint p = GetHighestPointInClassRange(iRow, jCol, lowestClass, highestClass);

                    if (p != null && p.Z >= centerHeight)
                        return false;
                }
            }

            return true;
        }

        public double GetGroundMedian(int iRow, int jCol)
        {
            return _grid[iRow][jCol].GetGroundMedian();
        }

        public BinPoint GetSurfaceReference(int iRow, int jCol)
        {
            return _grid[iRow][jCol].SurfaceReference;
        }

        public BinPoint GetHighestPointInClassRange(int iRow, int jCol, int lowestClass, int highestClass)
        {
            foreach (BinPoint bp in _grid[iRow][jCol].OtherPoints)
            {
                if (bp.Class >= lowestClass && bp.Class <= highestClass)
                    return bp;
            }

            return null;
        }

        public double GetHeight(double x, double y)
        {
            int iRow, jCol;
            double ret = double.NaN;

            if (GetGridIndexes(x, y, out iRow, out jCol))
            {
                ret = GetGroundMedian(iRow, jCol);
            }

            return ret;
        }
    }
}
