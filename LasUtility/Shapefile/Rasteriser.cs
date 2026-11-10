using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LasUtility.Common;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;

namespace LasUtility.Shapefile
{
    public class Rasteriser : ByteRaster
    {
        internal Dictionary<int, byte> _nlsClassesToRasterValues = new();

        internal CancellationToken _token;

        public void SetCancellationToken(CancellationToken token)
        {
            _token = token;
        }

        public void InitializeRaster(string[] filenames)
        {
            Envelope extent = new();

            foreach (var filename in filenames)
            {
                using ShapefileReader reader = NetTopologySuite.IO.Esri.Shapefile.OpenRead(filename);
                extent.ExpandToInclude(reader.BoundingBox);
            }

            // Expand to integer values to get cell size 1.0000000 meters 
            extent = new Envelope(
                Math.Floor(extent.MinX),
                Math.Ceiling(extent.MaxX),
                Math.Floor(extent.MinY),
                Math.Ceiling(extent.MaxY));

            InitializeRaster(extent);
        }

        /// <summary>
        /// Adds classes to rasterise. A class that is already added gets the new raster value.
        /// </summary>
        public void AddRasterizedClassesWithRasterValues(IReadOnlyDictionary<int, byte> classesToRasterValues)
        {
            foreach (var item in classesToRasterValues)
            {
                _nlsClassesToRasterValues[item.Key] = item.Value;
            }
        }

        public void RemoveRasterizedClassesWithRasterValues(IReadOnlyDictionary<int, byte> classesToRasterValues)
        {
            foreach (var item in classesToRasterValues)
            {
                _nlsClassesToRasterValues.Remove(item.Key);
            }
        }
    }
}
