using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvToBdf.FEData
{
    public class Rbe : IComparable<Rbe>
    {
        public int ElemID { get; set; }
        public Node Pos { get; set; }
        public List<Node> DepNodes { get; set; }
        public string Rest { get; set; }
        public string AmRef { get; set; }
        public string AMType { get; set; }
        public Rbe()
        {
            AMType = string.Empty;
            DepNodes = new List<Node>();
        }

        public int CompareTo(Rbe other)
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
