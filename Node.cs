using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace CsvToBdf.FEData
{
    public class Node : IComparable<Node>
    {
        public int nodeID { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Node(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Node(Point3D point)
        {
            X = Math.Round(point.X);
            Y = Math.Round(point.Y);
            Z = Math.Round(point.Z);
        }
        public int CompareTo(Node other)
        {
            if (this.nodeID < other.nodeID)
            {
                return -1;
            }
            else if (this.nodeID > other.nodeID)
            {
                return 1;
            }
            return 0;
        }

    }
}
