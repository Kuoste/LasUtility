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

        public static void FillPolygon(IRasterBounds bounds, ByteRaster dest, byte rasterValue, LineString polygonLine)
        {
            Envelope envelope = polygonLine.EnvelopeInternal;

            RcIndex iMin = bounds.ProjToCell(new(envelope.MinX, envelope.MinY));
            RcIndex iMax = bounds.ProjToCell(new(envelope.MaxX, envelope.MaxY));

            int IMAGE_TOP = iMin.Row;
            int IMAGE_BOT = iMax.Row;
            int IMAGE_LEFT = iMin.Column;
            int IMAGE_RIGHT = iMax.Column;
            int polyCorners = polygonLine.NumPoints;

            double[] polyX = new double[polyCorners];
            double[] polyY = new double[polyCorners];

            for (int pc = 0; pc < polyCorners; pc++)
            {
                var point = polygonLine.GetPointN(pc);
                RcIndex rc = bounds.ProjToCell(new Coordinate(point.X, point.Y));
                polyX[pc] = rc.Column;
                polyY[pc] = rc.Row;
            }

            FillPolygonInt(dest, rasterValue, IMAGE_TOP, IMAGE_BOT, IMAGE_LEFT, IMAGE_RIGHT, polyCorners, polyX, polyY);
        }

        private static void FillPolygonInt(ByteRaster dest, byte rasterValue, int IMAGE_TOP, int IMAGE_BOT, int IMAGE_LEFT, int IMAGE_RIGHT, int polyCorners, double[] polyX, double[] polyY)
        {
            // Originally from http://alienryderflex.com/polygon_fill/
            //  public-domain code by Darel Rex Finley, 2007

            const int iMaxNodesPerRow = 100;
            int nodes, pixelX, pixelY, i, j, swap;
            int[] nodeX = new int[iMaxNodesPerRow];

            //Loop through the rows of the image.
            for (pixelY = IMAGE_TOP; pixelY < IMAGE_BOT; pixelY++)
            {
                //  Build a list of nodes.
                nodes = 0; j = polyCorners - 1;

                if (nodes > iMaxNodesPerRow)
                    throw new Exception($"RasteriserEvenOdd: Cannot process polygons with more than {iMaxNodesPerRow} edges per row.");

                for (i = 0; i < polyCorners; i++)
                {
                    if (polyY[i] < pixelY && polyY[j] >= pixelY || polyY[j] < pixelY && polyY[i] >= pixelY)
                    {
                        nodeX[nodes++] = (int)(polyX[i] + (pixelY - polyY[i]) / (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]));
                    }

                    j = i;
                }

                //  Sort the nodes, via a simple “Bubble” sort.
                i = 0;

                while (i < nodes - 1)
                {
                    if (nodeX[i] > nodeX[i + 1])
                    {
                        swap = nodeX[i]; nodeX[i] = nodeX[i + 1]; nodeX[i + 1] = swap;
                        if (i > 0)
                            i--;
                    }
                    else
                    {
                        i++;
                    }
                }

                //  Fill the pixels between node pairs.
                for (i = 0; i < nodes; i += 2)
                {
                    if (nodeX[i] >= IMAGE_RIGHT)
                        break;

                    if (nodeX[i + 1] > IMAGE_LEFT)
                    {
                        if (nodeX[i] < IMAGE_LEFT)
                            nodeX[i] = IMAGE_LEFT;

                        if (nodeX[i + 1] > IMAGE_RIGHT)
                            nodeX[i + 1] = IMAGE_RIGHT;

                        for (pixelX = nodeX[i]; pixelX < nodeX[i + 1]; pixelX++)
                        {
                            dest.Raster[pixelY][pixelX] = rasterValue;
                        }
                    }
                }
            }
        }
    }
}
