using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LasUtility.VoxelGrid
{
    [MessagePackObject]
    public class Bin
    {
        [Key(0)]
        public BinPoint SurfaceReference { get; set; }
        [Key(1)]
        public BinPoint GroundReference { get; set; }

        [Key(2)]
        public List<BinPoint> GroundPoints { get; set; }
        [Key(3)]
        public List<BinPoint> OtherPoints { get; set; }

        public Bin()
        {
            GroundPoints = new List<BinPoint>(2);
            OtherPoints = new List<BinPoint>(2);
        }

        public void AddPoint(double z, byte classification, bool IsGroundPoint)
        {
            BinPoint point = new () { Z = z, Class = classification };

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

    [MessagePackObject]
    public class BinPoint : IComparable<BinPoint>
    {
        [Key(0)]
        public int ZInCentimeters { get; set; }

        [Key(1)]
        public byte Class { get; set; }

        [IgnoreMember]
        public double Z
        {
            get { return ZInCentimeters / 100D; }
            set { ZInCentimeters = (int)Math.Round(value * 100); }
        }

        public int CompareTo(BinPoint b)
        {
            return ZInCentimeters.CompareTo(b.ZInCentimeters);
        }
    }
}
