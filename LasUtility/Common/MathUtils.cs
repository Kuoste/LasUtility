using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace LasUtility.Common
{
    public static class MathUtils
    {
        /// <summary>
        /// Gives IEnumerable to go through a line coordinate by coordinate. 
        /// Originally from https://gist.github.com/Pyr3z/46884d67641094d6cf353358566db566
        /// </summary>
        /// <param name="ax"> X coordinate of the traverse starting point</param>
        /// <param name="ay"> Y coordinate of the traverse starting point</param>
        /// <param name="bx"> X coordinate of the traverse ending point</param>
        /// <param name="by"> Y coordinate of the traverse ending point</param>
        /// <returns></returns>
        public static IEnumerable<(int x, int y)> Line(int ax, int ay, int bx, int by)
        {
            /*!****************************************************************************
             * \file      RasterLineTo.cs
             * \author    Levi Perez (levianperez\@gmail.com)
             * \author    Jack Elton Bresenham (IBM, 1962)
             * 
             * \copyright None. I didn't invent this algorithm, and neither did you.
             *            If I had to choose a license, it would be <https://unlicense.org>.
             *****************************************************************************/

            // declare all locals at the top so it's obvious how big the footprint is
            int dx, dy, xinc, yinc, side, i, error;

            // starting cell is always returned
            yield return (ax, ay);

            xinc = (bx < ax) ? -1 : 1;
            yinc = (by < ay) ? -1 : 1;
            dx = xinc * (bx - ax);
            dy = yinc * (by - ay);

            if (dx == dy) // Handle perfect diagonals
            {
                // I include this "optimization" for more aesthetic reasons, actually.
                // While Bresenham's Line can handle perfect diagonals just fine, it adds
                // additional cells to the line that make it not a perfect diagonal
                // anymore. So, while this branch is ~twice as fast as the next branch,
                // the real reason it is here is for style.

                // Also, there *is* the reason of performance. If used for cell-based
                // raycasts, for example, then perfect diagonals will check half as many
                // cells.

                while (dx-- > 0)
                {
                    ax += xinc;
                    ay += yinc;
                    yield return (ax, ay);
                }

                yield break;
            }

            // Handle all other lines

            side = -1 * ((dx == 0 ? yinc : xinc) - 1);

            i = dx + dy;
            error = dx - dy;

            dx *= 2;
            dy *= 2;

            while (i-- > 0)
            {
                if (error > 0 || error == side)
                {
                    ax += xinc;
                    error -= dy;
                }
                else
                {
                    ay += yinc;
                    error += dx;
                }

                yield return (ax, ay);
            }
        }

        public static void FillPolygon(IRasterBounds bounds, ByteRaster dest, byte rasterValue, LineString lsPolygon)
        {
            Envelope envelope = lsPolygon.EnvelopeInternal;

            RcIndex iMin = bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
            RcIndex iMax = bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

            if (iMin == RcIndex.Empty || iMax == RcIndex.Empty)
                throw new Exception("Polygon is larger than the bounds");

            int iCornerCount = lsPolygon.NumPoints;

            double[] polyX = new double[iCornerCount];
            double[] polyY = new double[iCornerCount];

            for (int i = 0; i < iCornerCount; i++)
            {
                RcIndex rc = bounds.ProjToCell(lsPolygon.GetCoordinateN(i));
                polyX[i] = rc.Column;
                polyY[i] = rc.Row;
            }

            FillPolygonInt(dest, rasterValue, iMax.Row, iMin.Row, iMin.Column, iMax.Column, iCornerCount, polyX, polyY);
        }

        private static void FillPolygonInt(ByteRaster dest, byte rasterValue, int IMAGE_TOP, int IMAGE_BOT, int IMAGE_LEFT, int IMAGE_RIGHT, int polyCorners, double[] polyX, double[] polyY)
        {
            // Originally from http://alienryderflex.com/polygon_fill/
            //  public-domain code by Darel Rex Finley, 2007

            const int iMaxNodesPerRow = 100;
            int iNodeCount, pixelX, pixelY, i, j, swap;
            int[] nodeX = new int[iMaxNodesPerRow];

            //Loop through the rows of the image.
            for (pixelY = IMAGE_BOT; pixelY <= IMAGE_TOP; pixelY++)
            {
                //  Build a list of nodes.
                iNodeCount = 0; j = polyCorners - 1;

                for (i = 0; i < polyCorners; i++)
                {
                    if (polyY[i] < pixelY && polyY[j] >= pixelY || polyY[j] < pixelY && polyY[i] >= pixelY)
                    {
                        if (iNodeCount > iMaxNodesPerRow)
                            throw new Exception($"Cannot process polygons with more than {iMaxNodesPerRow} edges per row.");

                        nodeX[iNodeCount++] = (int)(polyX[i] + (pixelY - polyY[i]) / (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]));
                    }

                    j = i;
                }

                //  Sort the nodes, via a simple “Bubble” sort.
                i = 0;

                while (i < iNodeCount - 1)
                {
                    if (nodeX[i] > nodeX[i + 1])
                    {
                        swap = nodeX[i];
                        nodeX[i] = nodeX[i + 1];
                        nodeX[i + 1] = swap;
                        if (i > 0)
                            i--;
                    }
                    else
                    {
                        i++;
                    }
                }

                //  Fill the pixels between node pairs.
                for (i = 0; i < iNodeCount; i += 2)
                {
                    for (pixelX = nodeX[i]; pixelX < nodeX[i + 1]; pixelX++)
                    {
                        dest.Raster[pixelY][pixelX] = rasterValue;
                    }
                }
            }
        }
    }
}
