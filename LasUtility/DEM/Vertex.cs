using DotSpatial.Topology;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.DEM
{
    internal class Vertex : IVertex, IComparable<Vertex>
    {
        double[] _positionXY;

        public double Height { get; set; }
        public byte Class { get; set; }

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

        public Coordinate Coordinate
        {
            get
            {
                return new Coordinate(_positionXY[0], _positionXY[1], Height);
            }
        }


        public Vertex(double x, double y, double z, byte classification)
        {
            Position = new double[] { x, y };
            Height = z;
            Class = classification;
        }

        public int CompareTo(Vertex other)
        {
            return Height.CompareTo(other.Height);
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
