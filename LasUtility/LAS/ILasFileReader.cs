
namespace LasUtility.LAS
{
    public interface ILasFileReader
    {
        double MinX { get; }
        double MinY { get; }
        double MaxY { get; }
        double MaxX { get; }

        void ReadHeader(string fullFilePath);
        LasPoint ReadPoint();
        int OpenReader(string fullFilePath);
        void CloseReader();
    }
}
