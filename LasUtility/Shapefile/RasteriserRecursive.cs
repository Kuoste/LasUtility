using LasUtility.Common;
using NetTopologySuite.Geometries;
using System;
using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Esri;

namespace LasUtility.ShapefileRasteriser
{
    public class RasteriserRecursive : Rasteriser, IShapefileRasteriser
    {
        public void RasteriseShapefile(string filename)
        {
            int nAdded = 0;
            int nTotal = 0;

            foreach (Feature feature in Shapefile.ReadAllFeatures(filename))
            {
                if (null != _token && _token.IsCancellationRequested)
                    return;

                nTotal++;

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
            if (null != _token && _token.IsCancellationRequested)
                return;

            Coordinate max = new(Bounds.CellTopRightToProj(iRowMax, jColMax));
            Coordinate min = new(Bounds.CellBottomLeftToProj(iRowMin, jColMin));

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
                            Raster[iRow][jCol] = rasterValue;
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
    }
}
