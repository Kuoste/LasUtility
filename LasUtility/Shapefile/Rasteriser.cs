﻿using DotSpatial.Data;
using DotSpatial.Topology;
using LasUtility.DEM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.Shapefile
{
    public class Rasteriser : IHeightMap
    {
        const double _resolution = 1D;
        const int _noDataValue = 0;

        Dictionary<int, sbyte> _nlsPolygonClasses;
        Dictionary<int, sbyte> _nlsLineClasses;

        sbyte[][] _raster;
        IRasterBounds _bounds;

        public Rasteriser()
        {
            _nlsPolygonClasses = new Dictionary<int, sbyte>()
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
            };

            _nlsLineClasses = new Dictionary<int, sbyte>()
            {
                {12111, 70}, //Autotie Ia // mc id 44
                {12112, 72}, //Autotie Ib
                {12121, 74}, //Autotie IIa
                {12122, 76}, //Autotie IIb
                {12131, 78}, //Autotie IIIa
                {12132, 80}, //Autotie IIIb
                {12141, 82}, //Ajotie
                {12151, 99}, //Lautta
                {12152, 99}, //Lossi
                {12153, 99}, //Huoltoaukko ilman puomia
                {12154, 99}, //Huoltoaukko puomilla
                {12312, 99}, //Talvitie
                {12313, 88}, //Polku
                {12314, 86}, //Kävely- ja pyörätie
                {12316, 84}  //Ajopolku
            };
        }



        public void AddShapefile(string filename)
        {
            IFeatureSet fs = FeatureSet.Open(filename);
            int nShapes = fs.NumRows();

            if (_raster == null)
                CreaterRaster(fs.Extent);

            Console.WriteLine("File {0} contains {1} shapes", Path.GetFileName(filename), nShapes);
            int nAdded = 0;
            long nCellsModified = 0;

            for (int i = 0; i < nShapes; i++)
            {
                Shape shape = fs.GetShape(i, true);

                int classification = (int)(long)shape.Attributes[2];
                sbyte rasterValue;

                if (_nlsPolygonClasses.ContainsKey(classification))
                    rasterValue = _nlsPolygonClasses[classification];
                else if (_nlsLineClasses.ContainsKey(classification))
                    rasterValue = _nlsLineClasses[classification];
                else
                    continue;

                IGeometry geometry = shape.ToGeometry();

                RcIndex iMin = _bounds.ProjToCell(geometry.Envelope.TopLeft());
                RcIndex iMax = _bounds.ProjToCell(geometry.Envelope.BottomRight());

                if (iMin == RcIndex.Empty || iMax == RcIndex.Empty)
                    continue;

                //if (geometry is LineString)
                //{
                //    LineString ls = geometry as LineString;
                //    geometry = ls.Buffer(2, BufferStyle.CapButt);
                //}

                nAdded++;
                SetValueIfInside(iMin.Row, iMax.Row, iMin.Column, iMax.Column, geometry, rasterValue);
            }

            Console.WriteLine("  of which {0} were added to {1} cells", nAdded, nCellsModified);
        }

        private void SetValueIfInside(int iRowMin, int iRowMax, int jColMin, int jColMax, IGeometry geometry, sbyte rasterValue)
        {
            Coordinate min = new Coordinate(_bounds.CellCenter_ToProj(iRowMin, jColMax));
            Coordinate max = new Coordinate(_bounds.CellCenter_ToProj(iRowMax, jColMin));

            IGeometry rect = new Envelope(min, max).ToPolygon();

            if (geometry.Intersects(rect))
            {
                if (((iRowMax - iRowMin) < 2 && (jColMax - jColMin) < 2) || geometry.Contains(rect))
                {
                    if (rasterValue >= 70 && rasterValue < 100)
                    {
                        if (iRowMin > 0)
                            iRowMin--;
                        if (jColMin > 0)
                            jColMin--;
                        if (jColMax < _bounds.NumColumns - 1)
                            jColMax++;
                        if (iRowMax < _bounds.NumRows - 1)
                            iRowMax++;
                    }

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
            using (StreamWriter file = new StreamWriter(fullFileName))
            {
                file.WriteLine("ncols         " + _bounds.NumColumns);
                file.WriteLine("nrows         " + _bounds.NumRows);
                file.WriteLine("xllcorner     " + _bounds.BottomLeft().X);
                file.WriteLine("yllcorner     " + _bounds.BottomLeft().Y);
                file.WriteLine("cellsize      " + _bounds.CellWidth);
                file.WriteLine("NODATA_value  " + _noDataValue);

                foreach (sbyte[] row in _raster)
                    file.WriteLine(String.Join(" ", row));
            }
        }

        public static Rasteriser CreateFromAscii(string fullFileName)
        {
            Rasteriser ras = new Rasteriser();

            char[] delimiters = new char[] {' ', '\t'};

            int nRows = -1, nCols = -1, minX = -1, minY = -1, cellSize = -1, noDataValue = -1;

            using (StreamReader file = new StreamReader(fullFileName))
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
                            minX = int.Parse(words[1]);
                        else if (words[0].ToUpper().Trim() == "YLLCORNER")
                            minY = int.Parse(words[1]);
                        else if (words[0].ToUpper().Trim() == "CELLSIZE")
                            cellSize = int.Parse(words[1]);
                        else if (words[0].ToUpper().Trim() == "NODATA_VALUE")
                            noDataValue = int.Parse(words[1]);
                        else
                        {
                            if (nRows < 0 || nCols < 0 || minX < 0 || minY < 0 || cellSize < 0)
                                throw new Exception("Invalid format in header " + fullFileName);

                            Extent extent = new Extent(minX, minY, minX + nCols, minY + nRows);
                            ras.CreaterRaster(extent);
                            IsHeaderRead = true;
                            iRow = 0;
                        }
                    }

                    if (IsHeaderRead)
                    {
                        if (iRow > nRows - 1)
                            throw new Exception(String.Format("File {0} contains too many data rows", fullFileName));

                        if (words.Count() != nCols)
                        {
                            throw new Exception(String.Format("File {0} contains {1} colums on line {2}",
                                fullFileName, words.Count(), nRows - 1 - iRow));
                        }

                        ras._raster[iRow] = Array.ConvertAll(words, sbyte.Parse);
                        iRow++;
                    }


                }

                if (iRow < nRows)
                    throw new Exception(String.Format("File {0} contains too few data rows", fullFileName));
            }

            return ras;
        }

        private void CreaterRaster(Extent extent)
        {
            _bounds = new RasterBounds((int)extent.Height, (int)extent.Width, extent);
            _raster = new sbyte[_bounds.NumRows][];

            for (int iRow = 0; iRow < _bounds.NumRows; iRow++)
            {
                _raster[iRow] = new sbyte[_bounds.NumColumns];
                for (int jCol = 0; jCol < _bounds.NumColumns; jCol++)
                    _raster[iRow][jCol] = _noDataValue;
            }
        }

        public double GetHeight(double x, double y)
        {
            RcIndex rc = _bounds.ProjToCell(new Coordinate(x, y));

            if (rc == RcIndex.Empty)
            {
                Console.WriteLine("Coordinate out of bounds " + x + " " + y);
                return double.NaN;
            }

            if (_raster[rc.Row][rc.Column] == _noDataValue)
                return double.NaN;

            return _raster[rc.Row][rc.Column];
        }
    }
}
