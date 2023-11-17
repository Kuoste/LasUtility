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
    public class RasteriserEvenOdd: HeightMap, IShapefileRasteriser
    {
        private Dictionary<int, byte> _nlsClassesToRasterValues = new();

        private CancellationToken _token;

        private const int MAX_POLY_CORNERS = 10000;
        private readonly int[] _nodeX = new int[MAX_POLY_CORNERS];

        /// <summary>
        /// Use temporary raster when rasterising cannot be done in-place, e.g. polygons with holes.
        /// </summary>
        private HeightMap _tempRaster;

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

                MultiPolygon multiPolygon = (MultiPolygon)feature.Geometry;

                if (multiPolygon.NumGeometries > 1)
                    throw new Exception($"RasteriserEvenOdd: Only multipolygons with single geometry are supported.");

                Polygon p = (Polygon)multiPolygon.GetGeometryN(0);

                Envelope envelope = p.ExteriorRing.EnvelopeInternal;

                // No need to substract epsilon from the max values since the shapefile coordinates
                // do not contain the upper limits.

                RcIndex iMin = Bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
                RcIndex iMax = Bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

                // Skip if outside the raster.
                if (iMin == RcIndex.Empty || iMax == RcIndex.Empty)
                    continue;

                // If polygon has holes, use temporary raster so that previous values inside holes (=interiorRings) are not lost
                bool bUseSeparateRaster = p.NumInteriorRings > 0;

                HeightMap destRaster = this;

                if (true == bUseSeparateRaster)
                {
                    if (null == _tempRaster)
                    {
                        _tempRaster = new HeightMap();
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
        }

        private void FillPolygon(HeightMap dest, byte rasterValue, LineString line)
        {
            Envelope envelope = line.EnvelopeInternal;

            RcIndex iMin = Bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
            RcIndex iMax = Bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

            int IMAGE_TOP = iMin.Row;
            int IMAGE_BOT = iMax.Row;
            int IMAGE_LEFT = iMin.Column;
            int IMAGE_RIGHT = iMax.Column;
            int polyCorners = line.NumPoints;

            if (polyCorners > MAX_POLY_CORNERS)
                throw new Exception($"RasteriserEvenOdd: Cannot process polygons with more than {MAX_POLY_CORNERS} corners.");

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

        private void FillPolygonInt(HeightMap dest, byte rasterValue, int IMAGE_TOP, int IMAGE_BOT, int IMAGE_LEFT, int IMAGE_RIGHT, int polyCorners, double[] polyX, double[] polyY)
        {
            // Originally from http://alienryderflex.com/polygon_fill/
            //  public-domain code by Darel Rex Finley, 2007

            int nodes, pixelX, pixelY, i, j, swap;

            //Loop through the rows of the image.
            for (pixelY = IMAGE_TOP; pixelY < IMAGE_BOT; pixelY++)
            {
                //  Build a list of nodes.
                nodes = 0; j = polyCorners - 1;

                for (i = 0; i < polyCorners; i++)
                {
                    if (polyY[i] < pixelY && polyY[j] >= pixelY || polyY[j] < pixelY && polyY[i] >= pixelY)
                    {
                        _nodeX[nodes++] = (int)(polyX[i] + (pixelY - polyY[i]) / (polyY[j] - polyY[i])
                        * (polyX[j] - polyX[i]));
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
