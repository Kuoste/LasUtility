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
        public List<BinPoint> Points { get; set; }

        public Bin()
        {
            Points = new List<BinPoint>(1);
        }

        public void AddPoint(float z, byte classification)
        {
            Points.Add(new() { Z = z, Class = classification });
        }

        /// <summary>
        /// Unallocate unused memory reservations
        /// </summary>
        public void Trim()
        {
            Points.TrimExcess();
        }

        /// <summary>
        /// Sorts points
        /// </summary>
        public void OrderPointsFromHighestToLowest()
        {
            Points.Sort();
            Points.Reverse();
        }
    }

    [MessagePackObject]
    public class BinPoint : IComparable<BinPoint>
    {
        [Key(0)]
        public float Z { get; set; }

        [Key(1)]
        public byte Class { get; set; }

        public int CompareTo(BinPoint b)
        {
            return Z.CompareTo(b.Z);
        }

        public static bool operator <(BinPoint l, BinPoint r)
        {
            return l.Z < r.Z;
        }
        public static bool operator >(BinPoint l, BinPoint r)
        {
            return l.Z > r.Z;
        }
    }
}
