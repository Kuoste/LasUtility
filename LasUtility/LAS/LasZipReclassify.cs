using Kuoste.LasZipNetStandard;
using LasUtility.Common;
using NetTopologySuite.Geometries;
using System;

namespace LasUtility.LAS
{
    public class LasZipReclassify
    {
        private readonly string _sLasFullFilename;
        private readonly string _sOutputLasFileName;
        private readonly IRaster _classGrid;

        const int _iLasGroundClass = 2;

        public LasZipReclassify(string lasFullFilename, string outputLasFileName, IRaster classGrid)
        {
            _sLasFullFilename = lasFullFilename;
            _sOutputLasFileName = outputLasFileName;
            _classGrid = classGrid;
        }

        public long Run()
        {
            LasZip lasZip = new(out string version);
            Console.WriteLine("Using laszip dll " + version);

            lasZip.OpenReader(_sLasFullFilename);

            LaszipHeaderStruct h = lasZip.GetReaderHeader();
            lasZip.SetWriterHeader(h);

            lasZip.OpenWriter(_sOutputLasFileName, true);

            Kuoste.LasZipNetStandard.LasPoint p = new();
            long nReclassified = 0;
            ulong ulPointCount = Math.Max(h.NumberOfPointRecords, h.ExtendedNumberOfPointRecords);


            for (ulong i = 0; i < ulPointCount; i++)
            {
                lasZip.ReadPoint(ref p);

                double classValue = _classGrid.GetValue(new Coordinate(p.X, p.Y));

                if (!double.IsNaN(classValue))
                {
                    if (classValue >= 70 && classValue < 100)
                    {
                        if (p.Classification == _iLasGroundClass)
                        {
                            p.Classification = (byte)classValue;
                        }
                    }
                    else
                    {
                        if (p.ReturnNumber == p.NumberOfReturns)
                            p.Classification = (byte)classValue;
                    }
                }

                lasZip.WritePoint(ref p);
            }

            lasZip.CloseReader();
            lasZip.CloseWriter();

            lasZip.DestroyReader();
            lasZip.DestroyWriter();

            return nReclassified;
        }
    }
}
