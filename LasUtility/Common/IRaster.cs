
using NetTopologySuite.Geometries;

namespace LasUtility.Common
{
    public interface IRaster
    {
        double GetValue(Coordinate c);

        double GetValue(int iRow, int jCol);

    }
}
