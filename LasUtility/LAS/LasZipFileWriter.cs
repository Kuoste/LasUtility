using System;
using laszip.net;

namespace LasUtility.LAS
{
    public class LasZipFileWriter
    {
        private laszip_dll _lasZip;

        public LasZipFileWriter()
        {
            _lasZip = laszip_dll.laszip_create();
        }

        public int OpenWriter(string fullFilePath, bool isCompressed)
        {
            return _lasZip.laszip_open_writer(fullFilePath, isCompressed);
        }

        public void CloseWriter()
        {
            _lasZip.laszip_close_writer();
        }

        public void SetHeader(laszip_header header)
        {
            int ret = _lasZip.laszip_set_header(header);

            if (ret != 0)
                throw new Exception(_lasZip.laszip_get_error());
        }

        public void WritePoint(laszip_point point)
        {
            int ret = _lasZip.laszip_set_point(point);
            if (ret != 0)
                throw new Exception(_lasZip.laszip_get_error());

            ret = _lasZip.laszip_write_point();
            if (ret != 0)
                throw new Exception(_lasZip.laszip_get_error());
        }
    }
}
