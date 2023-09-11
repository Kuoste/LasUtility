using LasUtility.Common;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Esri;
#if OPEN_CV
using OpenCvSharp;
#endif

namespace LasUtility.ShapefileRasteriser
{
    public class Rasteriser : IHeightMap
    {
        private const int _iNoDataValue = 0;

        private readonly Dictionary<int, byte> _nlsPolygonClasses;
        private readonly Dictionary<int, byte> _nlsLineClasses;

        private byte[][] _raster;
        private IRasterBounds _bounds;

        public Rasteriser()
        {
            _nlsPolygonClasses = new Dictionary<int, byte>()
            {
                {42210, 100},  //	Asuinrakennus, ? krs
                {42211, 101},  //	Asuinrakennus, 1-2 krs
                {42212, 102},  //	Asuinrakennus, 3-n krs
                {42220, 103},  //	Liike- tai julkinen rakennus, ? krs
                {42221, 104},  //	Liike- tai julkinen rakennus, 1-2 krs
                {42222, 105},  //	Liike- tai julkinen rakennus, 3-n krs
                {42230, 106},  //	Lomarakennus, ? krs
                {42231, 107},  //	Lomarakennus, 1-2 krs
                {42232, 108},  //	Lomarakennus, 3-n krs
                {42240, 109},  //	Teollinen rakennus, ? krs
                {42241, 110},  //	Teollinen rakennus, 1-2 krs
                {42242, 111},  //	Teollinen rakennus, 3-n krs
                {42270, 112},  //	Kirkko
                {42250, 113},  //	Kirkollinen rakennus, ? krs
                {42251, 114},  //	Kirkollinen rakennus, 1-2 krs
                {42252, 115},  //	Kirkollinen rakennus, 3-n krs
                {42260, 116},  //	Muu rakennus, ? krs
                {42261, 117},  //	Muu rakennus, 1-2 krs
                {42262, 118},  //	Muu rakennus, 3-n krs

                //{36200, 130},  //	Järvivesi
                //{36211, 131},  //	Merivesi
                //{35411, 135},  //	Suo, helppokulkuinen puuton 
                //{35412, 136},  //	Suo, helppokulkuinen metsää kasvava 
                //{35421, 137},  //	Suo, vaikeakulkuinen puuton 
                //{35422, 138},  //	Suo, vaikeakulkuinen metsää kasvava 

            };

            _nlsLineClasses = new Dictionary<int, byte>()
            {
                {12111, 70}, //Autotie Ia // mc id 44
                {12112, 72}, //Autotie Ib
                {12121, 74}, //Autotie IIa
                {12122, 76}, //Autotie IIb
                {12131, 78}, //Autotie IIIa
                {12132, 80}, //Autotie IIIb
                {12141, 82}, //Ajotie
                //{12151, 99}, //Lautta
                //{12152, 99}, //Lossi
                //{12153, 99}, //Huoltoaukko ilman puomia
                //{12154, 99}, //Huoltoaukko puomilla
                //{12312, 99}, //Talvitie
                //{12313, 88}, //Polku
                {12314, 86}, //Kävely- ja pyörätie
                {12316, 84}  //Ajopolku

                //{36311, 50}, //Virtavesi, alle 2m
                //{36312, 51}  //Virtavesi, 2-5m
            };
        }


        public void InitializeRaster(string[] filenames)
        {
            Envelope extent = null;

            foreach (var filename in filenames)
            {
                using ShapefileReader reader = Shapefile.OpenRead(filename);

                if (extent == null)
                    extent = reader.BoundingBox;
                else
                    extent.ExpandToInclude(reader.BoundingBox);
            }


            // Expand to integer values to get cell size 1.0000000 meters 
            extent = new Envelope(
                Math.Floor(extent.MinX),
                Math.Ceiling(extent.MaxX),
                Math.Floor(extent.MinY),
                Math.Ceiling(extent.MaxY));



            CreaterRaster(extent);
        } 

        public void InitializeRaster(int minX, int minY, int maxX, int maxY)
        {
            Envelope extent = new (minX, maxX, minY, maxY);

            CreaterRaster(extent);
        }

        public void AddShapefile(string filename)
        {
            int nAdded = 0;
            int nTotal = 0;

            foreach (Feature feature in Shapefile.ReadAllFeatures(filename))
            {
                nTotal++;

                int classification = (int)(long)feature.Attributes["LUOKKA"];
                
                byte rasterValue;

                if (_nlsPolygonClasses.ContainsKey(classification))
                    rasterValue = _nlsPolygonClasses[classification];
                else if (_nlsLineClasses.ContainsKey(classification))
                    rasterValue = _nlsLineClasses[classification];
                else
                    continue;

                Envelope envelope = feature.Geometry.EnvelopeInternal;

                RcIndex iMin = _bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
                RcIndex iMax = _bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

                // Skip if outside the raster.
                if (iMin == RcIndex.Empty || iMax == RcIndex.Empty)
                    continue;

                nAdded++;
                SetValueIfInside(iMin.Row, iMax.Row, iMin.Column, iMax.Column, feature.Geometry, rasterValue);
            }

            if (nAdded > 0)
            {
                Console.Write(Environment.NewLine);
                Console.WriteLine("File {0} contained {1} shapes of the wanted class of which {2} were added",
                    Path.GetFileName(filename), nTotal, nAdded);
            }
        }

        private void SetValueIfInside(int iRowMin, int iRowMax, int jColMin, int jColMax, Geometry geometry, byte rasterValue)
        {
            Coordinate max = new(_bounds.CellTopRightToProj(iRowMax, jColMax));
            Coordinate min = new(_bounds.CellBottomLeftToProj(iRowMin, jColMin));

            Geometry rect = new Polygon(new LinearRing(new Coordinate[]
            {
                new Coordinate(min.X, min.Y),
                new Coordinate(min.X, max.Y),
                new Coordinate(max.X, max.Y),
                new Coordinate(max.X, min.Y),
                new Coordinate(min.X, min.Y)
            }));

            if (geometry.Intersects(rect))
            {
                if (((iRowMax - iRowMin) < 2 && (jColMax - jColMin) < 2) || geometry.Contains(rect))
                {
                    for (int iRow = iRowMin; iRow <= iRowMax; iRow++)
                    {
                        for (int jCol = jColMin; jCol <= jColMax; jCol++)
                        {
                            _raster[iRow][jCol] = rasterValue;
                        }
                    }
                }
                else
                {
                    // Split the rectangle into four parts and check each part recursively.

                    int iRowCenter = iRowMax, jColCenter = jColMax;

                    if ((iRowMax - iRowMin) > 1)
                        iRowCenter = (int)Math.Ceiling((iRowMax + iRowMin) / 2D);
                    if ((jColMax - jColMin) > 1)
                        jColCenter = (int)Math.Ceiling((jColMax + jColMin) / 2D);

                    SetValueIfInside(iRowMin, iRowCenter, jColMin, jColCenter, geometry, rasterValue);

                    if (jColCenter != jColMax)
                        SetValueIfInside(iRowMin, iRowCenter, jColCenter, jColMax, geometry, rasterValue);

                    if (iRowCenter != iRowMax)
                        SetValueIfInside(iRowCenter, iRowMax, jColMin, jColCenter, geometry, rasterValue);

                    if (iRowCenter != iRowMax && jColCenter != jColMax)
                        SetValueIfInside(iRowCenter, iRowMax, jColCenter, jColMax, geometry, rasterValue);
                }
            }
        }

        public void WriteAsAscii(string fullFileName)
        {
            using StreamWriter file = new (fullFileName);

            file.WriteLine("ncols         " + _bounds.ColumnCount);
            file.WriteLine("nrows         " + _bounds.RowCount);
            file.WriteLine("xllcorner     " + _bounds.MinX);
            file.WriteLine("yllcorner     " + _bounds.MinY);
            file.WriteLine("cellsize      " + _bounds.CellWidth);
            file.WriteLine("NODATA_value  " + _iNoDataValue);

            for (int iRow = _bounds.RowCount - 1; iRow >= 0; --iRow)
            {
                file.WriteLine(String.Join(" ", _raster[iRow]));
            }
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

        public static Rasteriser CreateFromAscii(string fullFileName)
        {
            Rasteriser ras = new ();

            char[] delimiters = new char[] {' ', '\t'};

            int nRows = -1, nCols = -1, minX = -1, minY = -1, cellSize = -1, noDataValue = -1;

            using (StreamReader file = new (fullFileName))
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
                                throw new Exception("Invalid format in header " + fullFileName);

                            Envelope extent = new (minX, minX + nCols, minY, minY + nRows);
                            ras.CreaterRaster(extent);
                            IsHeaderRead = true;
                            iRow = nRows;
                        }
                    }

                    if (IsHeaderRead)
                    {
                        if (iRow < 0)
                            throw new Exception(String.Format("File {0} contains too many data rows", fullFileName));

                        if (words.Length != nCols)
                        {
                            throw new Exception(String.Format("File {0} contains {1} colums on line {2}",
                                fullFileName, words.Length, nRows - 1 - iRow));
                        }

                        ras._raster[--iRow] = Array.ConvertAll(words, byte.Parse);
                    }


                }

                if (iRow < 0)
                    throw new Exception(String.Format("File {0} contains too few data rows", fullFileName));
            }

            return ras;
        }

        private void CreaterRaster(Envelope extent)
        {
            _bounds = new RasterBounds((int)extent.Height, (int)extent.Width, extent);
            _raster = new byte[_bounds.RowCount][];

            for (int iRow = 0; iRow < _bounds.RowCount; iRow++)
            {
                _raster[iRow] = new byte[_bounds.ColumnCount];
                for (int jCol = 0; jCol < _bounds.ColumnCount; jCol++)
                    _raster[iRow][jCol] = _iNoDataValue;
            }
        }

        public double GetHeight(double x, double y)
        {
            RcIndex rc = _bounds.ProjToCell(new Coordinate(x, y));

            if (rc == RcIndex.Empty)
            {
               // Console.WriteLine("Coordinate out of bounds " + x + " " + y);
                return double.NaN;
            }

            if (_raster[rc.Row][rc.Column] == _iNoDataValue)
                return double.NaN;

            return _raster[rc.Row][rc.Column];
        }
    }
}
