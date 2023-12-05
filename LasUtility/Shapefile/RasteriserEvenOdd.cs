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
    public class RasteriserEvenOdd: Rasteriser, IShapefileRasteriser
    {
        /// <summary>
        /// Use temporary raster when rasterising cannot be done in-place, e.g. polygons with holes.
        /// </summary>
        private ByteRaster _tempRaster;

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

                if (feature.Geometry is MultiPolygon mp)
                {
                    for (int i = 0; i < mp.NumGeometries; i++)
                    {
                        ProcessPolygon(rasterValue, iMin, iMax, (Polygon)mp.GetGeometryN(i));
                    }
                }
                else if (feature.Geometry is MultiLineString mls)
                {
                    for (int i = 0; i < mls.NumGeometries; i++)
                    {
                        ProcessLine(rasterValue, (LineString)mls.GetGeometryN(i));
                    }
                }
                else
                {
                    throw new Exception("Unsupported geometry");
                }
            }
        }

        private void ProcessLine(byte rasterValue, LineString ls)
        {
            CoordinateSequence coordinateSequence = ls.CoordinateSequence;

            for (int i = 1; i < coordinateSequence.Count; i++)
            {
                Coordinate c0 = coordinateSequence.GetCoordinate(i - 1);
                Coordinate c1 = coordinateSequence.GetCoordinate(i);

                RcIndex iMin = Bounds.ProjToCell(c0);
                RcIndex iMax = Bounds.ProjToCell(c1);

                foreach ((int x, int y) in MathUtils.Line(iMin.Column, iMin.Row, iMax.Column, iMax.Row))
                {
                    Raster[y][x] = rasterValue;
                }
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

            MathUtils.FillPolygon(Bounds, destRaster, rasterValue, p.ExteriorRing);

            // Fill holes back to the background value
            for (int i = 0; i < p.NumInteriorRings; i++)
            {
                MathUtils.FillPolygon(Bounds, destRaster, NoDataValue, p.GetInteriorRingN(i));
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
}
