using LasUtility.Common;
using laszip.net;

namespace LasUtility.LAS
{
    public class LasZipReclassify
    {
        private readonly string _sLasFullFilename;
        private readonly string _sOutputLasFileName;
        private readonly IHeightMap _classGrid;

        const int _iLasGroundClass = 2;

        public LasZipReclassify(string lasFullFilename, string outputLasFileName, IHeightMap classGrid)
        {
            _sLasFullFilename = lasFullFilename;
            _sOutputLasFileName = outputLasFileName;
            _classGrid = classGrid;
        }

        public long Run()
        {
            LasZipFileReader reader = new ();
            LasZipFileWriter writer = new ();

            reader.OpenReader(_sLasFullFilename);
            writer.SetHeader(reader.GetHeader());
            writer.OpenWriter(_sOutputLasFileName, true);

            laszip_point p;
            double[] scaledCoords = new double[3];
            long nReclassified = 0;
            while ((p = reader.ReadPointAsLasZipPoint(ref scaledCoords)) != null)
            {
                double classValue = _classGrid.GetHeight(scaledCoords[0], scaledCoords[1]);

                if (!double.IsNaN(classValue))
                {
                    if (classValue >= 70 && classValue < 100)
                    {
                        if (p.classification == _iLasGroundClass)
                        {
                            p.classification = (byte)classValue;
                        }
                    }
                    else
                    {
                        if (p.return_number == p.number_of_returns_of_given_pulse)
                            p.classification = (byte)classValue;
                    }
                }

                writer.WritePoint(p);
            }

            reader.CloseReader();
            writer.CloseWriter();

            return nReclassified;
        }
    }
}
