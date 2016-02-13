using DotSpatial.Topology;
using LasUtility.DEM;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasReader.DEM
{
    internal class Cell<TVertex> : TriangulationCell<TVertex, Cell<TVertex>> where TVertex : Vertex
    {
        public Polygon GetPolygon()
        {
            return new Polygon(new Coordinate[]
            {
                Vertices[0].Coordinate,
                Vertices[1].Coordinate,
                Vertices[2].Coordinate
            });
        }
    }
}
