using System;
using System.Collections.Generic;

namespace LasUtility.LAS
{
    public interface ILasFileReader : IDisposable
    {
        double MinX { get; }
        double MinY { get; }
        double MaxY { get; }
        double MaxX { get; }

        void ReadHeader(string fullFilePath);
        IEnumerable<LasPoint> Points();
        int OpenReader(string fullFilePath);
        void CloseReader();
    }
}
