using System;
using System.Collections.Generic;
using System.Text;
using Kuoste.LasZipNetStandard;
using NetTopologySuite.Noding;

namespace LasUtility.LAS
{
    public class LasZipNetReader : ILasFileReader, IDisposable
    {
        private LasZip _lasZip;

        private LaszipHeaderStruct _header;

        private bool _isOpened = false;
        private bool _isHeaderRead = false;

        public double MinX { get { return _isOpened ? _header.MinX : double.NaN; } }

        public double MinY { get { return _isOpened ? _header.MinY : double.NaN; } }

        public double MaxY { get { return _isOpened ? _header.MaxY : double.NaN; } }

        public double MaxX { get { return _isOpened ? _header.MaxX : double.NaN; } }

        public LasZipNetReader()
        {
            _lasZip = new LasZip(out _);
        }

        public void CloseReader()
        {
            _lasZip.CloseReader();
            _isOpened = false;
            _isHeaderRead = false;
        }

        public void Dispose()
        {
            _lasZip.DestroyReader();
        }

        public int OpenReader(string fullFilePath)
        {
            if (_lasZip.OpenReader(fullFilePath))
            {
                _isOpened = true;
                return 0;
            }

            return 1;
        }

        public void ReadHeader(string fullFilePath)
        {
            if (_isOpened == false)
                OpenReader(fullFilePath);

            _header = _lasZip.GetReaderHeader();
            _isHeaderRead = true;
        }

        IEnumerable<LasPoint> ILasFileReader.Points()
        {
            if (_isOpened == false )
                throw new Exception("Cannot read point since las reader is not opened");

            if (_isHeaderRead == false)
                throw new Exception("Cannot read points since las header is not yet read");

            Kuoste.LasZipNetStandard.LasPoint point = new();

            ulong uPointCount = Math.Max(_header.NumberOfPointRecords, _header.ExtendedNumberOfPointRecords);
            ulong uIndex = 0;

            while (uIndex < uPointCount)
            {
                _lasZip.ReadPoint(ref point);
                uIndex++;

                yield return new LasPoint
                {
                    x = point.X,
                    y = point.Y,
                    z = point.Z,
                    classification = point.Classification
                };
            }
        }

        IEnumerable<Kuoste.LasZipNetStandard.LasPoint> Points()
        {
            if (_isOpened == false)
                throw new Exception("Cannot read point since las reader is not opened");

            if (_isHeaderRead == false)
                throw new Exception("Cannot read points since las header is not yet read");

            Kuoste.LasZipNetStandard.LasPoint point = new();

            ulong uPointCount = Math.Max(_header.NumberOfPointRecords, _header.ExtendedNumberOfPointRecords);
            ulong uIndex = 0;

            while (uIndex < uPointCount)
            {
                _lasZip.ReadPoint(ref point);
                uIndex++;

                yield return point;
            }
        }
    }
}
