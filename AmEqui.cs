using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Text.RegularExpressions;

namespace CsvToBdf.AMData
{
    public class AMEqui
    {
        private string _name;
        private Point3D _pos;
        private Point3D _cog;
        private double _mass;
        private List<Point3D> _depPos;

        public AMEqui()
        {

        }
        public AMEqui(string row)
        {
            _depPos = new List<Point3D>();
            string[] columns = row.Split(',');
            _name = columns[0];
            _pos = GetPoint3D(columns[1]);
            _cog = GetPoint3D(columns[2]);
            SetDepPos(columns[3]);
            _mass = double.Parse(columns[4]);

        }
        public static Point3D GetPoint3D(string str)
        {
            string[] substrings = str.Split(' ');
            Regex regex = new Regex(@"-?\d+");
            double x = double.Parse(regex.Match(substrings[1]).Value);
            double y = double.Parse(regex.Match(substrings[3]).Value);
            double z = double.Parse(regex.Match(substrings[5]).Value);
            Point3D vector = new Point3D(x, y, z);
            return vector;
        }
        public void SetDepPos(string str)
        {
            _depPos = new List<Point3D>();
            if (str == null || str == string.Empty)
                return;
            List<string> posStrList = str.Trim().Split('+').ToList();
            if (posStrList.Count() > 0)
                posStrList.ForEach(s => _depPos.Add(GetPoint3D(s)));
        }
        public string Name
        {
            get { return _name; }
        }
        public Point3D Pos
        {
            get { return _pos; }
        }
        public Point3D Cog
        {
            get { return _cog; }
        }
        public double Mass
        {
            get { return _mass; }
        }
        public List<Point3D> DepPos
        {
            get { return _depPos; }
        }
    }
}
