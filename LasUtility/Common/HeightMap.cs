using NetTopologySuite.Geometries;
using System;
using System.IO;
using System.Text;

namespace LasUtility.Common
{
    public class HeightMap : IRaster
    {
        private const int _iNoDataValue = 0;
        public const string FileExtension = ".asc";
        public const string FileExtensionCompressed = ".asp";

        public byte[][] Raster;
        public IRasterBounds Bounds;

        public void InitializeRaster(int minX, int minY, int maxX, int maxY)
        {
            Envelope extent = new(minX, maxX, minY, maxY);

            InitializeRaster(extent);
        }

        public void WriteAsAscii(string sFullFilename)
        {
            bool bIsCompressed = sFullFilename.EndsWith(FileExtensionCompressed);

            using StreamWriter file = new(sFullFilename);

            file.WriteLine("ncols         " + Bounds.ColumnCount);
            file.WriteLine("nrows         " + Bounds.RowCount);
            file.WriteLine("xllcorner     " + Bounds.MinX);
            file.WriteLine("yllcorner     " + Bounds.MinY);
            file.WriteLine("cellsize      " + Bounds.CellWidth);
            file.WriteLine("NODATA_value  " + _iNoDataValue);

            for (int iRow = Bounds.RowCount - 1; iRow >= 0; --iRow)
            {
                if (false == bIsCompressed)
                {
                    file.WriteLine(String.Join(" ", Raster[iRow]));
                }
                else
                {
                    file.WriteLine(GetCompressedString(Raster[iRow]));
                }
            }
        }

        public void WriteAsAscii(string sFullFilename, int iMinX, int iMinY, int iMaxX, int iMaxY)
        {
            // Max values are not included in the raster
            double dMaxX = iMaxX - RasterBounds.dEpsilon;
            double dMaxY = iMaxY - RasterBounds.dEpsilon;

            RcIndex start = Bounds.ProjToCell(new Coordinate(iMinX, iMinY));
            RcIndex end = Bounds.ProjToCell(new Coordinate(dMaxX, dMaxY));
            int iColumnCount = end.Column - start.Column + 1;
            int iRowCount = end.Row - start.Row + 1;

            bool bIsCompressed = sFullFilename.EndsWith(FileExtensionCompressed);

            using StreamWriter file = new(sFullFilename);

            file.WriteLine("ncols         " + iColumnCount);
            file.WriteLine("nrows         " + iRowCount);
            file.WriteLine("xllcorner     " + iMinX);
            file.WriteLine("yllcorner     " + iMinY);
            file.WriteLine("cellsize      " + Bounds.CellWidth);
            file.WriteLine("NODATA_value  " + _iNoDataValue);

            for (int iRow = end.Row; iRow >= start.Row; --iRow)
            {
                if (false == bIsCompressed)
                {
                    // Write values separated by spaces
                    file.WriteLine(String.Join(" ", Raster[iRow][start.Column..(end.Column + 1)]));
                }
                else
                {
                    file.WriteLine(GetCompressedString(Raster[iRow][start.Column..(end.Column + 1)]));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private ReadOnlySpan<char> GetCompressedString(byte[] line)
        {
            StringBuilder sb = new(line.Length);

            int i = 0;
            int iValue = line[i];
            int iValueCount = 0;

            while (i < line.Length)
            {
                if (line[i] == iValue)
                {
                    iValueCount++;
                }
                else
                {
                    sb.Append(iValueCount + "x" + iValue + " ");

                    iValue = line[i];
                    iValueCount = 1;
                }

                i++;
            }

            sb.Append(iValueCount + "x" + iValue);

            return sb.ToString();
        }

#if OPEN_CV
        public void WriteAsPng(string fullFileName)
        {
            using Mat shpPic = new (_bounds.NumRows, _bounds.NumColumns, MatType.CV_8UC3);

            // OpenCV image channes are in order BGR(Blue - Green - Red)
            //const int OPEN_CV_RED = 2;
            //const int OPEN_CV_GREEN = 1;
            //const int OPEN_CV_BLUE = 0;

            for (int iRow = 0; iRow < _bounds.NumRows; iRow++)
            {
                for (int iCol = 0; iCol < _bounds.NumColumns; iCol++)
                {
                    if (_raster[iRow][iCol] != _noDataValue)
                    {
                        // Mirror rows
                        int iRowMirrored = _bounds.NumRows - 1 - iRow;

                        shpPic.At<Vec3b>(iRowMirrored, iCol)[0] = _raster[iRow][iCol];
                        shpPic.At<Vec3b>(iRowMirrored, iCol)[1] = _raster[iRow][iCol];
                        shpPic.At<Vec3b>(iRowMirrored, iCol)[2] = _raster[iRow][iCol];

                    }
                }
            }

            shpPic.SaveImage(fullFileName);
        }
#endif
        public HeightMap Crop(int iMinX, int iMinY, int iMaxX, int iMaxY)
        {
            // Max values are not included in the raster
            double dMaxX = iMaxX - RasterBounds.dEpsilon;
            double dMaxY = iMaxY - RasterBounds.dEpsilon;

            // Copy values from this rasteriser to the new one
            RcIndex start = Bounds.ProjToCell(new Coordinate(iMinX, iMinY));
            RcIndex end = Bounds.ProjToCell(new Coordinate(dMaxX, dMaxY));
            int iColumnCount = end.Column - start.Column + 1;
            int iRowCount = end.Row - start.Row + 1;

            // Create a new heightmap with the new bounds and row/column counts
            // Note that max values are included in the new bounds
            HeightMap hm = new();
            hm.InitializeRaster(iRowCount, iColumnCount, new Envelope(iMinX, iMaxX, iMinY, iMaxY));

            for (int iRow = start.Row; iRow <= end.Row; iRow++)
            {
                hm.Raster[iRow - start.Row] = new byte[iColumnCount];
                Array.Copy(Raster[iRow], start.Column, hm.Raster[iRow - start.Row], 0, iColumnCount);
            }

            return hm;
        }


        public static HeightMap CreateFromAscii(string sFullFilename)
        {
            HeightMap hm = new();

            char[] delimiters = new char[] { ' ', '\t' };

            int nRows = -1, nCols = -1, minX = -1, minY = -1, cellSize = -1, noDataValue = -1;

            bool bIsCompressed = sFullFilename.EndsWith(FileExtensionCompressed);

            using (StreamReader file = new(sFullFilename))
            {
                string line;
                bool IsHeaderRead = false;
                int iRow = -1;

                while ((line = file.ReadLine()) != null)
                {
                    string[] words = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    if (!IsHeaderRead)
                    {
                        if (words[0].ToUpper().Trim() == "NCOLS")
                            nCols = int.Parse(words[1]);
                        else if (words[0].ToUpper().Trim() == "NROWS")
                            nRows = int.Parse(words[1]);
                        else if (words[0].ToUpper().Trim() == "XLLCORNER")
                            minX = Convert.ToInt32(Math.Floor(Convert.ToDouble(words[1])));
                        else if (words[0].ToUpper().Trim() == "YLLCORNER")
                            minY = Convert.ToInt32(Math.Floor(Convert.ToDouble(words[1])));
                        else if (words[0].ToUpper().Trim() == "CELLSIZE")
                            cellSize = Convert.ToInt32(Math.Floor(Convert.ToDouble(words[1])));
                        else if (words[0].ToUpper().Trim() == "NODATA_VALUE")
                            noDataValue = int.Parse(words[1]);
                        else
                        {
                            if (nRows < 0 || nCols < 0 || minX < 0 || minY < 0 || cellSize < 0)
                                throw new Exception("Invalid format in header " + sFullFilename);

                            Envelope extent = new(minX, minX + nCols, minY, minY + nRows);
                            hm.InitializeRaster(extent);
                            IsHeaderRead = true;
                            iRow = nRows;
                        }
                    }

                    if (IsHeaderRead)
                    {
                        if (iRow < 0)
                            throw new Exception(String.Format("File {0} contains too many data rows", sFullFilename));

                        if (false == bIsCompressed)
                        {
                            if (words.Length != nCols)
                            {
                                throw new Exception(String.Format("File {0} contains invalid column count {1} on line {2}",
                                    sFullFilename, words.Length, nRows - iRow));
                            }

                            hm.Raster[--iRow] = Array.ConvertAll(words, byte.Parse);
                        }
                        else
                        {
                            int iCol = 0;
                            iRow--;

                            foreach (string word in words)
                            {
                                string[] parts = word.Split('x');

                                if (parts.Length != 2)
                                {
                                    throw new Exception(String.Format("File {0} contains invalid [count]x[value] format on line {1}",
                                        sFullFilename, nRows - iRow + 1));
                                }

                                int iValueCount = int.Parse(parts[0]);
                                byte bValue = byte.Parse(parts[1]);

                                for (int i = 0; i < iValueCount; i++)
                                {
                                    hm.Raster[iRow][iCol++] = bValue;
                                }

                            }

                            if (iCol != nCols)
                            {
                                throw new Exception(String.Format("File {0} contains invalid column count {1} on line {2}",
                                    sFullFilename, iCol, nRows - iRow + 1));
                            }
                        }
                    }
                }

                if (iRow < 0)
                    throw new Exception(String.Format("File {0} contains too few data rows", sFullFilename));
            }

            return hm;
        }

        public void InitializeRaster(Envelope extent)
        {
            InitializeRaster((int)extent.Height, (int)extent.Width, extent);
        }

        public void InitializeRaster(int iRowCount, int iColumnCount, Envelope extent)
        {
            Bounds = new RasterBounds(iRowCount, iColumnCount, extent);
            Raster = new byte[Bounds.RowCount][];

            for (int iRow = 0; iRow < Bounds.RowCount; iRow++)
            {
                Raster[iRow] = new byte[Bounds.ColumnCount];
                for (int jCol = 0; jCol < Bounds.ColumnCount; jCol++)
                    Raster[iRow][jCol] = _iNoDataValue;
            }
        }

        public double GetValue(Coordinate c)
        {
            RcIndex rc = Bounds.ProjToCell(c);

            if (rc == RcIndex.Empty)
            {
                // Console.WriteLine("Coordinate out of bounds " + x + " " + y);
                return double.NaN;
            }

            if (Raster[rc.Row][rc.Column] == _iNoDataValue)
                return double.NaN;

            return Raster[rc.Row][rc.Column];
        }

        public double GetValue(int iRow, int jCol)
        {
            if (iRow < 0 || iRow > (Bounds.RowCount - 1) || jCol < 0 || jCol > (Bounds.ColumnCount - 1))
            {
                throw new ArgumentException("Cell indexes are out of range.");
            }

            if (Raster[iRow][jCol] == _iNoDataValue)
                return double.NaN;

            return Raster[iRow][jCol];
        }
    }
}
