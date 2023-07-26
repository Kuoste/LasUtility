using MIConvexHull;
using NetTopologySuite.Geometries;
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

        public CoordinateZ Coordinate
        {
            get
            {
                return new CoordinateZ(_positionXY[0], _positionXY[1], Height);
            }
        }

        public Point Point
        {
            get
            {
                return new Point(_positionXY[0], _positionXY[1], Height);
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

        public override string ToString()
        {
            return "(x" + _positionXY[0] + " y " + _positionXY[1] + " z " + Height + " class " + Class + ")";
        }
    }
}
