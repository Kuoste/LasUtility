using DotSpatial.Topology;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LasReader.DEM
{

    class TriangleIndexGrid
    {
        List<int>[][] _grid;

        int _nRows, _nCols;

        public TriangleIndexGrid(int nRows, int nCols)
        {
            _grid = new List<int>[nRows][];
            _nRows = nRows;
            _nCols = nCols;

            for (int i = 0; i < nRows; i++)
                _grid[i] = new List<int>[nCols];
        }


        public List<int> GetIndexes(int iRow, int jCol)
        {
            if (_grid[iRow][jCol] == null)
                return new List<int>();

            return _grid[iRow][jCol];
        }


        public void AddIndex(IVertex[] vertices, int index)
        {
            throw new NotImplementedException();

            //int iRowMin = (int)Math.Floor(p.Envelope.Minimum.Y);
            //int iColMin = (int)Math.Floor(p.Envelope.Minimum.X);
            //int iRowMax = (int)Math.Ceiling(p.Envelope.Maximum.Y);
            //int iColMax = (int)Math.Ceiling(p.Envelope.Maximum.X);

            //if (iRowMin >= 0 && iRowMax < _nRows && iColMin >= 0 && iColMax < _nCols)
            //{
            //    for (int iRow = iRowMin; iRow <= iRowMax; iRow++)
            //    {
            //        for (int jCol = iColMin; jCol <= iColMax; jCol++)
            //        {
            //            if (_grid[iRow][jCol] == null)
            //                _grid[iRow][jCol] = new List<int>();

            //            if (!_grid[iRow][jCol].Contains(index))
            //                _grid[iRow][jCol].Add(index);
            //        }
            //    }
            //}
            //else
            //{
            //    throw new IndexOutOfRangeException("Polygon envelope out of bounds");
            //}

        }


    }
}
