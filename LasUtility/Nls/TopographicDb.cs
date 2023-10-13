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
                {36311, 50}, //Virtavesi, alle 2m
                {36312, 51}  //Virtavesi, 2-5m
        };

        public static readonly Dictionary<int, byte> RoadLineClassesToRasterValues = new()
        {
                {12111, 70}, //Autotie Ia // mc id 44
                {12112, 72}, //Autotie Ib
                {12121, 74}, //Autotie IIa
                {12122, 76}, //Autotie IIb
                {12131, 78}, //Autotie IIIa
                {12132, 80}, //Autotie IIIb
                {12141, 82}, //Ajotie
                //{12151, 99}, //Lautta
                //{12152, 99}, //Lossi
                //{12153, 99}, //Huoltoaukko ilman puomia
                //{12154, 99}, //Huoltoaukko puomilla
                //{12312, 99}, //Talvitie
                {12313, 88}, //Polku
                {12314, 86}, //Kävely- ja pyörätie
                {12316, 84}  //Ajopolku
        };

        public static readonly Dictionary<int, byte> BuildingPolygonClassesToRasterValues = new()
        {
                {42210, 100},  //	Asuinrakennus, ? krs
                {42211, 101},  //	Asuinrakennus, 1-2 krs
                {42212, 102},  //	Asuinrakennus, 3-n krs
                {42220, 103},  //	Liike- tai julkinen rakennus, ? krs
                {42221, 104},  //	Liike- tai julkinen rakennus, 1-2 krs
                {42222, 105},  //	Liike- tai julkinen rakennus, 3-n krs
                {42230, 106},  //	Lomarakennus, ? krs
                {42231, 107},  //	Lomarakennus, 1-2 krs
                {42232, 108},  //	Lomarakennus, 3-n krs
                {42240, 109},  //	Teollinen rakennus, ? krs
                {42241, 110},  //	Teollinen rakennus, 1-2 krs
                {42242, 111},  //	Teollinen rakennus, 3-n krs
                {42270, 112},  //	Kirkko
                {42250, 113},  //	Kirkollinen rakennus, ? krs
                {42251, 114},  //	Kirkollinen rakennus, 1-2 krs
                {42252, 115},  //	Kirkollinen rakennus, 3-n krs
                {42260, 116},  //	Muu rakennus, ? krs
                {42261, 117},  //	Muu rakennus, 1-2 krs
                {42262, 118},  //	Muu rakennus, 3-n krs
        };

        public static readonly Dictionary<int, byte> WaterPolygonClassesToRasterValues = new()
        {
            {36200, 130},  //	Järvivesi
            {36211, 131},  //	Merivesi
            {35411, 135},  //	Suo, helppokulkuinen puuton 
            {35412, 136},  //	Suo, helppokulkuinen metsää kasvava 
            {35421, 137},  //	Suo, vaikeakulkuinen puuton 
            {35422, 138},  //	Suo, vaikeakulkuinen metsää kasvava
        };

    }
}
