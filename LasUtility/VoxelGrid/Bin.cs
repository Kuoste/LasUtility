using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.VoxelGrid
{
    public class Bin
    {
        int _groundReferenceHeight;
        int _surfaceReferenceHeight;

        public List<BinPoint> GroundPoints { get; private set; }
        public List<BinPoint> OtherPoints { get; private set; }

        public Bin()
        {
            GroundPoints = new List<BinPoint>();
            OtherPoints = new List<BinPoint>();
        }

        public void AddPoint(double z, byte classification)
        {
            BinPoint point = new BinPoint() { Z = z, Class = classification };

            if (classification == 2)
                GroundPoints.Add(point);
            else
                OtherPoints.Add(point);
        }

        public double GroundReferenceHeight
        {
            get { return _groundReferenceHeight / 100D; }
            set { _groundReferenceHeight = (int)(value * 100); }
        }

        public double SurfaceReferenceHeight
        {
            get { return _surfaceReferenceHeight / 100D; }
            set { _surfaceReferenceHeight = (int)(value * 100); }
        }

        public void OrderPointsFromHighestToLowest()
        {

            GroundPoints.Sort();
            OtherPoints.Sort();

            GroundPoints.Reverse();
            OtherPoints.Reverse();
        }

        public double GetGroundMedian()
        {
            double ret = double.NaN;

            if (GroundPoints.Any())
                ret = GroundPoints[GroundPoints.Count / 2].Z;

            return ret;
        }
    }

    public class BinPoint : IComparable<BinPoint>
    {
        private int _z;

        public byte Class { get; set; }

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
