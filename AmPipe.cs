using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Text.RegularExpressions;

namespace CsvToBdf.AMData
{
    public class AMPipe
    {
        private string _name;
        private string _type;
        private Point3D _pos;
        private Point3D _apos;
        private Point3D _lpos;
        private string _bran;
        private double _outDia;
        private double _thick;
        private string _normal;
        private List<Point3D> _interPos;
        private Point3D _p3Pos;
        private double _outDia2;
        private double _thick2;
        private string _rest;
        private List<double> _wvol;
        private double _mass;
        private string _remark;
        public AMPipe()
        {

        }
        public string Name
        {
            get { return _name; }
        }
        public string Type
        {
            get { return _type; }
        }
        public Point3D Pos
        {
            get { return _pos; }
            set { _pos = value; }
        }
        public Point3D APos
        {
            get { return _apos; }
            set { _apos = value; }
        }
        public Point3D LPos
        {
            get { return _lpos; }
            set { _lpos = value; }
        }
        public Point3D P3Pos
        {
            get { return _p3Pos; }
            set { _p3Pos = value; }
        }
        public string Bran
        {
            get { return _bran; }
        }
        public double OutDia
        {
            get { return _outDia; }
        }
        public double Thick
        {
            get { return _thick; }
        }
        public string Normal
        {
            get { return _normal; }
        }
        public List<Point3D> InterPos
        {
            get { return _interPos; }
            set { _interPos = value; }
        }


        public double OutDia2
        {
            get { return _outDia2; }
        }
        public double Thick2
        {
            get { return _thick2; }
        }
        public string Rest
        {
            get { return _rest; }
        }
        public List<double> Wvol
        {
            get { return _wvol; }
        }
        public string Remark
        {
            get { return _remark; }
        }

        public double Mass
        {
            get { return _mass; }
        }
        public AMPipe(string row)
        {
            //System.Diagnostics.Debugger.Launch();

            try
            {
              _interPos = new List<Point3D>();
              string[] columns = row.Split(',');
              _name = columns[0];
              _type = columns[1];
              if (columns[2] != "")
                _pos = GetPoint3D(columns[2]);
              if (columns[3] != "")
                _apos = GetPoint3D(columns[3]);
              if (columns[4] != "")
                _lpos = GetPoint3D(columns[4]);
              _bran = columns[5];
              _outDia = double.Parse(columns[6]);
              _thick = double.Parse(columns[7]);
              _normal = columns[8];
              if (columns[9] != "")
                _interPos = GetListPos(columns[9]);
              if (columns[10] != "")
                _p3Pos = GetPoint3D(columns[10]);
              _outDia2 = double.Parse(columns[11]);
              _thick2 = double.Parse(columns[12]);
              _rest = columns[13];
              _mass = double.Parse(columns[14]);
              _wvol = new List<double>();
              columns[15].Split('+').ToList().ForEach(s => _wvol.Add(double.Parse(s)));
              _remark = columns[16];
            }
            catch (Exception ex)
            {
              // 디버그 출력 창에 에러 내용과 해당 줄의 전체 텍스트를 기록합니다.
              System.Diagnostics.Debug.WriteLine("--------------------------------------------------");
              System.Diagnostics.Debug.WriteLine($"[CSV 파싱 에러] 메시지: {ex.Message}");
              System.Diagnostics.Debug.WriteLine($"[에러 발생 데이터]: {row}");
              System.Diagnostics.Debug.WriteLine("--------------------------------------------------");

              // GUI 환경에서 사용자에게 알림을 띄우고 싶다면 아래 코드 사용
              // System.Windows.Forms.MessageBox.Show($"에러 발생 데이터: {row}");

              throw; // 에러를 다시 던져 디버거가 해당 위치에서 멈추게 함
            }


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
        private static List<Point3D> GetListPos(string str)
        {
            List<Point3D> posList = new List<Point3D>();
            List<string> substrings = str.Split('+').ToList();
            substrings.ForEach(s => posList.Add(GetPoint3D(s)));
            return posList;
        }
    }
}
