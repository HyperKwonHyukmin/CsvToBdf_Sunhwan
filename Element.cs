using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvToBdf.AMData;

namespace CsvToBdf.FEData
{
    public class Element : IComparable<Element>
    {
        public int ElemID { get; set; }
        public Node Poss { get; set; }
        public Node Pose { get; set; }
        public int PropID { get; set; }
        public string Cood { get; set; }
        public string AmRef { get; set; }
        public Element()
        {
        }

        public Element(Element elem)
        {
            PropID = elem.PropID;
            Cood = elem.Cood;
            AmRef = elem.AmRef;
        }
        public int CompareTo(Element other)
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
