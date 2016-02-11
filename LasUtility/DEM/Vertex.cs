using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.DEM
{
    internal class Vertex : IVertex
    {
        double[] _positionXY;

        public double Height { get; set; }

        public double[] Position
        {
            get
            {
                return _positionXY;
            }
            private set
            {
                _positionXY = value;
            }
        }

        public Vertex(double x, double y, double z)
        {
            Position = new double[] { x, y };
            Height = z;
        }
		
        //protected override Geometry DefiningGeometry
        //{
        //    get
        //    {
        //        return new EllipseGeometry
        //        {
        //            Center = new Point(Position[0], Position[1]),
        //            RadiusX = 1.5,
        //            RadiusY = 1.5
        //        };
        //    }
        //}

    }
}
