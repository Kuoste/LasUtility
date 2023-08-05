using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LasUtility.Common;
using laszip.net;

namespace LasUtility.LAS
{
    public class LasZipReclassify
    {
        private string lasFullFilename;
        private string outputLasFileName;
        private IHeightMap classGrid;

        const int _lasGroundClass = 2;

        public LasZipReclassify(string lasFullFilename, string outputLasFileName, IHeightMap classGrid)
        {
            this.lasFullFilename = lasFullFilename;
            this.outputLasFileName = outputLasFileName;
            this.classGrid = classGrid;
        }

        public long Run()
        {
            LasZipFileReader reader = new LasZipFileReader();
            LasZipFileWriter writer = new LasZipFileWriter();

            reader.OpenReader(lasFullFilename);
            writer.SetHeader(reader.GetHeader());
            writer.OpenWriter(outputLasFileName, true);

            laszip_point p;
            double[] scaledCoords = new double[3];
            long nReclassified = 0;
            while ((p = reader.ReadPointAsLasZipPoint(ref scaledCoords)) != null)
            {
                double classValue = classGrid.GetHeight(scaledCoords[0], scaledCoords[1]);

                if (!double.IsNaN(classValue))
                {
                    if (classValue >= 70 && classValue < 100)
                    {
                        if (p.classification == _lasGroundClass)
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
