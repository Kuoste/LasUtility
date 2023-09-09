using System;
using System.Collections.Generic;
using System.Linq;

namespace LasUtility.VoxelGrid
{
    [Serializable]
    public class Bin
    {
        public BinPoint SurfaceReference { get; set; }
        public BinPoint GroundReference { get; set; }

        public List<BinPoint> GroundPoints { get; private set; }
        public List<BinPoint> OtherPoints { get; private set; }

        public Bin()
        {
            GroundPoints = new List<BinPoint>(2);
            OtherPoints = new List<BinPoint>(2);
        }

        public void AddPoint(double z, byte classification, bool IsGroundPoint)
        {
            BinPoint point = new BinPoint() { Z = z, Class = classification };

            if (IsGroundPoint)
                GroundPoints.Add(point);
            else
                OtherPoints.Add(point);
        }

        /// <summary>
        /// Unallocate unused memory reservations
        /// </summary>
        public void Trim()
        {
            GroundPoints.TrimExcess();
            OtherPoints.TrimExcess();
        }

        /// <summary>
        /// Sort points in order to get the median
        /// </summary>
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

        public BinPoint GetGroundMedianPoint()
        {
            BinPoint p = null;

            if (GroundPoints.Any())
                p = GroundPoints[GroundPoints.Count / 2];

            return p;
        }
    }

    [Serializable]
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
