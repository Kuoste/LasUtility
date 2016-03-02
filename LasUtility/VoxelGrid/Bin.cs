﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.VoxelGrid
{
    internal class Bin
    {
        int _referenceHeight;
        public List<BinPoint> GroundPoints { get; private set; }
        public List<BinPoint> OtherPoints { get; private set; }

        public void AddPoint(double z, byte classification)
        {
            if (GroundPoints == null || OtherPoints == null)
            {
                GroundPoints = new List<BinPoint>();
                OtherPoints = new List<BinPoint>();
            }

            BinPoint point = new BinPoint() { Z = z, Class = classification };

            if (classification == 2)
                GroundPoints.Add(point);
            else
                OtherPoints.Add(point);
        }

        public double ReferenceHeight
        {
            get { return _referenceHeight / 100D; }
            set { _referenceHeight = (int)(value * 100); }
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

    internal class BinPoint : IComparable<BinPoint>
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
