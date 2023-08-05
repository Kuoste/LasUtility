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

        public void AddPoint(LasPoint p)
        {
            _vertices.Add(new Vertex(p.x, p.y, p.z, p.classification));
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
                    if (IsPointInPolygon(cell.GetPolygon(), point))
                    {
                        ret = InterpolateHeightFromPolygon(cell.GetPolygon(), x, y);
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

        private static bool IsPointInPolygon(Polygon polygon, Point point)
        {
            //return polygon.Contains(point);
            return polygon.Intersects(point);
        }

        private static double InterpolateHeightFromPolygon(Polygon p, double x, double y)
        {
            Coordinate p1 = p.Coordinates[0];
            Coordinate p2 = p.Coordinates[1];
            Coordinate p3 = p.Coordinates[2];

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
