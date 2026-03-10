using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvToBdf.FEData
{
    public class Mat
    {
        public string Str { get; set; }
        public double Density { get; set; }
        public int Id { get; set; }
        public Mat(int num, string str)
        {
            Id = num;
            Str = str;
            string struType = str.Split('_')[0];
            string[] dims = str.Split('_')[1].Split('x');
            double outerRadius = double.Parse(dims[0]) / 2;
            double innerRadius = (double.Parse(dims[0]) - double.Parse(dims[1]) * 2) / 2;
            Density = GetEquivalentDensity(outerRadius, innerRadius);
        }

        private double GetEquivalentDensity(double outerRadius, double innerRadius)
        {
            double fluidArea = innerRadius * innerRadius * Math.PI;
            double steelArea = outerRadius * outerRadius * Math.PI - fluidArea;
            double density;
            double steelDensity = 7850;
            double fluidDensity = 1000;
            density = (steelArea * steelDensity + fluidArea * fluidDensity) / steelArea; 
            return density;
        }
    }
}
