using LasUtility.DEM;
using System;
using System.IO;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using LasUtility.Common;
using MessagePack;

namespace LasUtility.VoxelGrid
{
    [MessagePackObject]
    public class VoxelGrid : IHeightMap
    {
        [Key(0)]
        public IRasterBounds _bounds;
        [Key(1)]
        public Bin[,] _grid;

        [Key(2)]
        public float[,] Dem { get; set; }

        [IgnoreMember]
        public IRasterBounds Bounds => _bounds;
        [IgnoreMember]
        public Bin[,] Grid => _grid;

        [Key(3)]
        public bool _bIsSorted;

        public static VoxelGrid CreateGrid(IRasterBounds bounds, Bin[,] grid, float[,] dem)
        {
            return new()
            {
                _bounds = bounds,
                _grid = grid,
                Dem = dem
            };
        }

        /// <summary>
        /// Returns a new 3D bin grid with specified parameters.
        /// </summary>
        /// <param name="nRows"> Y-resolution: Bin height is (maxY - minY) / nRows </param>
        /// <param name="nCols"> X-resolution  Bin width is (maxX - minX / nCols </param>
        /// <param name="minX"> Minimun x coodinate. This is included in the area. </param>
        /// <param name="minY"> Minimun y coodinate. This is included in the area. </param>
        /// <param name="maxX"> Maximum x coodinate. This is NOT included in the area. I.e. [minX, maxX[ </param>
        /// <param name="maxY"> Maximum y coodinate. This is NOT included in the area. I.e. [minY, maxY[ </param>
        /// <returns></returns>
        public static VoxelGrid CreateGrid(int nRows, int nCols, Envelope extent)
        {
            Bin[,] grid = new Bin[nRows, nCols];
            float[,] dem = new float[nRows, nCols];

            for (int iRow = 0; iRow < nRows; iRow++)
            {
                for (int jCol = 0; jCol < nCols; jCol++)
                {
                    dem[iRow, jCol] = Single.NaN;
                    grid[iRow, jCol] = new();
                }
            }

            return new()
            {
                _bounds = new RasterBounds(nRows, nCols, extent),
                Dem = dem,
                _grid = grid
            };
        }

        public bool GetGridIndexes(double x, double y, out int iRow, out int jCol)
        {
            RcIndex rc = _bounds.ProjToCell(new Coordinate(x, y));
            iRow = rc.Row;
            jCol = rc.Column;

            if (jCol >= 0 && jCol < _bounds.ColumnCount && iRow >= 0 && iRow < _bounds.RowCount)
                return true;

            return false;
        }

        public void GetGridCoordinates(int iRow, int jCol, out double x, out double y)
        {
            Coordinate c = _bounds.CellBottomLeftToProj(iRow, jCol);

            x = c.X;
            y = c.Y;
        }

        public bool AddPoint(double x, double y, float z, byte classification, bool IsGroundPoint)
        {
            bool IsAdded = false;

            if (GetGridIndexes(x, y, out int iRow, out int jCol))
            {
                if (IsGroundPoint)
                {
                    if (float.IsNaN(Dem[iRow, jCol]))
                    {
                        Dem[iRow, jCol] = z;
                    }
                    else
                    {
                        Dem[iRow, jCol] = Math.Max(Dem[iRow, jCol], z);
                    }
                }
                else
                {
                    _grid[iRow, jCol].AddPoint(z, classification);
                    _bIsSorted = false;
                }

                IsAdded = true;
            }

            return IsAdded;
        }

        public void SetMissingHeightsFromTriangulation(SurfaceTriangulation tri,
            int iMinX, int iMinY, int iMaxX, int iMaxY,
            out int nMissingBefore, out int nMissingAfter)
        {
            nMissingBefore = 0;
            nMissingAfter = 0;

            // Max values are not included in the raster
            double dMaxX = iMaxX - RasterBounds.dEpsilon;
            double dMaxY = iMaxY - RasterBounds.dEpsilon;

            RcIndex rcMin = _bounds.ProjToCell(new Coordinate(iMinX, iMinY));
            RcIndex rcMax = _bounds.ProjToCell(new Coordinate(dMaxX, dMaxY));

            if (rcMin == RcIndex.Empty || rcMax == RcIndex.Empty)
                throw new Exception();

            for (int iRow = rcMin.Row; iRow <= rcMax.Row; iRow++)
            {
                for (int jCol = rcMin.Column; jCol <= rcMax.Column; jCol++)
                {
                    if (float.IsNaN(Dem[iRow, jCol]))
                    {
                        nMissingBefore++;
                        Coordinate c = _bounds.CellBottomLeftToProj(iRow, jCol);

                        float fHeight = (float)tri.GetHeightAndClass(c.X, c.Y, out byte _);

                        if (float.IsNaN(fHeight))
                        {
                            nMissingAfter++;
                        }
                        else
                        {
                            Dem[iRow, jCol] = fHeight;
                        }
                    }
                }
            }
        }

        public void SaveAsAscHighestInClassRange(string outputFileName, int lowestClass, int highestClass, float fNoDataValue = -9999)
        {
            using StreamWriter file = new(outputFileName);

            WriteAscHeader(fNoDataValue, file);

            for (int iRow = 0; iRow < _bounds.RowCount; iRow++)
            {
                List<double> heights = new();

                for (int jCol = 0; jCol < _bounds.ColumnCount; jCol++)
                {
                    float h = float.NaN;

                    foreach (BinPoint p in _grid[iRow, jCol].Points)
                    {
                        if (p.Class >= lowestClass && p.Class <= highestClass)
                        {
                            h = p.Z;
                            break;
                        }
                    }

                    if (float.IsNaN(h) && !float.IsNaN(Dem[iRow, jCol]))
                    {
                        h = Dem[iRow, jCol];
                    }

                    if (float.IsNaN(h))
                        h = fNoDataValue;

                    heights.Add(h);
                }

                file.WriteLine(string.Join(" ", heights));
            }
        }

        public void SaveAsAscGroundHeight(string outputFileName, float fNoDataValue = -9999)
        {
            using StreamWriter file = new(outputFileName);

            WriteAscHeader(fNoDataValue, file);

            for (int iRow = 0; iRow < _bounds.RowCount; iRow++)
            {
                List<double> heights = new();

                for (int jCol = 0; jCol < _bounds.ColumnCount; jCol++)
                {
                    if (float.IsNaN(Dem[iRow, jCol]))
                        heights.Add(fNoDataValue);
                    else
                        heights.Add(Dem[iRow, jCol]);
                }

                file.WriteLine(String.Join(" ", heights));
            }
        }

        private void WriteAscHeader(double noDataValue, StreamWriter file)
        {
            file.WriteLine("ncols         " + _bounds.ColumnCount);
            file.WriteLine("nrows         " + _bounds.RowCount);
            file.WriteLine("xllcorner     " + _bounds.MinX);
            file.WriteLine("yllcorner     " + _bounds.MinY);
            file.WriteLine("cellsize      " + _bounds.CellWidth);
            file.WriteLine("NODATA_value  " + noDataValue);
        }

        /// <summary>
        /// Run after you are done with adding new points.
        /// </summary>
        public void SortAndTrim()
        {
            for (int iRow = 0; iRow < _bounds.RowCount; iRow++)
            {
                for (int jCol = 0; jCol < _bounds.ColumnCount; jCol++)
                {
                    _grid[iRow, jCol].OrderPointsFromHighestToLowest();
                    _grid[iRow, jCol].Trim();
                }
            }

            _bIsSorted = true;
        }

        public List<BinPoint> GetPoints(int iRow, int jCol)
        {
            return _grid[iRow, jCol].Points;
        }

        public List<float> GetHeightsByClassRange(int iRow, int jCol, int lowestClass, int highestClass)
        {
            List<float> heights = new();

            foreach (BinPoint p in _grid[iRow, jCol].Points)
            {
                if (p.Class >= lowestClass && p.Class <= highestClass)
                    heights.Add(p.Z);
            }

            return heights;
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
            if (iRowMax > _bounds.RowCount - 1)
                iRowMax = _bounds.RowCount - 1;
            if (jColMax > _bounds.ColumnCount - 1)
                jColMax = _bounds.ColumnCount - 1;

            BinPoint center = GetHighestPointInClassRange(iRowCenter, jColCenter, lowestClass, highestClass);

            if (center == null) 
                return false;   

            for (int iRow = iRowMin; iRow <= iRowMax; iRow++)
            {
                for (int jCol = jColMin; jCol <= jColMax; jCol++)
                {
                    if (iRow == iRowCenter && jCol == jColCenter)
                        continue;

                    BinPoint p = GetHighestPointInClassRange(iRow, jCol, lowestClass, highestClass);

                    if (p != null && p.Z >= center.Z)
                        return false;
                }
            }

            return true;
        }

        public BinPoint GetHighestPointInClassRange(int iRow, int jCol, int lowestClass, int highestClass)
        {
            if (false == _bIsSorted)
                throw new Exception("Call SortAndTrim() first");

            foreach (BinPoint bp in _grid[iRow, jCol].Points)
            {
                if (bp.Class >= lowestClass && bp.Class <= highestClass)
                    return bp;
            }

            return null;
        }

        public void Serialize(string sFullFilename)
        {
            // Write to a temporary file first to avoid corrupting the file if the process is interrupted.
            string sTempFilename = Path.ChangeExtension(sFullFilename, ".tmp");

            byte[] bytes = MessagePackSerializer.Serialize(this);
            File.WriteAllBytes(sTempFilename, bytes);

            if (File.Exists(sFullFilename))
                File.Delete(sFullFilename);

            File.Move(sTempFilename, sFullFilename);
        }

        public static VoxelGrid Deserialize(string sFilename)
        {
            byte[] bytes = File.ReadAllBytes(sFilename);

            return MessagePackSerializer.Deserialize<VoxelGrid>(bytes);
        }

        public double GetHeight(double x, double y)
        {
            if (GetGridIndexes(x, y, out int iRow, out int jCol))
            {
                return Dem[iRow, jCol];
            }

            throw new Exception("Coordinate out of bounds");
        }

        public float GetGroundHeight(int iRow, int jCol)
        {
            if (jCol >= 0 && jCol < _bounds.ColumnCount && iRow >= 0 && iRow < _bounds.RowCount)
            {
                return Dem[iRow, jCol];
            }

            throw new Exception("Grid indices out of bounds");
        }
    }
}
