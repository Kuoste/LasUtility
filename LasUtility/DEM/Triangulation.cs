using LasUtility.Common;
using LasUtility.LAS;
using MIConvexHull;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LasUtility.DEM
{
    public class SurfaceTriangulation : IHeightMap
    {
        readonly List<Vertex> _vertices = new ();
        TriangleIndexGrid _grid;
        VoronoiMesh<Vertex, Cell<Vertex>, VoronoiEdge<Vertex, Cell<Vertex>>> _voronoiMesh;

        public void Clear()
        {
            _voronoiMesh = null;
            _vertices.Clear();
            _grid.ResetGrid();
            _grid = null;
        }

        public bool AddPoint(LasPoint p)
        {
            if (p.x < _grid.Bounds.MinX || p.x >= _grid.Bounds.MaxX ||
                p.y < _grid.Bounds.MinY || p.y >= _grid.Bounds.MaxY)
            {
                return false;
            }

            _vertices.Add(new Vertex(p.x, p.y, p.z, p.classification));
            return true;
        }

        public void AddPoint(int iRow, int jCol, double z, byte classification)
        {
            Coordinate c = _grid.Bounds.CellBottomLeftToProj(iRow, jCol);
            _vertices.Add(new Vertex(c.X, c.Y, z, classification));
        }

        public SurfaceTriangulation(int nRows, int nCols, double minX, double minY, double maxX, double maxY)
        {
            _grid = new TriangleIndexGrid(nRows, nCols, minX, minY, maxX, maxY);
        }

        public void Create()
        {
            if (!_vertices.Any())
                throw new InvalidOperationException("Add triangulation points before creating triangulation.");

            _grid.ResetGrid();

            _voronoiMesh = VoronoiMesh.Create<Vertex, Cell<Vertex>>(_vertices);

            for (int i = 0; i < _voronoiMesh.Vertices.Count(); i++)
            {
                var v = _voronoiMesh.Vertices.ElementAt(i);
                _grid.AddIndex(v.GetPolygon().EnvelopeInternal, i);
            }
        }

        public int GetTriangleCount()
        {
            if (null == _voronoiMesh)
                throw new InvalidOperationException("Triangulation is not created.");

            return _voronoiMesh.Vertices.Count();
        }

        public void GetTriangle(int i, out Coordinate c1, out Coordinate c2, out Coordinate c3)
        {
            if (null == _voronoiMesh)
                throw new InvalidOperationException("Triangulation is not created.");

            var v = _voronoiMesh.Vertices.ElementAt(i);
            c1 = v.Vertices[0].Coordinate;
            c2 = v.Vertices[1].Coordinate;
            c3 = v.Vertices[2].Coordinate;
        }

        public void ExportToShp(string shpFilePath)
        {
            List<Feature> features = new();

            int i = 0;
            foreach (var v in _voronoiMesh.Vertices)
            {
                var attributes = new AttributesTable
                {
                    { "ID", i++ },
                };

                features.Add(new Feature(v.GetPolygon(), attributes));
            }

            Shapefile.WriteAllFeatures(features, shpFilePath);
        }

        public double GetValue(double x, double y, out byte classification)
        {
            double ret = double.NaN;
            classification = 0;

            if (_grid == null)
                throw new InvalidOperationException("Triangulation is not created.");

            List<int> indexes = _grid.GetTriangleIndexesInCell(x, y);
            Point point = new (x, y);

            foreach (int i in indexes)
            {
                var cell = _voronoiMesh.Vertices.ElementAt(i);
                try
                {
                    if (IsPointInTriangle(point, cell.Vertices))
                    {
                        ret = InterpolateHeightFromTriangle(cell.Vertices, x, y);
                        point.Z = ret;
                        classification = GetClosestVertex(point, cell.Vertices).Class;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e + " Coords are " + String.Join(", ", cell.Vertices as IEnumerable<Vertex>));
                }
            }

            return ret;
        }

        private static Vertex GetClosestVertex(Point point, Vertex[] vertices)
        {
            Vertex nearest = null;
            double minDistance = double.MaxValue;

            foreach (Vertex vertex in vertices)
            {
                double distance = point.Distance(vertex.Point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = vertex;
                }
            }

            return nearest;
        }

        //private static bool IsPointInPolygon(Polygon polygon, Point point)
        //{
        //    //return polygon.Contains(point);
        //    return polygon.Intersects(point);
        //}

        private static double Sign(Point p1, Point p2, Point p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        /* 
         * From https://stackoverflow.com/a/2049593
         */
        private static bool IsPointInTriangle(Point pt, Vertex[] vertices)
        {
            if (vertices.Length != 3)
                throw new Exception("Triangle must contain exactly 3 points");

            double d1, d2, d3;
            bool has_neg, has_pos;

            d1 = Sign(pt, vertices[0].Point, vertices[1].Point);
            d2 = Sign(pt, vertices[1].Point, vertices[2].Point);
            d3 = Sign(pt, vertices[2].Point, vertices[0].Point);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }

        private static double InterpolateHeightFromTriangle(Vertex[] vertices, double x, double y)
        {
            if (vertices.Length != 3)
                throw new Exception("Triangle must contain exactly 3 points");

            Coordinate p1 = vertices[0].Point.Coordinate;
            Coordinate p2 = vertices[1].Point.Coordinate;
            Coordinate p3 = vertices[2].Point.Coordinate;

            double det = (p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y);

            double l1 = ((p2.Y - p3.Y) * (x - p3.X) + (p3.X - p2.X) * (y - p3.Y)) / det;
            double l2 = ((p3.Y - p1.Y) * (x - p3.X) + (p1.X - p3.X) * (y - p3.Y)) / det;
            double l3 = 1.0f - l1 - l2;

            return l1 * p1.Z + l2 * p2.Z + l3 * p3.Z;
        }

        public double GetHeightAndClass(double x, double y, out byte classification)
        {
            return GetValue(x, y, out classification);
        }

        public double GetHeight(double x, double y)
        {
            return GetValue(x, y, out _);
        }


    }
}
