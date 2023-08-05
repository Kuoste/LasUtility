using LasUtility.DEM;
using MIConvexHull;
using NetTopologySuite.Geometries;

namespace LasUtility.DEM
{
    internal class Cell<TVertex> : TriangulationCell<TVertex, Cell<TVertex>> where TVertex : Vertex
    {
        Polygon _polygon;

        public Polygon GetPolygon()
        {
            if (_polygon == null)
            {
                _polygon = new Polygon(new LinearRing(new Coordinate[]
                {
                    Vertices[0].Coordinate,
                    Vertices[1].Coordinate,
                    Vertices[2].Coordinate,
                    Vertices[0].Coordinate
                }));
            }
            return _polygon;
        }
    }
}
