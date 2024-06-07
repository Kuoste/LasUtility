using System;
using System.Collections.Generic;
using System.IO;

namespace LasUtility.LAS
{
    public class LasFileReader : ILasFileReader
    {
        private double _minX = double.NaN;
        private double _maxX = double.NaN;
        private double _minY = double.NaN;
        private double _maxY = double.NaN;

        public double MinX
        {
            get { return _minX; }
            private set { _minX = value; }
        }

        public double MinY
        {
            get { return _minY; }
            private set { _minY = value; }
        }

        public double MaxX
        {
            get { return _maxX; }
            private set { _maxX = value; }
        }

        public double MaxY
        {
            get { return _maxY; }
            private set { _maxY = value; }
        }

        public void ReadHeader(string fullFilePath)
        {
            if (!File.Exists(fullFilePath))
                throw new FileNotFoundException();

            using BinaryReader reader = new (File.OpenRead(fullFilePath));

            byte[] value = new byte[200];
            string signature = new (reader.ReadChars(4));

            if (!signature.Equals("LASF"))
                throw new InvalidDataException("File not recognized as a LAS file");

            value = reader.ReadBytes(20);

            int versionMajor = reader.ReadByte();
            int versionMinor = reader.ReadByte();

            if (versionMajor != 1 || versionMinor < 0 || versionMinor > 4)
            {
                throw new InvalidDataException(String.Format("File is LAS {0}.{1} Format. Only LAS 1.x is supported.",
                    versionMajor, versionMinor));
            }

            value = reader.ReadBytes(153);

            MaxX = reader.ReadDouble();
            MinX = reader.ReadDouble();
            MaxY = reader.ReadDouble();
            MinY = reader.ReadDouble();
        }

        IEnumerable<LasPoint> ILasFileReader.Points()
        {
            throw new NotImplementedException();
        }

        public int OpenReader(string fullFilePath)
        {
            throw new NotImplementedException();
        }

        public void CloseReader()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }
    }
}
