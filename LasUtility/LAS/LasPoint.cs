
namespace LasUtility.LAS
{
    /// <summary>
    /// A struct so that reading millions of points does not allocate an object per point.
    /// </summary>
    public struct LasPoint
    {
        public double x;
        public double y;
        public double z;
        public byte classification;
    }
}
