using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.VoxelGrid
{
    internal class Bin
    {
        int _referenceHeight;
        List<BinPoint> groundPoints;
        List<BinPoint> otherPoints;

        public Bin()
        {
            groundPoints = new List<BinPoint>();
            otherPoints = new List<BinPoint>();
        }

        public void AddPoint(double z, byte classification)
        {
            BinPoint point = new BinPoint() { Z = z };

            if (classification == 2)
                groundPoints.Add(point);
            else
                otherPoints.Add(point);
        }

        public double ReferenceHeight
        {
            get { return _referenceHeight / 100D; }
            set { _referenceHeight = (int)(value * 100); }
        }

        public void OrderPointsFromHighestToLowest()
        {

            groundPoints.Sort();
            otherPoints.Sort();

            groundPoints.Reverse();
            otherPoints.Reverse();
        }

        public double GetGroundMedian()
        {
            double ret = double.NaN;

            if (groundPoints.Any())
                ret = groundPoints[groundPoints.Count / 2].Z;

            return ret;
        }
    }

    internal class BinPoint : IComparable<BinPoint>
    {
        private int _z;

        //public byte Class { get; set; }

        public double Z
        {
            get { return _z / 100D; }
            set { _z = (int)Math.Round(value * 100); }
        }

        public int CompareTo(BinPoint b)
        {
            return _z.CompareTo(b._z);
        }
    }
}
