using DotSpatial.Topology;
using LasReader.DEM;
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
            _vertices.Add(new Vertex(p.x, p.y, p.z));
        }

        public void Create(int nRows, int nCols, double minX, double minY, double maxX, double maxY)
        {
            if (!_vertices.Any())
                throw new InvalidOperationException("Add triangulation points before creating triangulation.");

            TriangulationComputationConfig config = new TriangulationComputationConfig
            {
                PointTranslationType = PointTranslationType.None,
                PlaneDistanceTolerance = 0.00000000001,
            };

            _tri = Triangulation.CreateDelaunay<Vertex, Cell<Vertex>>(_vertices, config);

            _grid = new TriangleIndexGrid(nRows, nCols, minX, minY, maxX, maxY);

            for (int i = 0; i < _tri.Cells.Count(); i++)
            {
                var c = _tri.Cells.ElementAt(i);
                _grid.AddIndex(c.GetPolygon().Envelope, i);
            }
        }

        public double GetValue(double x, double y)
        {
            if (_grid == null)
                throw new InvalidOperationException("Triangulation is not created.");

            List<int> indexes = _grid.GetTriangleIndexesInCell(x, y);
            Point c = new Point(x, y);

            foreach (int i in indexes)
            {
                var cell = _tri.Cells.ElementAt(i);
                Polygon p = cell.GetPolygon();

                if (p.Intersects(c))
                {
                    return InterpolateHeightFromPolygon(p, c);
                }
            }

            return double.NaN;
        }

        private double InterpolateHeightFromPolygon(Polygon p, Point c)
        {
            // todo: Find height from plane. Or from surface formed by adjacent cells.

            return ((p.Coordinates[0].Z + p.Coordinates[1].Z + p.Coordinates[2].Z) / 3);
        }

        public double GetHeight(double x, double y)
        {
            return GetValue(x, y);
        }
    }
}
