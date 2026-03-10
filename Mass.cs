using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvToBdf.FEData
{
    public class Mass : IComparable<Mass>
    {
        public int ElemID { get; set; }
        public Node Pos { get; set; }
        public double massVal { get; set; }
        public string AmRef { get; set; }
        public Mass()
        {
        }

        public int CompareTo(Mass other)
        {
            if (this.ElemID < other.ElemID)
            {
                return -1;
            }
            else if (this.ElemID > other.ElemID)
            {
                return 1;
            }
            return 0;
        }

    }
}
