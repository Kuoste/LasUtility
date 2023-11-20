using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;
using NetTopologySuite.IO.Esri;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LasUtility.Common;

namespace LasUtility.ShapefileRasteriser
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
                using ShapefileReader reader = Shapefile.OpenRead(filename);
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

        public void AddRasterizedClassesWithRasterValues(Dictionary<int, byte> classesToRasterValues)
        {
            // Join the dictionaries
            _nlsClassesToRasterValues = _nlsClassesToRasterValues.Concat(classesToRasterValues)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public void RemoveRasterizedClassesWithRasterValues(Dictionary<int, byte> classesToRasterValues)
        {
            foreach (var item in classesToRasterValues)
            {
                _nlsClassesToRasterValues.Remove(item.Key);
            }
        }
    }
}
