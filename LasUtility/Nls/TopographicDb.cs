using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.NetworkInformation;

namespace LasUtility.Nls
{
    public static class TopographicDb
    {
        public const int iMapTileEdgeLengthInMeters = 12000;

        public const string sPrefixForTerrainType = "m_";
        public const string sPrefixForBuildings = "r_";
        public const string sPrefixForRoads = "l_";

        public const string sPostfixForPolygon = "_p";
        public const string sPostfixForLine = "_v";

        public static readonly Dictionary<int, byte> WaterLineClassesToRasterValues = new()
        {
                {36311, 50}, // Virtavesi, Watercourse, width under 2m
                {36312, 51}  // Virtavesi, Watercourse 2-5m
        };

        public static readonly Dictionary<int, byte> RoadLineClassesToRasterValues = new()
        {
                {12111, 70}, // Autotie, Road Ia // mc id 44
                {12112, 72}, // Autotie, Road Ib
                {12121, 74}, // Autotie, Road IIa
                {12122, 76}, // Autotie, Road IIb
                {12131, 78}, // Autotie, Road IIIa
                {12132, 80}, // Autotie, Road IIIb
                {12141, 82}, // Ajotie, Roadway
                //{12151, 99}, // Lautta, Ferry
                //{12152, 99}, // Lossi, Small ferry
                //{12312, 99}, // Talvitie, Winter road
                {12313, 88}, // Polku, Path
                {12314, 86}, // Kävely- ja pyörätie, Pedestrian and bicycle route
                {12316, 84}  // Ajopolku, Track
        };

        public static readonly Dictionary<int, byte> BuildingPolygonClassesToRasterValues = new()
        {
                {42210, 100},  //	Asuinrakennus, Residential building, number of floors unspecified
                {42211, 101},  //	Asuinrakennus, Residental building, 1-2 floors
                {42212, 102},  //	Asuinrakennus, Residential building, 3-n floors
                {42220, 103},  //	Liike- tai julkinen rakennus, Office or public building, number of floors unspecified
                {42221, 104},  //	Liike- tai julkinen rakennus, Office or public building, 1-2 floors
                {42222, 105},  //	Liike- tai julkinen rakennus, Office or public building, 3-n floors
                {42230, 106},  //	Lomarakennus, Holiday building, number of floors unspecified
                {42231, 107},  //	Lomarakennus, Holiday building, 1-2 floors
                {42232, 108},  //	Lomarakennus, Holiday building, 3-n floors
                {42240, 109},  //	Teollinen rakennus, Industrial building, number of floors unspecified
                {42241, 110},  //	Teollinen rakennus, Industrial building, 1-2 floors
                {42242, 111},  //	Teollinen rakennus, Industrial building, 3-n floors
                {42270, 112},  //	Kirkko, Church
                {42250, 113},  //	Kirkollinen rakennus, Religious building, number of floors unspecified
                {42251, 114},  //	Kirkollinen rakennus, Religious building, 1-2 floors
                {42252, 115},  //	Kirkollinen rakennus, Religious building, 3-n floors
                {42260, 116},  //	Muu rakennus, Other building, number of floors unspecified
                {42261, 117},  //	Muu rakennus, Other building, 1-2 floors
                {42262, 118},  //	Muu rakennus, Other building, 3-n floors
        };

        public static readonly Dictionary<int, byte> WaterPolygonClassesToRasterValues = new()
        {
            {36200, 130},  //	Järvivesi, Lake water
            {36211, 131},  //	Merivesi, Sea water
        };

        public static readonly Dictionary<int, byte> SwampPolygonClassesToRasterValues = new()
        {
            {35411, 135},  //	Suo, helppokulkuinen puuton, Open bog, easy to traverse treeless
            {35412, 136},  //	Suo, helppokulkuinen metsää kasvava, Bog, easy to traverse forested
            {35421, 137},  //	Suo, vaikeakulkuinen puuton, Open fen, difficult to traverse treeless
            {35422, 138},  //	Suo, vaikeakulkuinen metsää kasvava, Fen, difficult to traverse forested
        };

        public static readonly Dictionary<int, byte> FieldPolygonClassesToRasterValues = new ()
        {
            {32611, 140},  //	Pelto, Field
            {32612, 141},  //   Puutarha, Garden
            {32800, 142},  //   Niitty, Meadow
        };

        public static readonly Dictionary<int, byte> RockPolygonClassesToRasterValues = new()
        {
            {34700, 145},  //	Kivikko, Rocky area
            {34100, 146},  //   Kallio - alue, Rock - area
            {32500, 147}, //   Louhos, Quarry
            {32111, 148},  //   Maa-aineksenottoalue, karkea kivennäisaines, Mineral resources extraction area, coarse-grained material
        };

        public static readonly Dictionary<int, byte> RockLineClassesToRasterValues = new()
        {
            {34400, 150},  //	Jyrkänne, Escarpment
            {34500, 151},  //   Kalliohalkeama, Fissure
            {34800, 152},  //   Luiska, Slope
        };

        public static readonly Dictionary<int, byte> SandPolygonClassesToRasterValues = new()
        {
            {34300, 160},  //	Hietikko, Sand
            {32112, 161},  //   Maa-aineksenottoalue, hieno kivennäisaines, Mineral resources extraction area, fine-grained material
        };
    }
}
