﻿using LasUtility.DEM;
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
        public Bin[][] _grid;

        [IgnoreMember]
        public IRasterBounds Bounds => _bounds;
        [IgnoreMember]
        public Bin[][] Grid => _grid;

        [Key(2)]
        public bool _bIsSorted;

        [Key(3)]
        public string Name;

        [Key(4)]
        public string Version { get; set; }

        public static VoxelGrid CreateGrid(string sName, string sVersion, IRasterBounds bounds, Bin[][] grid)
        {
            return new()
            {
                _bounds = bounds,
                _grid = grid,
                Name = sName,
                Version = sVersion
            };
        }

        /// <summary>
        /// Returns a new 3D bin grid with specified parameters.
        /// </summary>
        /// <param name="sName"> Name of the grid </param>
        /// <param name="nRows"> Y-resolution: Bin height is (maxY - minY) / nRows </param>
        /// <param name="nCols"> X-resolution  Bin width is (maxX - minX / nCols </param>
        /// <param name="minX"> Minimun x coodinate. This is included in the area. </param>
        /// <param name="minY"> Minimun y coodinate. This is included in the area. </param>
        /// <param name="maxX"> Maximum x coodinate. This is NOT included in the area. I.e. [minX, maxX[ </param>
        /// <param name="maxY"> Maximum y coodinate. This is NOT included in the area. I.e. [minY, maxY[ </param>
        /// <returns></returns>
        public static VoxelGrid CreateGrid(string sName, int nRows, int nCols, Envelope extent)
        {
            Bin[][] grid = new Bin[nRows][];

            for (int iRow = 0; iRow < grid.Length; iRow++)
            {
                grid[iRow] = new Bin[nCols];

                for (int jCol = 0; jCol < nCols; jCol++)
                    grid[iRow][jCol] = new Bin();
            }        

            return new()
            {
                _bounds = new RasterBounds(nRows, nCols, extent),
                Name = sName,
                _grid = grid
            };
        }

        public bool GetGridIndexes(double x, double y, out int iRow, out int jCol)
        {
            RcIndex rc = _bounds.ProjToCell(new Coordinate(x, y));
            iRow = rc.Row;
            jCol = rc.Column;

            if ((jCol >= 0 && jCol < _bounds.ColumnCount && iRow >= 0 && iRow < _bounds.RowCount))
                return true;

            return false;
        }

        public void GetGridCoordinates(int iRow, int jCol, out double x, out double y)
        {
            Coordinate c = _bounds.CellBottomLeftToProj(iRow, jCol);

            x = c.X;
            y = c.Y;
        }

        public bool AddPoint(double x, double y, double z, byte classification, bool IsGroundPoint)
        {
            _bIsSorted = false;

            bool IsAdded = false;

            if (GetGridIndexes(x, y, out int iRow, out int jCol))
            {
                if (z < 0)
                    z = 0;

                _grid[iRow][jCol].AddPoint(z, classification, IsGroundPoint);
                IsAdded = true;
            }

            return IsAdded;
        }
        public void SetGroundReferenceHeights(SurfaceTriangulation tri, 
            double minX, double minY, double maxX, double maxY, 
            out int nMissingBefore, out int nMissingAfter)
        {
            nMissingBefore = 0;
            nMissingAfter = 0;

            RcIndex rcMin = _bounds.ProjToCell(new Coordinate(minX, minY));
            RcIndex rcMax = _bounds.ProjToCell(new Coordinate(maxX, maxY));

            if (rcMin == RcIndex.Empty || rcMax == RcIndex.Empty)
                throw new Exception();

            for (int iRow = rcMin.Row; iRow <= rcMax.Row; iRow++)
            {
                for (int jCol = rcMin.Column; jCol <= rcMax.Column; jCol++)
                {
                    double median = GetGroundMedian(iRow, jCol);

                    if (double.IsNaN(median))
                    {
                        nMissingBefore++;
                        Coordinate c = _bounds.CellBottomLeftToProj(iRow, jCol);
                        
                        median = tri.GetHeightAndClass(c.X, c.Y, out byte classification);

                        if (double.IsNaN(median))
                        {
                            nMissingAfter++;
                        }
                        else
                        {                          
                            _grid[iRow][jCol].GroundReference = new() { Z = median, Class = classification };
                        }
                    }
                }
            }
        }

        public void SaveAsAscHighestInClassRange(string outputFileName, int lowestClass, int highestClass, double noDataValue = -9999)
        {
            using StreamWriter file = new (outputFileName);

            WriteAscHeader(noDataValue, file);

            foreach (Bin[] row in _grid)
            {
                List<double> heights = new ();
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

        public void SaveAsAscGroundMedian(string outputFileName, double noDataValue = -9999)
        {
            using StreamWriter file = new (outputFileName);

            WriteAscHeader(noDataValue, file);

            foreach (Bin[] row in _grid)
            {
                List<double> heights = new ();
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
            foreach (Bin[] row in _grid)
            {
                foreach (Bin b in row)
                {
                    b.OrderPointsFromHighestToLowest();
                    b.Trim();
                }
            }

            _bIsSorted = true;
        }

        public List<BinPoint> GetOtherPoints(int iRow, int jCol)
        {
            return _grid[iRow][jCol].OtherPoints;
        }

        public List<double> GetOtherPointsByClassRange(int iRow, int jCol, int lowestClass, int highestClass)
        {
            List<double> heights = new ();

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
            if (!_bIsSorted)
                throw new Exception("VoxelGrid: Call SortAndTrim first.");

            BinPoint p = _grid[iRow][jCol].GetGroundMedianPoint();

            p ??= _grid[iRow][jCol].GroundReference;

            return p;
        }

        private double GetGroundMedianOrRerefence(Bin b)
        {
            if (!_bIsSorted)
                throw new Exception("VoxelGrid: Call SortAndTrim first.");

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
            if (iRowMax > _bounds.RowCount - 1)
                iRowMax = _bounds.RowCount - 1;
            if (jColMax > _bounds.ColumnCount - 1)
                jColMax = _bounds.ColumnCount - 1;

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
            if (!_bIsSorted)
                throw new Exception("VoxelGrid: Call SortAndTrim first.");

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
            double ret = double.NaN;

            if (GetGridIndexes(x, y, out int iRow, out int jCol))
            {
                ret = GetGroundMedian(iRow, jCol);
            }

            return ret;
        }

        public void Serialize(string sDirectory)
        {
            //BinaryFormatter formatter = new();

            //using FileStream fs = new (sFilename, FileMode.Create, FileAccess.Write);

            //formatter.Serialize(fs, this);

            string sFilename = Path.Combine(sDirectory, Name);

            if (!string.IsNullOrWhiteSpace(Version))
                sFilename += "_v" + Version;

            sFilename += ".obj";

            string sFilenameTemp = sFilename + ".tmp";

            byte[] bytes = MessagePackSerializer.Serialize(this);

            File.WriteAllBytes(sFilenameTemp, bytes);

            if (File.Exists(sFilename))
                File.Delete(sFilename);

            File.Move(sFilenameTemp, sFilename);
        }

        public static VoxelGrid Deserialize(string sFilename)
        {
            //BinaryFormatter formatter = new ();

            //using FileStream fs = new (sFilename, FileMode.Open, FileAccess.Read);

            //return (VoxelGrid)formatter.Deserialize(fs);

            byte[] bytes = File.ReadAllBytes(sFilename);

            return MessagePackSerializer.Deserialize<VoxelGrid>(bytes);
        }
    }
}
