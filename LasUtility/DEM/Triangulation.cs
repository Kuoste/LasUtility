using DelaunatorSharp;
using LasUtility.Common;
using LasUtility.LAS;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace LasUtility.DEM
{
    /// <summary>
    /// 2D Delaunay triangulation using DelaunatorSharp.
    /// </summary>
    public class SurfaceTriangulation : ITriangulation
    {
        readonly List<double> _x = new ();
        readonly List<double> _y = new ();
        readonly List<double> _z = new ();
        readonly List<byte> _classifications = new ();

        DelaunatorSharp.Delaunator _delaunator;

        // Spatial index for point queries
        TriangleIndexGrid _grid;
        readonly bool _bUseIndexingOnTriangles;

        public int PointCount => _x.Count;

        public SurfaceTriangulation(int nRows, int nCols, double minX, double minY, double maxX, double maxY, bool bUseIndexingOnTriangles = true)
        {
            _bUseIndexingOnTriangles = bUseIndexingOnTriangles;
            _grid = new TriangleIndexGrid(nRows, nCols, minX, minY, maxX, maxY);
        }

        public void AddPoint(LasPoint p)
        {
            if (p.x < _grid.Bounds.MinX || p.x >= _grid.Bounds.MaxX ||
                p.y < _grid.Bounds.MinY || p.y >= _grid.Bounds.MaxY)
            {
                throw new Exception("Adding point that is out of bounds");
            }

            _x.Add(p.x);
            _y.Add(p.y);
            _z.Add(p.z);
            _classifications.Add(p.classification);
        }

        public void AddPoint(int iRow, int jCol, double z, byte classification)
        {
            Coordinate c = _grid.Bounds.CellBottomLeftToProj(iRow, jCol);
            _x.Add(c.X);
            _y.Add(c.Y);
            _z.Add(z);
            _classifications.Add(classification);
        }

        public void Create()
        {
            if (_x.Count == 0)
                throw new InvalidOperationException("Add triangulation points before creating triangulation.");

            // Build temporary IPoint array — Delaunator copies coords internally
            var points = new IPoint[_x.Count];
            for (int i = 0; i < _x.Count; i++)
                points[i] = new DelaunatorSharp.Point(_x[i], _y[i]);

            _delaunator = new DelaunatorSharp.Delaunator(points);

            if (_bUseIndexingOnTriangles)
                CreateGridIndexing();
        }

        private void CreateGridIndexing()
        {
            _grid.ResetGrid();

            int[] triangles = _delaunator.Triangles;
            int triangleCount = triangles.Length / 3;

            for (int t = 0; t < triangleCount; t++)
            {
                int i0 = triangles[t * 3];
                int i1 = triangles[t * 3 + 1];
                int i2 = triangles[t * 3 + 2];

                double x0 = _x[i0], x1 = _x[i1], x2 = _x[i2];
                double y0 = _y[i0], y1 = _y[i1], y2 = _y[i2];

                double minX = Math.Min(x0, Math.Min(x1, x2));
                double minY = Math.Min(y0, Math.Min(y1, y2));
                double maxX = Math.Max(x0, Math.Max(x1, x2));
                double maxY = Math.Max(y0, Math.Max(y1, y2));

                _grid.AddIndex(new Envelope(minX, maxX, minY, maxY), t);
            }
        }

        public void Clear()
        {
            _delaunator = null;
            _x.Clear();
            _y.Clear();
            _z.Clear();
            _classifications.Clear();
            _grid.ResetGrid();
            _grid = null;
        }

        public int GetTriangleCount()
        {
            if (_delaunator == null)
                throw new InvalidOperationException("Triangulation is not created.");

            return _delaunator.Triangles.Length / 3;
        }

        public void GetTriangle(int i, out Coordinate c1, out Coordinate c2, out Coordinate c3)
        {
            if (_delaunator == null)
                throw new InvalidOperationException("Triangulation is not created.");

            int i0 = _delaunator.Triangles[i * 3];
            int i1 = _delaunator.Triangles[i * 3 + 1];
            int i2 = _delaunator.Triangles[i * 3 + 2];

            c1 = new CoordinateZ(_x[i0], _y[i0], _z[i0]);
            c2 = new CoordinateZ(_x[i1], _y[i1], _z[i1]);
            c3 = new CoordinateZ(_x[i2], _y[i2], _z[i2]);
        }

        public void ExportToShp(string shpFilePath)
        {
            if (_delaunator == null)
                throw new InvalidOperationException("Triangulation is not created.");

            List<Feature> features = new();
            int[] triangles = _delaunator.Triangles;
            int triangleCount = triangles.Length / 3;

            for (int t = 0; t < triangleCount; t++)
            {
                int i0 = triangles[t * 3];
                int i1 = triangles[t * 3 + 1];
                int i2 = triangles[t * 3 + 2];

                Coordinate c0 = new CoordinateZ(_x[i0], _y[i0], _z[i0]);
                Coordinate c1 = new CoordinateZ(_x[i1], _y[i1], _z[i1]);
                Coordinate c2 = new CoordinateZ(_x[i2], _y[i2], _z[i2]);

                Polygon polygon = new (new LinearRing(new[] { c0, c1, c2, c0 }));
                var attributes = new AttributesTable { { "ID", t } };
                features.Add(new Feature(polygon, attributes));
            }

            NetTopologySuite.IO.Esri.Shapefile.WriteAllFeatures(features, shpFilePath);
        }

        public double GetValue(double x, double y, out byte classification)
        {
            classification = 0;

            if (!_bUseIndexingOnTriangles)
                throw new InvalidOperationException("Indexing on triangles is not enabled.");

            if (_delaunator == null)
                throw new InvalidOperationException("Triangulation is not created.");

            List<int> indexes = _grid.GetTriangleIndexesInCell(x, y);
            int[] triangles = _delaunator.Triangles;

            foreach (int t in indexes)
            {
                int i0 = triangles[t * 3];
                int i1 = triangles[t * 3 + 1];
                int i2 = triangles[t * 3 + 2];

                double x0 = _x[i0], y0 = _y[i0];
                double x1 = _x[i1], y1 = _y[i1];
                double x2 = _x[i2], y2 = _y[i2];

                if (IsPointInTriangle(x, y, x0, y0, x1, y1, x2, y2))
                {
                    double height = InterpolateHeight(
                        x0, y0, _z[i0],
                        x1, y1, _z[i1],
                        x2, y2, _z[i2],
                        x, y);

                    classification = GetClosestClassification(x, y, height, i0, i1, i2);
                    return height;
                }
            }

            return double.NaN;
        }

        /// <summary>
        /// Triangle-push rasterisation: iterates all triangles and fills covered grid cells.
        /// If lockedCells is provided, those cells are skipped and not updated. This allows for incremental updates to an existing DEM.
        /// </summary>
        public void RasteriseDem(RasteriseDemRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_delaunator == null)
                throw new InvalidOperationException("Triangulation is not created.");

            float[,] dem = request.Dem;
            IRasterBounds bounds = request.Bounds;
            bool[,] lockedCells = request.LockedCells;

            ValidateRasterDimensions(dem, nameof(request.Dem), bounds);

            if (lockedCells != null)
                ValidateRasterDimensions(lockedCells, nameof(request.LockedCells), bounds);

            byte[,] classificationRaster = null;
            foreach (KeyValuePair<string, byte[,]> item in request.ByteMetadata)
            {
                ValidateRasterDimensions(item.Value, $"request.ByteMetadata[\"{item.Key}\"]", bounds);

                if (item.Key == RasteriseDemRequest.ClassificationMetadataName)
                {
                    classificationRaster = item.Value;
                    continue;
                }

                throw new NotSupportedException($"Byte metadata rasterisation is not implemented for \"{item.Key}\".");
            }

            foreach (KeyValuePair<string, float[,]> item in request.FloatMetadata)
            {
                ValidateRasterDimensions(item.Value, $"request.FloatMetadata[\"{item.Key}\"]", bounds);
                throw new NotSupportedException($"Float metadata rasterisation is not implemented for \"{item.Key}\".");
            }

            int[] triangles = _delaunator.Triangles;
            int triangleCount = triangles.Length / 3;

            for (int t = 0; t < triangleCount; t++)
            {
                int i0 = triangles[t * 3];
                int i1 = triangles[t * 3 + 1];
                int i2 = triangles[t * 3 + 2];

                double x0 = _x[i0], y0 = _y[i0], z0 = _z[i0];
                double x1 = _x[i1], y1 = _y[i1], z1 = _z[i1];
                double x2 = _x[i2], y2 = _y[i2], z2 = _z[i2];

                // Bounding box clamped to grid bounds
                double minX = Math.Max(Math.Min(x0, Math.Min(x1, x2)), bounds.MinX);
                double minY = Math.Max(Math.Min(y0, Math.Min(y1, y2)), bounds.MinY);
                double maxX = Math.Min(Math.Max(x0, Math.Max(x1, x2)), bounds.MaxX - bounds.Epsilon);
                double maxY = Math.Min(Math.Max(y0, Math.Max(y1, y2)), bounds.MaxY - bounds.Epsilon);

                RcIndex rcMin = bounds.ProjToCell(new Coordinate(minX, minY));
                RcIndex rcMax = bounds.ProjToCell(new Coordinate(maxX, maxY));

                if (rcMin == RcIndex.Empty || rcMax == RcIndex.Empty)
                    continue;

                // Precompute barycentric denominator
                double det = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
                if (Math.Abs(det) < 1e-12)
                    continue; // Degenerate triangle

                double invDet = 1.0 / det;

                for (int iRow = rcMin.Row; iRow <= rcMax.Row; iRow++)
                {
                    for (int jCol = rcMin.Column; jCol <= rcMax.Column; jCol++)
                    {
                        if (lockedCells != null && lockedCells[iRow, jCol])
                            continue;

                        Coordinate c = bounds.CellBottomLeftToProj(iRow, jCol);
                        double px = c.X, py = c.Y;

                        // Barycentric coordinates
                        double l1 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) * invDet;
                        double l2 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) * invDet;
                        double l3 = 1.0 - l1 - l2;

                        // Point-in-triangle: all barycentric coords >= 0
                        if (l1 >= 0 && l2 >= 0 && l3 >= 0)
                        {
                            double height = l1 * z0 + l2 * z1 + l3 * z2;
                            dem[iRow, jCol] = (float)height;

                            if (classificationRaster != null)
                                classificationRaster[iRow, jCol] = GetClosestClassification(px, py, height, i0, i1, i2);
                        }
                    }
                }
            }
        }

        public double GetHeightAndClass(double x, double y, out byte classification)
        {
            return GetValue(x, y, out classification);
        }

        public double GetValue(Coordinate c)
        {
            return GetValue(c.X, c.Y, out _);
        }

        public double GetValue(int iRow, int jCol)
        {
            return GetValue(_grid.Bounds.CellBottomLeftToProj(iRow, jCol));
        }

        private byte GetClosestClassification(double x, double y, double z, int i0, int i1, int i2)
        {
            double d0 = DistanceSquared(x, y, z, _x[i0], _y[i0], _z[i0]);
            double d1 = DistanceSquared(x, y, z, _x[i1], _y[i1], _z[i1]);
            double d2 = DistanceSquared(x, y, z, _x[i2], _y[i2], _z[i2]);

            if (d0 <= d1 && d0 <= d2) return _classifications[i0];
            if (d1 <= d2) return _classifications[i1];
            return _classifications[i2];
        }

        private static void ValidateRasterDimensions(Array raster, string parameterName, IRasterBounds bounds)
        {
            if (raster == null)
                throw new ArgumentNullException(parameterName);

            if (raster.Rank != 2)
                throw new ArgumentException("Raster array must be two-dimensional.", parameterName);

            if (raster.GetLength(0) != bounds.RowCount || raster.GetLength(1) != bounds.ColumnCount)
                throw new ArgumentException("Raster array dimensions must match raster bounds.", parameterName);
        }

        private static double DistanceSquared(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            double dz = z1 - z2;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Point-in-triangle test using sign/cross-product method. No object allocations.
        /// </summary>
        private static bool IsPointInTriangle(double px, double py,
            double x0, double y0, double x1, double y1, double x2, double y2)
        {
            double d1 = (px - x1) * (y0 - y1) - (x0 - x1) * (py - y1);
            double d2 = (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
            double d3 = (px - x0) * (y2 - y0) - (x2 - x0) * (py - y0);

            bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }

        private static double InterpolateHeight(
            double x0, double y0, double z0,
            double x1, double y1, double z1,
            double x2, double y2, double z2,
            double px, double py)
        {
            double det = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);

            double l1 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / det;
            double l2 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / det;
            double l3 = 1.0 - l1 - l2;

            return l1 * z0 + l2 * z1 + l3 * z2;
        }
    }
}
