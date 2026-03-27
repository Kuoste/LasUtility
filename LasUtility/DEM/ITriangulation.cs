using LasUtility.Common;
using LasUtility.LAS;
using NetTopologySuite.Geometries;

namespace LasUtility.DEM
{
    /// <summary>
    /// Interface for 2D Delaunay triangulation with height interpolation.
    /// </summary>
    public interface ITriangulation : IRaster
    {
        int PointCount { get; }

        void AddPoint(LasPoint p);

        void AddPoint(int iRow, int jCol, double z, byte classification);

        void Create();

        void Clear();

        int GetTriangleCount();

        void GetTriangle(int i, out Coordinate c1, out Coordinate c2, out Coordinate c3);

        double GetHeightAndClass(double x, double y, out byte classification);

        void ExportToShp(string shpFilePath);

        /// <summary>
        /// Rasterizes the triangulation into the requested rasters using a triangle-push approach.
        /// Only fills DEM cells that are currently float.NaN.
        /// </summary>
        void RasteriseDem(RasteriseDemRequest request);
    }
}
