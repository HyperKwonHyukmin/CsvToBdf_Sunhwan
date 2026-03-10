using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvToBdf.AMData;

namespace CsvToBdf.FEData
{
    public class Prop : IComparable<Prop>
    {
        public int PropID { get; set; }
        public int MatID { get; set; }
        public string Str { get; set; }
        public string Type { get; set; }
        public string Dim1 { get; set; }
        public string Dim2 { get; set; }
        public string Dim3 { get; set; }
        public string Dim4 { get; set; }
        public string Division { get; set; }
        public Prop()
        {
        }
        public Prop(int num, double outDia, double thick)
        {
            PropID = num;
            MatID = 1;
            Str = $"TUBE_{outDia}x{thick}";
            Dim1 = (outDia / 2).ToString("F1");
            Dim2 = ((outDia - thick * 2) / 2).ToString("F1");
            Dim3 = string.Empty;
            Dim4 = string.Empty;
            Type = "TUBE";
            Division = "PIPE";
        }
        public Prop(int num, string size, string division)
        {
            PropID = num;
            MatID = 1;
            Str = size;
            Division = division;
            string struType = size.Split('_')[0];
            string[] dims = size.Split('_')[1].Split('x');
            if (struType == "ANG")
            {
                Dim1 = double.Parse(dims[0]).ToString("F1");
                Dim2 = double.Parse(dims[1]).ToString("F1");
                Dim3 = double.Parse(dims[2]).ToString("F1");
                Dim4 = double.Parse(dims[2]).ToString("F1");
                Type = "L";
            }
            else if (struType == "JISI" || struType == "BEAM")
            {
                double tempD1 = double.Parse(dims[0]) - double.Parse(dims[3]) * 2;
                double tempD2 = double.Parse(dims[3]) * 2;
                Dim1 = tempD1.ToString("F1");
                Dim2 = tempD2.ToString("F1");
                Dim3 = double.Parse(dims[1]).ToString("F1");
                Dim4 = double.Parse(dims[2]).ToString("F1");
                Type = "H";
            }
            else if (struType == "TUBE")
            {
                double tempD1 = double.Parse(dims[0]) / 2;
                double tempD2 = (double.Parse(dims[0]) - double.Parse(dims[1]) * 2) / 2;
                Dim1 = tempD1.ToString("F1");
                Dim2 = tempD2.ToString("F1");
                Dim3 = string.Empty;
                Dim4 = string.Empty;
                Type = "TUBE";
            }
            else if (struType == "FBAR" || struType == "BULB")
            {
                Dim1 = double.Parse(dims[0]).ToString("F1");
                Dim2 = double.Parse(dims[1]).ToString("F1");
                Dim3 = string.Empty;
                Dim4 = string.Empty;
                Type = "BAR";
            }
            else if (struType == "RBAR")
            {
                double tempD1 = double.Parse(dims[0]) / 2;
                Dim1 = tempD1.ToString("F1");
                Dim2 = string.Empty;
                Dim3 = string.Empty;
                Dim4 = string.Empty;
                Type = "ROD";
            }
            else if (struType == "BSC")
            {
                Dim1 = double.Parse(dims[1]).ToString("F1");
                Dim2 = double.Parse(dims[0]).ToString("F1");
                Dim3 = double.Parse(dims[2]).ToString("F1");
                Dim4 = double.Parse(dims[3]).ToString("F1");
                Type = "CHAN";
            }
            else if (struType == "BOX")
            {
                Dim1 = double.Parse(dims[0]).ToString("F1");
                Dim2 = double.Parse(dims[1]).ToString("F1");
                Dim3 = double.Parse(dims[2]).ToString("F1");
                Dim4 = double.Parse(dims[3]).ToString("F1");
                Type = "BOX";
            }

        }
        // for Pipe with fluid
        public Prop(int num, string size, int matId)
        {
            PropID = num;
            MatID = matId;
            Str = size;
            Division = "PIPE";
            string struType = size.Split('_')[0];
            string[] dims = size.Split('_')[1].Split('x');
            if (struType == "TUBE")
            {
                double tempD1 = double.Parse(dims[0]) / 2;
                double tempD2 = (double.Parse(dims[0]) - double.Parse(dims[1]) * 2) / 2;
                Dim1 = tempD1.ToString("F1");
                Dim2 = tempD2.ToString("F1");
                Dim3 = string.Empty;
                Dim4 = string.Empty;
                Type = "TUBE";
            }
        }

        public int CompareTo(Prop other)
        {
            if (this.PropID < other.PropID)
            {
                return -1;
            }
            else if (this.PropID > other.PropID)
            {
                return 1;
            }
            return 0;
        }

    }
}
