using System;
using laszip.net;

namespace LasUtility.LAS
{
    public class LasZipFileReader : ILasFileReader
    {
        private laszip_dll _lasZip;
        private bool _isCompressed;

        //double _readingLimitMinX = Double.NaN;
        //double _readingLimitMinY = Double.NaN;
        //double _readingLimitMaxY = Double.NaN;
        //double _readingLimitMaxX = Double.NaN;

        //public void SetReadingLimits(double minY, double minX, double maxY, double maxX)
        //{
        //    _readingLimitMinX = minX;
        //    _readingLimitMinY = minY;
        //    _readingLimitMaxY = maxY;
        //    _readingLimitMaxX = maxX;
        //} 

        public LasZipFileReader()
        {
            _lasZip = laszip_dll.laszip_create();
        }

        public int OpenReader(string fullFilePath)
        {
            return _lasZip.laszip_open_reader(fullFilePath, ref _isCompressed);
        }

        public void CloseReader()
        {
            _lasZip.laszip_close_reader();
        }

        public void ReadHeader(string fullFilePath)
        {
            int ret = OpenReader(fullFilePath);

            if (ret != 0)
                throw new Exception(_lasZip.laszip_get_error());

            CloseReader();
        }

        public LasPoint ReadPoint()
        {
            LasPoint p = new LasPoint();

            if (GetNextPoint() == false)
                return null;

            double[] coordinates = new double[3];
            _lasZip.laszip_get_coordinates(coordinates);

            p.x = coordinates[0];
            p.y = coordinates[1];
            p.z = coordinates[2];
            p.classification = _lasZip.point.classification;

            return p;
        }

        internal laszip_point ReadPointAsLasZipPoint(ref double[] coordinates)
        {
            if (GetNextPoint() == false)
                return null;

            coordinates = new double[3];
            _lasZip.laszip_get_coordinates(coordinates);

            return _lasZip.point;
        }

        private bool GetNextPoint()
        {
            long nPointsRead, nPointsInFile;
            _lasZip.laszip_get_number_of_point(out nPointsInFile);
            _lasZip.laszip_get_point_count(out nPointsRead);

            //do
            //{
            if (nPointsRead >= nPointsInFile)
                    return false;

            if (_lasZip.laszip_read_point() != 0)
                throw new Exception(_lasZip.laszip_get_error());

            //    nPointsRead++;
            //}
            //while (!IsPointInBounds());

            return true;
        }

        //private bool IsPointInBounds()
        //{
        //    if (_readingLimitMinX == Double.NaN)
        //        return true;

        //    if (_lasZip.point.Y > _readingLimitMaxY ||
        //        _lasZip.point.Y < _readingLimitMinY ||
        //        _lasZip.point.X > _readingLimitMaxX ||
        //        _lasZip.point.X < _readingLimitMinX)
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        public double MinX
        {
            get { return _lasZip.header.min_x; }
        }

        public double MinY
        {
            get { return _lasZip.header.min_y; }
        }

        public double MaxX
        {
            get { return _lasZip.header.max_x; }
        }

        public double MaxY
        {
            get { return _lasZip.header.max_y; }
        }
    }
}
