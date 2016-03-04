using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasUtility.DEM
{
    public interface IHeightMap
    {
        double GetHeight(double x, double y);
    }
}
