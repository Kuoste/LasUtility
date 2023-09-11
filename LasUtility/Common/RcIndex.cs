namespace LasUtility.Common
{
    public class RcIndex
    {
        // Empty is Row and Column with int min value
        internal static readonly RcIndex Empty = new (int.MinValue, int.MinValue);
        public int Row { get; internal set; }
        public int Column { get; internal set; }

        public RcIndex(int iRow, int iColumn)
        {
            this.Row = iRow;
            this.Column = iColumn;
        }
    }
}