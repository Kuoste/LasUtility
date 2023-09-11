using System;
using LasUtility.Common;
using NetTopologySuite.Geometries;

namespace LasUtility.NlsTileName
{
    public class NlsTileNamer
    {
        enum CharsTowardsNorth96000
        {
            K, L, M, N, P, Q, R, S, T, U, V, W, X
        }

        enum CharsTowardsEast6000_1 { B, D, F, H };
        enum CharsTowardsEast6000_0 { A, C, E, G };

        enum CharsTowardsEast1000_2 { _3, _6, _9 };
        enum CharsTowardsEast1000_1 { _2, _5, _8 };
        enum CharsTowardsEast1000_0 { _1, _4, _7 };


        // All consts defined here are from 
        // https://www.maanmittauslaitos.fi/kartat-ja-paikkatieto/kartat/osta-kartta/nain-loydat-oikean-karttalehden

        /// <summary>
        /// The min coordinate of the starting tile if it was full sized. Actual coordinate is -76000 + 192000/2 = 20000,
        /// but use this for simplier calculations.
        /// </summary>
        private const int _iStartOrigoEast = -76000;

        /// <summary>
        /// The min coordinate of the starting tile K2
        /// </summary>
        private const int _iStartOrigoNorth = 6570000;


        /// <summary>
        /// Smallest coordinate supported by the ETRS-TM35FIN system
        /// </summary>
        private const int _iMinEast = 20000;

        /// <summary>
        /// Smallest coordinate supported by the ETRS-TM35FIN system
        /// </summary>
        private const int _iMinNorth = _iStartOrigoNorth;

        /// <summary>
        /// Largest coordinate supported by the ETRS-TM35FIN system.
        /// </summary>
        private const int _iMaxEast = _iStartOrigoEast + 4 * _iStartSizeEast;

        /// <summary>
        /// Largest coordinate supported by the ETRS-TM35FIN system.
        /// </summary>
        private const int _iMaxNorth = _iStartOrigoNorth + 13 * _iStartSizeNorth;

        /// <summary>
        /// Largest supported tile edge length.
        /// </summary>
        private const int _iStartSizeEast = 192000;

        /// <summary>
        /// Largest supported tile edge length.
        /// </summary>
        private const int _iStartSizeNorth = 96000;

        /// <summary>
        /// Since eastward indexing starts from 2 (K2, L2, M2, ...) use this offset in calculations.
        /// </summary>
        private const int _iStartOffsetEast = 2;

        /// <summary>
        /// Calculates the coordinates of the map tile from the map tile name.
        /// Note that the max coordinates are actually already part of the next tile, so the accurate bounding box is [min, max[
        /// </summary>
        /// <param name="sMapTileName">The name of the map tile, e.g. L4 or Q5232G1_6 </param>
        /// <param name="iMinEast">Lower left corner east coordinate</param>
        /// <param name="iMinNorth">Lower left corner north coordinate</param>
        /// <param name="iMaxEast">Upper right corner east coordinate</param>
        /// <param name="iMaxNorth">Upper right corner north coordinate</param>
        /// <returns>True if ok</returns>
        /// <exception cref="Exception"></exception>
        public static bool Decode(string sMapTileName, out Envelope envelope)
        {
            if (sMapTileName == null || sMapTileName.Length < 2)
            {
                throw new Exception("Input string is null or less than 2 chars long");
            }

            sMapTileName = sMapTileName.ToUpper();
            int iStringIndex = 0;

            char c = sMapTileName[iStringIndex];

            int iIndexNorth = -1;
            string[] charsTowardsNorth = Enum.GetNames(typeof(CharsTowardsNorth96000));

            for (int i = 0; i < charsTowardsNorth.Length; i++)
            {
                if (charsTowardsNorth[i][0] == c)
                {
                    iIndexNorth = i;
                    break;
                }
            }

            if (iIndexNorth < 0)
                throw new Exception("Map tile has invalid first letter");

            iStringIndex++;
            if (int.TryParse(sMapTileName[iStringIndex].ToString(), out int iIndexEast) == false)
                throw new Exception("Map tile name must continue with digit after the first char");


            if (iIndexEast < 2 || iIndexEast > 6)
                throw new Exception("Digit must be between 2-6 on the second char on the map tile name");

            iIndexEast -= _iStartOffsetEast;

            int iOrigoEast = _iStartOrigoEast + iIndexEast * _iStartSizeEast;
            int iOrigoNorth = _iStartOrigoNorth + iIndexNorth * _iStartSizeNorth;

            int iSizeEast = _iStartSizeEast;
            int iSizeNorth = _iStartSizeNorth;


            bool bIsOk = DecodeRecursive(sMapTileName, iStringIndex, ref iSizeEast, ref iSizeNorth, ref iOrigoEast, ref iOrigoNorth);

            if (bIsOk == true)
            {
                envelope = new(iOrigoEast, iOrigoEast + iSizeEast,
                    iOrigoNorth, iOrigoNorth + iSizeNorth);
            }
            else
            {
                envelope = new(0, 0, 0, 0);
            }

            return bIsOk;

        }


        private static bool DecodeRecursive(string sMapTileName, int iStringIndex, ref int iSizeEast, ref int iSizeNorth, ref int iOrigoEast, ref int iOrigoNorth)
        {
            iStringIndex++;

            if (iStringIndex >= sMapTileName.Length)
            {
                // Finished 
                return true;
            }

            if (iSizeNorth == 12000 && iStringIndex == sMapTileName.Length - 1)
            {
                // Special handling: On 12 km north resolution, check if we are processing the end of the string and if it ends with L or R.
                // In that case the tile is from terrain database (Maastotietokanta)
                if (sMapTileName[iStringIndex] == 'L')
                {
                    iSizeEast /= 2;
                    return true;
                }
                if (sMapTileName[iStringIndex] == 'R')
                {
                    iSizeEast /= 2;
                    iOrigoEast += iSizeEast;
                    return true;
                }
            }

            if (iSizeNorth == 12000)
            {
                // Special handling: Parse square 6x6 km2 tiles that are in 8 parts named with letters A-H
                // B D F H
                // A C E G

                iSizeEast /= 4;
                iSizeNorth /= 2;

                switch (sMapTileName[iStringIndex])
                {
                    case 'A':
                        break;
                    case 'B':
                        iOrigoNorth += iSizeNorth;
                        break;
                    case 'C':
                        iOrigoEast += iSizeEast;
                        break;
                    case 'D':
                        iOrigoEast += iSizeEast;
                        iOrigoNorth += iSizeNorth;
                        break;
                    case 'E':
                        iOrigoEast += 2 * iSizeEast;
                        break;
                    case 'F':
                        iOrigoEast += 2 * iSizeEast;
                        iOrigoNorth += iSizeNorth;
                        break;
                    case 'G':
                        iOrigoEast += 3 * iSizeEast;
                        break;
                    case 'H':
                        iOrigoEast += 3 * iSizeEast;
                        iOrigoNorth += iSizeNorth;
                        break;
                    default:
                        throw new Exception("Tile name character should be between A-G at location " + iStringIndex);
                }
            }
            else if (iSizeNorth == 3000)
            {
                // Special handling: parse 3x3 km2 las tiles into 9 squares of area 1 km2
                // Tiles are indexes as (prefixed with an underscore)
                // 3 6 9
                // 2 5 8
                // 1 4 7

                iSizeEast = 1000;
                iSizeNorth = 1000;

                if (sMapTileName[iStringIndex] != '_')
                    throw new Exception("Tile name should contain an underscore at location " + iStringIndex);

                iStringIndex++;

                if (int.TryParse(sMapTileName[iStringIndex].ToString(), out int d) == false)
                    throw new Exception("Tile name should contain a digit at location " + iStringIndex);

                if (d < 1 || d > 9)
                    throw new Exception("Tile name should contain a digit between 1-9 at location " + iStringIndex);

                if (d < 4)
                {
                    if (d == 2)
                    {
                        iOrigoNorth += iSizeNorth;
                    }
                    else if (d == 3)
                    {
                        iOrigoNorth += 2 * iSizeNorth;
                    }
                }
                else if (d < 7)
                {
                    iOrigoEast += iSizeEast;

                    if (d == 5)
                    {
                        iOrigoNorth += iSizeNorth;
                    }
                    else if (d == 6)
                    {
                        iOrigoNorth += 2 * iSizeNorth;
                    }
                }
                else
                {
                    iOrigoEast += 2 * iSizeEast;

                    if (d == 8)
                    {
                        iOrigoNorth += iSizeNorth;
                    }
                    else if (d == 9)
                    {
                        iOrigoNorth += 2 * iSizeNorth;
                    }
                }
            }
            else
            {
                // Normal handling: parse from 4 divided parts indexed as
                // 2 4
                // 1 3

                iSizeEast /= 2;
                iSizeNorth /= 2;

                if (int.TryParse(sMapTileName[iStringIndex].ToString(), out int d) == false)
                {
                    throw new Exception("Tile name should have a digit at location " + iStringIndex);
                }

                if (d < 1 || d > 4)
                {
                    throw new Exception("Tile name should have a digit value between 0-4 at location " + iStringIndex);
                }

                if (d == 2)
                {
                    iOrigoNorth += iSizeNorth;
                }
                else if (d == 3)
                {
                    iOrigoEast += iSizeEast;
                }
                else if (d == 4)
                {
                    iOrigoEast += iSizeEast;
                    iOrigoNorth += iSizeNorth;
                }
            }

            return DecodeRecursive(sMapTileName, iStringIndex, ref iSizeEast, ref iSizeNorth, ref iOrigoEast, ref iOrigoNorth);
        }

        /// <summary>
        /// Returns the name of the wanted map tile
        /// </summary>
        /// <param name="iEast">Input coordinate</param>
        /// <param name="iNorth">Input coordinate</param>
        /// <param name="iWantedSizeNorth">Vertical edge length of the map tile in meters 1000-96000</param>
        /// <returns>Name of the map tile where the coordinate belongs to</returns>
        /// <exception cref="Exception"></exception>
        public static string Encode(int iEast, int iNorth, int iWantedSizeNorth)
        {
            // Highest map level

            string sMapTileName = String.Empty;

            if (iEast < _iMinEast || iEast >= _iMaxEast || iNorth < _iMinNorth || iNorth >= _iMaxNorth)
            {
                throw new Exception("Coordinates out of bounds");
            }

            // Determine the character that starts the map tile name
            int iIndexNorth = (iNorth - _iStartOrigoNorth) / _iStartSizeNorth;
            CharsTowardsNorth96000 c = (CharsTowardsNorth96000)iIndexNorth;
            sMapTileName += Enum.GetName(typeof(CharsTowardsNorth96000), c);

            // Determine digit that follows the char
            int iIndexEast = (iEast - _iStartOrigoEast) / _iStartSizeEast;
            sMapTileName += (iIndexEast + _iStartOffsetEast).ToString();

            // Now we have calculated the name of the highest level. Return if that is enough.
            if (iWantedSizeNorth >= _iStartSizeNorth)
            {
                return sMapTileName;
            }

            // Otherwise enter a recursive function that stops at wanted level
            return EncodeRecursive(iEast, iNorth, iWantedSizeNorth, _iStartSizeEast, _iStartSizeNorth,
                _iStartOrigoEast + iIndexEast * _iStartSizeEast, _iStartOrigoNorth + iIndexNorth * _iStartSizeNorth, sMapTileName);
        }

        private static string EncodeRecursive(int iEast, int iNorth, int iWantedSizeKmNorth, int iSizeEast, int iSizeNorth,
            int iOrigoEast, int iOrigoNorth, string sMapTileName)
        {

            int iNextOrigoEast = iOrigoEast;
            int iNextOrigoNorth = iOrigoNorth;

            if (iSizeNorth == 12000 && iSizeEast == 24000)
            {
                // Special handling: Create square 6x6 km2 tiles by dividing into 8 parts using letters A-H
                // B D F H
                // A C E G

                iSizeEast /= 4;
                iSizeNorth /= 2;

                if (((iNorth - iOrigoNorth) / iSizeNorth) == 0)
                {
                    // A C E G
                    int iIndexEast = (iEast - iOrigoEast) / iSizeEast;
                    CharsTowardsEast6000_0 c = (CharsTowardsEast6000_0)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast6000_0), c);

                    iNextOrigoEast += iIndexEast * iSizeEast;
                }
                else
                {
                    // B D F H

                    int iIndexEast = (iEast - iOrigoEast) / iSizeEast;
                    CharsTowardsEast6000_1 c = (CharsTowardsEast6000_1)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast6000_1), c);

                    iNextOrigoNorth += iSizeNorth;
                    iNextOrigoEast += iIndexEast * iSizeEast;
                }
            }
            else if (iSizeNorth == 3000 && iSizeEast == 3000)
            {
                // Special handling: We'we reached the lowest level currently possible and try to divide the 3x3 km2 LAS tiles
                // So instead of dividing into 4 parts, divide into 9 parts using following indexing preceding with an underscore
                // 3 6 9
                // 2 5 8
                // 1 4 7

                iSizeNorth = 1000;
                iSizeEast = 1000;

                int iIndexNorth = (iNorth - iOrigoNorth) / iSizeNorth;
                int iIndexEast = (iEast - iOrigoEast) / iSizeEast;

                if (iIndexNorth == 0)
                {
                    // 1 4 7
                    CharsTowardsEast1000_0 c = (CharsTowardsEast1000_0)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast1000_0), c);
                }
                else if (iIndexNorth == 1)
                {
                    // 2 5 8
                    CharsTowardsEast1000_1 c = (CharsTowardsEast1000_1)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast1000_1), c);
                }
                else
                {
                    // 3 6 9
                    CharsTowardsEast1000_2 c = (CharsTowardsEast1000_2)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast1000_2), c);
                }

                // No need to update the next origos since we are finished now
                return sMapTileName;
            }
            else
            {
                // Normal handling: Divide into four parts and add the digit of the part to the map tile name.
                // 2 4
                // 1 3

                iSizeEast /= 2;
                iSizeNorth /= 2;

                if (((iEast - iOrigoEast) / iSizeEast) == 0)
                {
                    if (((iNorth - iOrigoNorth) / iSizeNorth) == 0)
                    {
                        sMapTileName += "1";
                    }
                    else
                    {
                        sMapTileName += "2";
                        iNextOrigoNorth += iSizeNorth;
                    }
                }
                else
                {
                    iNextOrigoEast += iSizeEast;

                    if (((iNorth - iOrigoNorth) / iSizeNorth) == 0)
                    {
                        sMapTileName += "3";
                    }
                    else
                    {
                        sMapTileName += "4";
                        iNextOrigoNorth += iSizeNorth;
                    }
                }
            }

            // Check if we reached the wanted recursion level
            if (iWantedSizeKmNorth >= iSizeNorth)
            {
                // If wanted edge size is 12 km we probably want the names of the terrain database (maastotietokanta)
                // Add A or B to the name so that the area is 12x12 km2 instead of 24x12 km2 
                if (iWantedSizeKmNorth == 12000 && iSizeNorth == 12000 && iSizeEast == 24000)
                {
                    iSizeEast /= 2;

                    if (((iEast - iOrigoEast) / iSizeEast) == 0)
                    {
                        sMapTileName += "L";
                    }
                    else
                    {
                        sMapTileName += "R";
                    }
                }

                return sMapTileName;
            }

            return EncodeRecursive(iEast, iNorth, iWantedSizeKmNorth, iSizeEast, iSizeNorth,
                iNextOrigoEast, iNextOrigoNorth, sMapTileName);
        }
    }
}