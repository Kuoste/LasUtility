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
    class SurfaceTriangulation
    {
        List<Vertex> vertices = new List<Vertex>();

        public void AddPoint(LasPoint p)
        {
            vertices.Add(new Vertex(p.x, p.y, p.z));
        }

        public void Create(int nRows, int nCols)
        {

            var tri = Triangulation.CreateDelaunay<Vertex>(vertices);

            TriangleIndexGrid grid = new TriangleIndexGrid(nRows, nCols);

            for (int i = 0; i < tri.Cells.Count(); i++)
            {
                var c = tri.Cells.ElementAt(i);
                grid.AddIndex(c.Vertices, i);
            }

        }
    }
}
