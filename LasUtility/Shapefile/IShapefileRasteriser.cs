using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace LasUtility.Shapefile
{
    public interface IShapefileRasteriser
    {
        public void InitializeRaster(int iRowCount, int iColumnCount, Envelope extent);

        public void InitializeRaster(string[] filenames);

        public void InitializeRaster(Envelope bounds);

        public void AddRasterizedClassesWithRasterValues(Dictionary<int, byte> classesToRasterValues);

        public void RemoveRasterizedClassesWithRasterValues(Dictionary<int, byte> classesToRasterValues);

        public void RasteriseShapefile(string filename);

        public void WriteAsAscii(string sFullFilename);

        public void SetCancellationToken(System.Threading.CancellationToken token);

    }
}
