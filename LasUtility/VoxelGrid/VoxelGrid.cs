﻿using DotSpatial.Data;
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

        public bool AddPoint(double x, double y, double z, byte classification)
        {
            int iRow, jCol;
            bool IsAdded = false;

            if (GetGridIndexes(x, y, out iRow, out jCol))
            {
                _grid[iRow][jCol].AddPoint(z, classification);
                IsAdded = true;
            }

            return IsAdded;
        }

        public void SetReferenceHeights(IHeightMap tri, out int nMissingBefore, out int nMissingAfter)
        {
            nMissingBefore = 0;
            nMissingAfter = 0;

            for (int iRow = 0; iRow < nRows; iRow++)
            {
                for (int jCol = 0; jCol < nCols; jCol++)
                {
                    double median = GetGroundMedian(iRow, jCol);

                    if (double.IsNaN(median))
                    {
                        nMissingBefore++;
                        Coordinate center = _bounds.CellCenter_ToProj(iRow, jCol);
                        median = tri.GetHeight(center.X, center.Y);

                        if (!double.IsNaN(median))
                            _grid[iRow][jCol].ReferenceHeight = median;
                        else
                            nMissingAfter++;
                    }
                }
            }
        }

        public enum AscSaveMode
        {
            GroundMedian,
            HightestSurface
        }

        public void SaveAsAsc(string outputFileName, AscSaveMode mode, double noDataValue = -9999D)
        {
            switch (mode)
            {
                case AscSaveMode.GroundMedian:
                    SaveAsAscGroundMedian(outputFileName, noDataValue);
                    break;
                case AscSaveMode.HightestSurface:
                    SaveAsAscHightestSurface(outputFileName, noDataValue);
                    break;
                default:
                    throw new Exception("Invalid save mode");

            }
        }

        private void SaveAsAscHightestSurface(string outputFileName, double noDataValue)
        {
            using (StreamWriter file = new StreamWriter(outputFileName))
            {
                WriteAscHeader(noDataValue, file);

                foreach (Bin[] row in _grid)
                {
                    List<double> heights = new List<double>();
                    foreach (Bin b in row)
                    {
                        if (b.OtherPoints.Any())
                        {
                            heights.Add(b.OtherPoints.First().Z);
                        }
                        else
                        {
                            double median = b.GetGroundMedian();
                            if (double.IsNaN(median))
                                median = b.ReferenceHeight;

                            if (Math.Abs(median) < 0.0001)
                                median = noDataValue;

                            heights.Add(median);
                        }
                    }

                    file.WriteLine(String.Join(" ", heights));
                }
            }
        }

        private void SaveAsAscGroundMedian(string outputFileName, double noDataValue)
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

        public List<double> GetOtherPointsByClass(int iRow, int jCol, int classification)
        {
            List<double> heights = new List<double>();

            foreach (BinPoint p in _grid[iRow][jCol].OtherPoints)
            {
                if (p.Class == classification)
                    heights.Add(p.Z);
            }

            return heights;
        }

        public double GetGroundMedianOrRerefence(int iRow, int jCol)
        {
            return GetGroundMedianOrRerefence(_grid[iRow][jCol]);
        }

        private double GetGroundMedianOrRerefence(Bin b)
        {
            double median = b.GetGroundMedian();
            if (double.IsNaN(median))
            {
                median = b.ReferenceHeight;
                if (Math.Abs(median) < 0.0001)
                    median = double.NaN;
            }
            return median;
        }

        public double GetGroundMedian(int iRow, int jCol)
        {
            return _grid[iRow][jCol].GetGroundMedian();
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
