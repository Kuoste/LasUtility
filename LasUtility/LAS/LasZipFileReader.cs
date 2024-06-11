//using System;
//using System.Collections.Generic;
//using laszip.net;

//namespace LasUtility.LAS
//{
//    public class LasZipFileReader : ILasFileReader
//    {
//        private readonly laszip_dll _lasZip;
//        private bool _isCompressed;

//        public LasZipFileReader()
//        {
//            _lasZip = laszip_dll.laszip_create();
//        }

//        public int OpenReader(string fullFilePath)
//        {
//            return _lasZip.laszip_open_reader(fullFilePath, ref _isCompressed);
//        }

//        public void CloseReader()
//        {
//            _lasZip.laszip_close_reader();
//        }

//        public void ReadHeader(string fullFilePath)
//        {
//            int ret = OpenReader(fullFilePath);

//            if (ret != 0)
//                throw new Exception(_lasZip.laszip_get_error());

//            CloseReader();
//        }

//        internal laszip_header GetHeader()
//        {
//            return _lasZip.header;
//        }

//        public IEnumerable<LasPoint> Points()
//        {
//            while (GetNextPoint() == true)
//            {
//                double[] coordinates = new double[3];
//                _lasZip.laszip_get_coordinates(coordinates);

//                yield return new LasPoint
//                {
//                    x = coordinates[0],
//                    y = coordinates[1],
//                    z = coordinates[2],
//                    classification = _lasZip.point.classification
//                };
//            }
//        }

//        internal laszip_point ReadPointAsLasZipPoint(ref double[] coordinates)
//        {
//            if (GetNextPoint() == false)
//                return null;

//            _lasZip.laszip_get_coordinates(coordinates);

//            return _lasZip.point;
//        }

//        private bool GetNextPoint()
//        {
//            _lasZip.laszip_get_number_of_point(out long nPointsInFile);
//            _lasZip.laszip_get_point_count(out long nPointsRead);

//            if (nPointsRead >= nPointsInFile)
//                    return false;

//            if (_lasZip.laszip_read_point() != 0)
//                throw new Exception(_lasZip.laszip_get_error());

//            return true;
//        }

//        public void Dispose()
//        {

//        }

//        public double MinX
//        {
//            get { return _lasZip.header.min_x; }
//        }

//        public double MinY
//        {
//            get { return _lasZip.header.min_y; }
//        }

//        public double MaxX
//        {
//            get { return _lasZip.header.max_x; }
//        }

//        public double MaxY
//        {
//            get { return _lasZip.header.max_y; }
//        }
//    }
//}
