using LasUtility.Common;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Esri;
using System.Linq;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;
using System.Threading;
using OpenCvSharp;
using System.Diagnostics;

namespace LasUtility.ShapefileRasteriser
{
    public class RasteriserEvenOdd: ByteRaster, IShapefileRasteriser
    {
        private Dictionary<int, byte> _nlsClassesToRasterValues = new();

        private CancellationToken _token;

        private const int iMaxNodesPerRow = 100;
        private readonly int[] _nodeX = new int[iMaxNodesPerRow];

        /// <summary>
        /// Use temporary raster when rasterising cannot be done in-place, e.g. polygons with holes.
        /// </summary>
        private ByteRaster _tempRaster;

        public void SetCancellationToken(CancellationToken token)
        {
            _token = token;
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

            InitializeRaster(extent);
        }

        public void AddRasterizedClassesWithRasterValues(Dictionary<int, byte> classesToRasterValues)
        {
            // Join the dictionaries
            _nlsClassesToRasterValues = _nlsClassesToRasterValues.Concat(classesToRasterValues)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public void RemoveRasterizedClassesWithRasterValues(Dictionary<int, byte> classesToRasterValues)
        {
            foreach (var item in classesToRasterValues)
            {
                _nlsClassesToRasterValues.Remove(item.Key);
            }
        }

        public void RasteriseShapefile(string filename)
        {
            if (null != _token && _token.IsCancellationRequested)
                return;


            foreach (Feature feature in Shapefile.ReadAllFeatures(filename))
            {
                int classification = (int)(long)feature.Attributes["LUOKKA"];

                if (!_nlsClassesToRasterValues.ContainsKey(classification))
                    continue;

                byte rasterValue = _nlsClassesToRasterValues[classification];

                Envelope envelope = feature.Geometry.EnvelopeInternal;

                // No need to substract epsilon from the max values since the shapefile coordinates
                // do not contain the upper limits.

                RcIndex iMin = Bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
                RcIndex iMax = Bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

                // Skip if outside the raster.
                if (iMin == RcIndex.Empty || iMax == RcIndex.Empty)
                    continue;

                if (feature.Geometry is MultiPolygon)
                {
                    MultiPolygon multiPolygon = (MultiPolygon)feature.Geometry;

                    for (int i = 0; i < multiPolygon.NumGeometries; i++)
                    {
                        Polygon p = (Polygon)multiPolygon.GetGeometryN(0);
                        ProcessPolygon(rasterValue, iMin, iMax, p);
                    }
                }
                else if (feature.Geometry is MultiLineString)
                {
                    MultiLineString multiLineString = (MultiLineString)feature.Geometry;

                    for (int i = 0; i < multiLineString.NumGeometries; i++)
                    {
                        LineString l = (LineString)multiLineString.GetGeometryN(0);

                        ProcessLine(rasterValue, l);
                    }
                }
                else
                {
                    throw new Exception("Unsupported geometry");
                }
            }
        }

        private void ProcessLine(byte rasterValue, LineString l)
        {
            CoordinateSequence coordinateSequence = l.CoordinateSequence;

            for (int i = 1; i < coordinateSequence.Count; i++)
            {
                Coordinate c0 = coordinateSequence.GetCoordinate(i - 1);
                Coordinate c1 = coordinateSequence.GetCoordinate(i);

                RcIndex iMin = Bounds.ProjToCell(c0);
                RcIndex iMax = Bounds.ProjToCell(c1);

                foreach ((int x, int y) c in Line(iMin.Column, iMin.Row, iMax.Column, iMax.Row))
                {
                    Raster[c.y][c.x] = rasterValue;
                }
            }
        }

        public static IEnumerable<(int x, int y)> Line(int ax, int ay, int bx, int by)
        {
            // https://gist.github.com/Pyr3z/46884d67641094d6cf353358566db566

            /*!****************************************************************************
             * \file      RasterLineTo.cs
             * \author    Levi Perez (levianperez\@gmail.com)
             * \author    Jack Elton Bresenham (IBM, 1962)
             * 
             * \copyright None. I didn't invent this algorithm, and neither did you.
             *            If I had to choose a license, it would be <https://unlicense.org>.
             *****************************************************************************/

            // declare all locals at the top so it's obvious how big the footprint is
            int dx, dy, xinc, yinc, side, i, error;

            // starting cell is always returned
            yield return (ax, ay);

            xinc = (bx < ax) ? -1 : 1;
            yinc = (by < ay) ? -1 : 1;
            dx = xinc * (bx - ax);
            dy = yinc * (by - ay);

            if (dx == dy) // Handle perfect diagonals
            {
                // I include this "optimization" for more aesthetic reasons, actually.
                // While Bresenham's Line can handle perfect diagonals just fine, it adds
                // additional cells to the line that make it not a perfect diagonal
                // anymore. So, while this branch is ~twice as fast as the next branch,
                // the real reason it is here is for style.

                // Also, there *is* the reason of performance. If used for cell-based
                // raycasts, for example, then perfect diagonals will check half as many
                // cells.

                while (dx-- > 0)
                {
                    ax += xinc;
                    ay += yinc;
                    yield return (ax, ay);
                }

                yield break;
            }

            // Handle all other lines

            side = -1 * ((dx == 0 ? yinc : xinc) - 1);

            i = dx + dy;
            error = dx - dy;

            dx *= 2;
            dy *= 2;

            while (i-- > 0)
            {
                if (error > 0 || error == side)
                {
                    ax += xinc;
                    error -= dy;
                }
                else
                {
                    ay += yinc;
                    error += dx;
                }

                yield return (ax, ay);
            }
        }

        private void ProcessPolygon(byte rasterValue, RcIndex iMin, RcIndex iMax, Polygon p)
        {
            // If polygon has holes, use temporary raster so that previous values inside holes (=interiorRings) are not lost
            bool bUseSeparateRaster = p.NumInteriorRings > 0;

            ByteRaster destRaster = this;

            if (true == bUseSeparateRaster)
            {
                if (null == _tempRaster)
                {
                    _tempRaster = new ByteRaster();
                    _tempRaster.InitializeRaster(Bounds.RowCount, Bounds.ColumnCount,
                        new Envelope(Bounds.MinX, Bounds.MaxX, Bounds.MinY, Bounds.MaxY));
                }

                destRaster = _tempRaster;
            }

            FillPolygon(destRaster, rasterValue, p.ExteriorRing);

            // Fill holes back to the background value
            for (int i = 0; i < p.NumInteriorRings; i++)
            {
                FillPolygon(destRaster, NoDataValue, p.GetInteriorRingN(i));
            }

            if (true == bUseSeparateRaster)
            {
                // Copy values from temporary raster and reset it

                for (int iRow = iMin.Row; iRow < iMax.Row; iRow++)
                {
                    for (int jCol = iMin.Column; jCol < iMax.Column; jCol++)
                    {
                        byte value = _tempRaster.Raster[iRow][jCol];

                        if (value != NoDataValue)
                        {
                            Raster[iRow][jCol] = value;
                            _tempRaster.Raster[iRow][jCol] = NoDataValue;
                        }
                    }
                }
            }
        }

        private void FillPolygon(ByteRaster dest, byte rasterValue, LineString line)
        {
            Envelope envelope = line.EnvelopeInternal;

            RcIndex iMin = Bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
            RcIndex iMax = Bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

            int IMAGE_TOP = iMin.Row;
            int IMAGE_BOT = iMax.Row;
            int IMAGE_LEFT = iMin.Column;
            int IMAGE_RIGHT = iMax.Column;
            int polyCorners = line.NumPoints;

            double[] polyX = new double[polyCorners];
            double[] polyY = new double[polyCorners];

            for (int pc = 0; pc < polyCorners; pc++)
            {
                var point = line.GetPointN(pc);
                RcIndex rc = Bounds.ProjToCell(new Coordinate(point.X, point.Y));
                polyX[pc] = rc.Column;
                polyY[pc] = rc.Row;
            }

            FillPolygonInt(dest, rasterValue, IMAGE_TOP, IMAGE_BOT, IMAGE_LEFT, IMAGE_RIGHT, polyCorners, polyX, polyY);
        }

  

        private void FillPolygonInt(ByteRaster dest, byte rasterValue, int IMAGE_TOP, int IMAGE_BOT, int IMAGE_LEFT, int IMAGE_RIGHT, int polyCorners, double[] polyX, double[] polyY)
        {
            // Originally from http://alienryderflex.com/polygon_fill/
            //  public-domain code by Darel Rex Finley, 2007

            int nodes, pixelX, pixelY, i, j, swap;

            //Loop through the rows of the image.
            for (pixelY = IMAGE_TOP; pixelY < IMAGE_BOT; pixelY++)
            {
                //  Build a list of nodes.
                nodes = 0; j = polyCorners - 1;

                if (nodes > iMaxNodesPerRow)
                    throw new Exception($"RasteriserEvenOdd: Cannot process polygons with more than {iMaxNodesPerRow} edges per row.");

                for (i = 0; i < polyCorners; i++)
                {
                    if (polyY[i] < pixelY && polyY[j] >= pixelY || polyY[j] < pixelY && polyY[i] >= pixelY)
                    {
                        _nodeX[nodes++] = (int)(polyX[i] + (pixelY - polyY[i]) / (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]));
                    }

                    j = i;
                }

                //  Sort the nodes, via a simple “Bubble” sort.
                i = 0;

                while (i < nodes - 1)
                {
                    if (_nodeX[i] > _nodeX[i + 1])
                    {
                        swap = _nodeX[i]; _nodeX[i] = _nodeX[i + 1]; _nodeX[i + 1] = swap;
                        if (i > 0)
                            i--;
                    }
                    else
                    {
                        i++;
                    }
                }

                //  Fill the pixels between node pairs.
                for (i = 0; i < nodes; i += 2)
                {
                    if (_nodeX[i] >= IMAGE_RIGHT)
                        break;

                    if (_nodeX[i + 1] > IMAGE_LEFT)
                    {
                        if (_nodeX[i] < IMAGE_LEFT)
                            _nodeX[i] = IMAGE_LEFT;

                        if (_nodeX[i + 1] > IMAGE_RIGHT)
                            _nodeX[i + 1] = IMAGE_RIGHT;

                        for (pixelX = _nodeX[i]; pixelX < _nodeX[i + 1]; pixelX++)
                        {
                            dest.Raster[pixelY][pixelX] = rasterValue;
                        }
                    }
                }
            }
        }
    }
}
