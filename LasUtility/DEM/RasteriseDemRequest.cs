using LasUtility.Common;
using System;
using System.Collections.Generic;

namespace LasUtility.DEM
{
    /// <summary>
    /// Request object for rasterizing triangulation output into DEM and optional metadata rasters.
    /// </summary>
    public sealed class RasteriseDemRequest
    {
        public const string ClassificationMetadataName = "Classification";

        public float[,] Dem { get; }

        public IRasterBounds Bounds { get; }

        /// <summary>
        /// Optional mask for cells that must not be overwritten.
        /// </summary>
        public bool[,] LockedCells { get; set; }

        /// <summary>
        /// Optional byte metadata rasters keyed by metadata name.
        /// </summary>
        public IDictionary<string, byte[,]> ByteMetadata { get; }

        /// <summary>
        /// Optional float metadata rasters keyed by metadata name.
        /// </summary>
        public IDictionary<string, float[,]> FloatMetadata { get; }

        public RasteriseDemRequest(float[,] dem, IRasterBounds bounds)
        {
            if (dem == null)
                throw new ArgumentNullException(nameof(dem));

            if (bounds == null)
                throw new ArgumentNullException(nameof(bounds));

            Dem = dem;
            Bounds = bounds;
            ByteMetadata = new Dictionary<string, byte[,]>(StringComparer.Ordinal);
            FloatMetadata = new Dictionary<string, float[,]>(StringComparer.Ordinal);
        }
    }
}
