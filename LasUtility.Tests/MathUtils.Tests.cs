using LasUtility.Common;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LasUtility.Tests
{
    public class MathUtilsTests
    {
        [Fact]
        public void RasteriseTriangle()
        {
            const int iMinX = 10;
            const int iMinY = 10;
            const int iMaxX = 20;
            const int iMaxY = 20;
            const int iRowCount = 10;
            const int iColumnCount = 10;

            const byte bRasterValue = 10;

            IRasterBounds bounds = new RasterBounds(iRowCount, iColumnCount, iMinX, iMinY, iMaxX, iMaxY);

            ByteRaster byteRaster = new();
            byteRaster.InitializeRaster(iRowCount, iColumnCount, new Envelope(iMinX,iMaxX, iMinY, iMaxY));

            Coordinate[] cornersForTriangle = [ new (15, 15), new (19 , 15), new (19 , 19)];
            LineString ls = new(cornersForTriangle);

            MathUtils.FillPolygon(bounds, byteRaster, bRasterValue, ls);

            for (int x = iMinX; x < iMaxX;  x++)
            {
                for (int y = iMinY; y  < iMaxY; y++)
                {
                    double dExpectedValue = double.NaN;

                    // Check points inside the triangle
                    if (y == 16 && x > 15 && x < 19)
                        dExpectedValue = bRasterValue;

                    if (y == 17 && x > 16 && x < 19)
                        dExpectedValue = bRasterValue;

                    if (y == 18 && x > 17 && x < 19)
                        dExpectedValue = bRasterValue;

                    Assert.Equal(dExpectedValue, byteRaster.GetValue(new Coordinate(x, y)));
                }
            }
        }
    }
}
