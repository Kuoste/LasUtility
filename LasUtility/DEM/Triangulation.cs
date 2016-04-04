using DotSpatial.Data;
using DotSpatial.Topology;
using LasUtility.LAS;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.DEM
{
    public class SurfaceTriangulation : IHeightMap
    {
        List<Vertex> _vertices = new List<Vertex>();
        ITriangulation<Vertex, Cell<Vertex>> _tri;
        TriangleIndexGrid _grid;

        public void AddPoint(LasPoint p)
        {
            _vertices.Add(new Vertex(p.x, p.y, p.z, p.classification));
        }

        public void AddPoint(int iRow, int jCol, double z, byte classification)
        {
            Coordinate c = _grid.Bounds.CellCenter_ToProj(iRow, jCol);
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

            TriangulationComputationConfig config = new TriangulationComputationConfig
            {
                PointTranslationType = PointTranslationType.TranslateInternal,
                PlaneDistanceTolerance = 0.000000001,
                // the translation radius should be lower than PlaneDistanceTolerance / 2
                PointTranslationGenerator = TriangulationComputationConfig.RandomShiftByRadius(0.0000000001, 0)
            };

            _tri = Triangulation.CreateDelaunay<Vertex, Cell<Vertex>>(_vertices, config);

            for (int i = 0; i < _tri.Cells.Count(); i++)
            {
                var c = _tri.Cells.ElementAt(i);
                _grid.AddIndex(c.GetPolygon().Envelope, i);
            }
        }

        public double GetValue(double x, double y, out byte classification)
        {
            double ret = double.NaN;
            classification = 0;

            if (_grid == null)
                throw new InvalidOperationException("Triangulation is not created.");

            List<int> indexes = _grid.GetTriangleIndexesInCell(x, y);
            Point point = new Point(x, y);

            foreach (int i in indexes)
            {
                var cell = _tri.Cells.ElementAt(i);

                if (IsPointInPolygon(cell.GetPolygon(), point))
                {
                    ret = InterpolateHeightFromPolygon(cell.GetPolygon(), x, y);
                    point.Z = ret;
                    classification = GetClosestVertex(point, cell.Vertices).Class;
                    break;
                }
            }

            return ret;
        }

        private Vertex GetClosestVertex(Point point, Vertex[] vertices)
        {
            Vertex nearest = null;
            double minDistance = double.MaxValue;

            foreach (Vertex vertex in vertices)
            {
                double distance = point.HyperDistance(vertex.Coordinate);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = vertex;
                }
            }

            return nearest;
        }

        private bool IsPointInPolygon(Polygon polygon, Point point)
        {
            //return polygon.Contains(point);
            return polygon.Intersects(point);
        }

        private double InterpolateHeightFromPolygon(Polygon p, double x, double y)
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
            byte classification;
            return GetValue(x, y, out classification);
        }
    }
}
