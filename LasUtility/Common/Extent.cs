//using NetTopologySuite.Geometries;
//using System;

//namespace LasUtility.Common
//{
//    internal class Extent
//    {
//        public Coordinate BottomLeft;
//        public Coordinate BottomRight;
//        public Coordinate TopLeft;
//        public Coordinate TopRight;

//        internal Extent(double minX, double minY, double maxX, double maxY)
//        {
//            BottomLeft = new Coordinate(minX, minY);
//            BottomRight = new Coordinate(maxX, minY);
//            TopLeft = new Coordinate(minX, maxY);
//            TopRight = new Coordinate(maxX, maxY);
//        }

//        public double Height { get { return TopLeft.Y - BottomLeft.Y; } }
//        public double Width { get {  return TopRight.X - TopLeft.X; } }

//        internal bool Contains(Coordinate coordinate)
//        {
//            return coordinate.X >= BottomLeft.X && coordinate.X <= BottomRight.X &&
//                coordinate.Y >= BottomLeft.Y && coordinate.Y <= TopLeft.Y;
//        }

//        internal void ExpandToInclude(Extent extent)
//        {
//            BottomLeft.X = Math.Min(BottomLeft.X, extent.BottomLeft.X);
//            BottomLeft.Y = Math.Min(BottomLeft.Y, extent.BottomLeft.Y);

//            BottomRight.X = Math.Max(BottomRight.X, extent.BottomRight.X);
//            BottomRight.Y = Math.Min(BottomRight.Y, extent.BottomRight.Y);

//            TopLeft.X = Math.Min(TopLeft.X, extent.TopLeft.X);
//            TopLeft.Y = Math.Max(TopLeft.Y, extent.TopLeft.Y);

//            TopRight.X = Math.Max(TopRight.X, extent.TopRight.X);
//            TopRight.Y = Math.Max(TopRight.Y, extent.TopRight.Y);
//        }
//    }
//}