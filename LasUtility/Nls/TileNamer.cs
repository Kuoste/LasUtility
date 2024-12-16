using System;
using NetTopologySuite.Geometries;

namespace LasUtility.Nls
{
    public class TileNamer
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
        private const int _iStartMinEast = -76000;

        /// <summary>
        /// The min coordinate of the starting tile K2
        /// </summary>
        private const int _iStartMinNorth = 6570000;


        /// <summary>
        /// Smallest coordinate supported by the ETRS-TM35FIN system
        /// </summary>
        private const int _iMinAllowedEast = 20000;

        /// <summary>
        /// Smallest coordinate supported by the ETRS-TM35FIN system
        /// </summary>
        private const int _iMinAllowedNorth = _iStartMinNorth;

        /// <summary>
        /// Largest coordinate supported by the ETRS-TM35FIN system.
        /// </summary>
        private const int _iMaxAllowedEast = _iStartMinEast + 4 * _iStartSizeEast;

        /// <summary>
        /// Largest coordinate supported by the ETRS-TM35FIN system.
        /// </summary>
        private const int _iMaxAllowedNorth = _iStartMinNorth + 13 * _iStartSizeNorth;

        /// <summary>
        /// Largest supported tile edge length.
        /// </summary>
        private const int _iStartSizeEast = 192000;

        /// <summary>
        /// Largest supported tile edge length.
        /// </summary>
        private const int _iStartSizeNorth = 96000;

        /// <summary>
        /// Since NLS eastward indexing starts from 2 (K2, L2, M2, ...) use this offset in calculations.
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

            int iMinEast = _iStartMinEast + iIndexEast * _iStartSizeEast;
            int iMinNorth = _iStartMinNorth + iIndexNorth * _iStartSizeNorth;

            int iSizeEast = _iStartSizeEast;
            int iSizeNorth = _iStartSizeNorth;

            bool bIsOk = DecodeRecursive(sMapTileName, iStringIndex, ref iMinEast, ref iMinNorth, ref iSizeEast, ref iSizeNorth);

            if (bIsOk == true)
            {
                envelope = new(iMinEast, iMinEast + iSizeEast, iMinNorth, iMinNorth + iSizeNorth);
            }
            else
            {
                envelope = new(0, 0, 0, 0);
            }

            return bIsOk;

        }


        private static bool DecodeRecursive(string sMapTileName, int iStringIndex, ref int iMinEast, ref int iMinNorth, ref int iSizeEast, ref int iSizeNorth)
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
                    iMinEast += iSizeEast;
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
                        iMinNorth += iSizeNorth;
                        break;
                    case 'C':
                        iMinEast += iSizeEast;
                        break;
                    case 'D':
                        iMinEast += iSizeEast;
                        iMinNorth += iSizeNorth;
                        break;
                    case 'E':
                        iMinEast += 2 * iSizeEast;
                        break;
                    case 'F':
                        iMinEast += 2 * iSizeEast;
                        iMinNorth += iSizeNorth;
                        break;
                    case 'G':
                        iMinEast += 3 * iSizeEast;
                        break;
                    case 'H':
                        iMinEast += 3 * iSizeEast;
                        iMinNorth += iSizeNorth;
                        break;
                    default:
                        throw new Exception($"Tile name {sMapTileName}, character should be between A-G at location {iStringIndex}.");
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
                    throw new Exception($"Tile name {sMapTileName} should contain an underscore at location {iStringIndex}.");

                iStringIndex++;

                if (int.TryParse(sMapTileName[iStringIndex].ToString(), out int d) == false)
                    throw new Exception($"Tile name {sMapTileName} should contain a digit at location {iStringIndex}.");

                if (d < 1 || d > 9)
                    throw new Exception($"Tile name {sMapTileName} should contain a digit between 1-9 at location {iStringIndex}.");

                if (d < 4)
                {
                    if (d == 2)
                    {
                        iMinNorth += iSizeNorth;
                    }
                    else if (d == 3)
                    {
                        iMinNorth += 2 * iSizeNorth;
                    }
                }
                else if (d < 7)
                {
                    iMinEast += iSizeEast;

                    if (d == 5)
                    {
                        iMinNorth += iSizeNorth;
                    }
                    else if (d == 6)
                    {
                        iMinNorth += 2 * iSizeNorth;
                    }
                }
                else
                {
                    iMinEast += 2 * iSizeEast;

                    if (d == 8)
                    {
                        iMinNorth += iSizeNorth;
                    }
                    else if (d == 9)
                    {
                        iMinNorth += 2 * iSizeNorth;
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
                    throw new Exception($"Tile name {sMapTileName} should have a digit at location {iStringIndex}.");
                }

                if (d < 1 || d > 4)
                {
                    throw new Exception($"Tile name {sMapTileName} should have a digit value between 1-4 at location {iStringIndex}.");
                }

                if (d == 2)
                {
                    iMinNorth += iSizeNorth;
                }
                else if (d == 3)
                {
                    iMinEast += iSizeEast;
                }
                else if (d == 4)
                {
                    iMinEast += iSizeEast;
                    iMinNorth += iSizeNorth;
                }
            }

            return DecodeRecursive(sMapTileName, iStringIndex, ref iMinEast, ref iMinNorth, ref iSizeEast, ref iSizeNorth);
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

            if (iEast < _iMinAllowedEast || iEast >= _iMaxAllowedEast || iNorth < _iMinAllowedNorth || iNorth >= _iMaxAllowedNorth)
            {
                throw new Exception("Coordinates out of bounds");
            }

            // Determine the character that starts the map tile name
            int iIndexNorth = (iNorth - _iStartMinNorth) / _iStartSizeNorth;
            CharsTowardsNorth96000 c = (CharsTowardsNorth96000)iIndexNorth;
            sMapTileName += Enum.GetName(typeof(CharsTowardsNorth96000), c);

            // Determine digit that follows the char
            int iIndexEast = (iEast - _iStartMinEast) / _iStartSizeEast;
            sMapTileName += (iIndexEast + _iStartOffsetEast).ToString();

            // Now we have calculated the name of the highest level. Return if that is enough.
            if (iWantedSizeNorth >= _iStartSizeNorth)
            {
                return sMapTileName;
            }

            // Otherwise enter a recursive function that stops at wanted level
            return EncodeRecursive(iEast, iNorth, iWantedSizeNorth, _iStartSizeEast, _iStartSizeNorth,
                _iStartMinEast + iIndexEast * _iStartSizeEast, _iStartMinNorth + iIndexNorth * _iStartSizeNorth, sMapTileName);
        }

        private static string EncodeRecursive(int iEast, int iNorth, int iWantedSizeKmNorth, int iSizeEast, int iSizeNorth,
            int iMinEast, int iMinNorth, string sMapTileName)
        {

            int iNextMinEast = iMinEast;
            int iNextMinNorth = iMinNorth;

            if (iSizeNorth == 12000 && iSizeEast == 24000)
            {
                // Special handling: Create square 6x6 km2 tiles by dividing into 8 parts using letters A-H
                // B D F H
                // A C E G

                iSizeEast /= 4;
                iSizeNorth /= 2;

                if (((iNorth - iMinNorth) / iSizeNorth) == 0)
                {
                    // A C E G
                    int iIndexEast = (iEast - iMinEast) / iSizeEast;
                    CharsTowardsEast6000_0 c = (CharsTowardsEast6000_0)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast6000_0), c);

                    iNextMinEast += iIndexEast * iSizeEast;
                }
                else
                {
                    // B D F H

                    int iIndexEast = (iEast - iMinEast) / iSizeEast;
                    CharsTowardsEast6000_1 c = (CharsTowardsEast6000_1)iIndexEast;
                    sMapTileName += Enum.GetName(typeof(CharsTowardsEast6000_1), c);

                    iNextMinNorth += iSizeNorth;
                    iNextMinEast += iIndexEast * iSizeEast;
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

                int iIndexNorth = (iNorth - iMinNorth) / iSizeNorth;
                int iIndexEast = (iEast - iMinEast) / iSizeEast;

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

                return sMapTileName;
            }
            else
            {
                // Normal handling: Divide into four parts and add the digit of the part to the map tile name.
                // 2 4
                // 1 3

                iSizeEast /= 2;
                iSizeNorth /= 2;

                if (((iEast - iMinEast) / iSizeEast) == 0)
                {
                    if (((iNorth - iMinNorth) / iSizeNorth) == 0)
                    {
                        sMapTileName += "1";
                    }
                    else
                    {
                        sMapTileName += "2";
                        iNextMinNorth += iSizeNorth;
                    }
                }
                else
                {
                    iNextMinEast += iSizeEast;

                    if (((iNorth - iMinNorth) / iSizeNorth) == 0)
                    {
                        sMapTileName += "3";
                    }
                    else
                    {
                        sMapTileName += "4";
                        iNextMinNorth += iSizeNorth;
                    }
                }
            }

            // Check if we reached the wanted recursion level
            if (iWantedSizeKmNorth >= iSizeNorth)
            {
                // If wanted edge size is 12 km we probably want the names of the topographic database (maastotietokanta)
                // Add A or B to the name so that the area is 12x12 km2 instead of 24x12 km2 
                if (iWantedSizeKmNorth == 12000 && iSizeNorth == 12000 && iSizeEast == 24000)
                {
                    iSizeEast /= 2;

                    int iIndexEast = (iEast - iMinEast) / iSizeEast;

                    if (iIndexEast == 0 || iIndexEast == 2)
                    {
                        sMapTileName += "L";
                    }
                    else
                    {
                        // 1 or 3
                        sMapTileName += "R";
                    }
                }

                return sMapTileName;
            }

            return EncodeRecursive(iEast, iNorth, iWantedSizeKmNorth, iSizeEast, iSizeNorth,
                iNextMinEast, iNextMinNorth, sMapTileName);
        }
    }
}