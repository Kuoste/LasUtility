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
using System.Diagnostics;

namespace LasUtility.Shapefile
{
    public class RasteriserEvenOdd: Rasteriser, IShapefileRasteriser
    {
        /// <summary>
        /// Use temporary raster when rasterising cannot be done in-place, e.g. polygons with holes.
        /// </summary>
        private ByteRaster _tempRaster;

        public void RasteriseShapefile(string filename)
        {
            Envelope areaBounds = new(Bounds.MinX, Bounds.MaxX - Bounds.Epsilon, Bounds.MinY, Bounds.MaxY - Bounds.Epsilon);
            GeometryFactory factory = new();
            Geometry areaGeometry = factory.ToGeometry(areaBounds);

            foreach (Feature feature in NetTopologySuite.IO.Esri.Shapefile.ReadAllFeatures(filename))
            {
                if (null != _token && _token.IsCancellationRequested)
                    return;

                int classification = (int)(long)feature.Attributes["LUOKKA"];

                if (!_nlsClassesToRasterValues.ContainsKey(classification))
                    continue;

                byte rasterValue = _nlsClassesToRasterValues[classification];

                Geometry featureGeometry = feature.Geometry;
                Envelope envelope = featureGeometry.EnvelopeInternal;

                // Clip the geometry if it is (partly) outside the raster 
                if (areaBounds.Contains(envelope) == false)
                {
                    featureGeometry = featureGeometry.Intersection(areaGeometry);
                    envelope = featureGeometry.EnvelopeInternal;
                }

                RcIndex iMin = Bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
                RcIndex iMax = Bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

                if (iMin == RcIndex.Empty || iMax == RcIndex.Empty)
                    throw new Exception("Still outside raster");

                switch (featureGeometry)
                {
                    case Polygon p:
                        RasterisePolygon(rasterValue, iMin, iMax, p);
                        break;
                    case MultiPolygon mp:
                        for (int i = 0; i < mp.NumGeometries; i++)
                        {
                            RasterisePolygon(rasterValue, iMin, iMax, (Polygon)mp.GetGeometryN(i));
                        }
                        break;
                    case LineString ls:
                        RasteriseLine(rasterValue, ls);
                        break;
                    case MultiLineString mls:
                        for (int i = 0; i < mls.NumGeometries; i++)
                        {
                            RasteriseLine(rasterValue, (LineString)mls.GetGeometryN(i));
                        }
                        break;
                    default:
                        throw new Exception("Unsupported geometry " + featureGeometry.GeometryType);
                    case null:
                        throw new ArgumentNullException(nameof(featureGeometry));
                }
            }
        }

        private void RasteriseLine(byte rasterValue, LineString ls)
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

        private void RasterisePolygon(byte rasterValue, RcIndex iMin, RcIndex iMax, Polygon p)
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
