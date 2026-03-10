using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Text.RegularExpressions;

namespace CsvToBdf.AMData
{
    public class AMStru
    {
        private string _name;
        private string _type;
        private Point3D _pos;
        // Start/End Position
        public Point3D _poss;
        public Point3D _pose;
        // 최종 Start/End Position
        private Point3D _finalPoss;
        private Point3D _finalPose;
        private string _size;
        private string _stru;
        private string _ori;
        private List<Point3D> _interPos;
        private List<double> _wvol;
        private double _rad;
        // Volume의 Start/End 꼭지점 좌표
        public List<Point3D> Corners { get; set; }
        // Pad로 인한 연결 부재를 막기 위해 연장한 꼭지점 좌표
        public List<Point3D> ExtCorners { get; set; }
        // Start에 연결된 부재 리스트
        public string[] StartConnection { get; set; }
        // End에 연결된 부재 리스트
        public string[] EndConnection { get; set; }
        // 중간에 걸쳐져 있는 부재 리스트
        public List<string> InterConnections { get; set; }
        // 기울어져있는지 확인
        public bool isSlanted;
        // 이동여부 판단
        public bool HasBeenMoved { get; set; }
        // 체크여부 판단
        public bool HasBeenChecked { get; set; }
        // Support인지 철의장인지 구분용
        public string Division { get; set; }
        // 연결점 확인을 위한 Center Position Start/End
        public Point3D CenPoss { get; set; }
        public Point3D CenPose { get; set; }
        // AM 단계의 원래 위치
        public Point3D OriPoss { get; set; }
        public Point3D OriPose { get; set; }
        public double Rad2 { get; set; }
        // 선체와 용접 여부 
        public string Weld { get; set; }
        public AMStru()
        {

        }
        public AMStru(AMStru other)
        {
            _interPos = new List<Point3D>();
            HasBeenMoved = other.HasBeenMoved;
            HasBeenChecked = other.HasBeenChecked;
            InterConnections = new List<string>();
            StartConnection = new string[5];
            EndConnection = new string[5];
            _name = other.Name;
            _type = other.Type;
            _pos = other.Pos;
            _poss = other.Poss;
            _pose = other.Pose;
            if (_poss == _pose)
                return;
            _finalPoss = other.FinalPoss;
            _finalPose = other.FinalPose;
            _size = other.Size;
            _stru = other.Stru;
            _ori = other.Ori;
            _interPos = other.InterPos;
            _wvol = other.Wvol;
            _rad = other.Rad;
            Division = other.Division;
            OriPoss = other.OriPoss;
            OriPose = other.OriPose;
            Weld = other.Weld;
        }
        public AMStru(string row)
        {
            _interPos = new List<Point3D>();
            InterConnections = new List<string>();
            StartConnection = new string[5];
            EndConnection = new string[5];
            isSlanted = false;
            HasBeenMoved = false;
            HasBeenChecked = false;
            string[] columns = row.Split(',');
            _name = columns[0];
            _type = columns[1];
            _pos = GetPoint3D(columns[2]);
            _poss = GetPoint3D(columns[3]);
            _pose = GetPoint3D(columns[4]);
            _finalPoss = _poss;
            _finalPose = _pose;
            OriPoss = _poss;
            OriPose = _pose;
            _size = columns[5];
            _stru = columns[6];
            _ori = columns[7];
            //System.Diagnostics.Debugger.Launch();
            //SetIntersection(columns[8]);
            _wvol = new List<double>();
            Division = columns[8];
            if (string.IsNullOrEmpty(Division))
                Division = "OUTF";
            if (columns.Count() > 9)
                Weld = columns[9];
            else
                Weld = string.Empty;
            
            SetCorners();

        }
        //public void ExtendWVol()
        //{
        //    double extValue = 30;
        //    Vector3D xDir = _pose - _poss;
        //    double xLen = xDir.Length;
        //    xDir = new Vector3D(xDir.X / xLen * extValue, xDir.Y / xLen * extValue, xDir.Z / xLen * extValue);
        //    List<double> newWvol = new List<double>();
        //    newWvol.Add(_wvol[0] - Math.Abs(xDir.X));
        //    newWvol.Add(_wvol[1] - Math.Abs(xDir.Y));
        //    newWvol.Add(_wvol[2] - Math.Abs(xDir.Z));
        //    newWvol.Add(_wvol[3] + Math.Abs(xDir.X));
        //    newWvol.Add(_wvol[4] + Math.Abs(xDir.Y));
        //    newWvol.Add(_wvol[5] + Math.Abs(xDir.Z));
        //    _wvol = newWvol.ToList();
        //}
        //public void SetIntersection(string str)
        //{
        //    _interPos = new List<Point3D>();
        //    if (str == null || str == string.Empty)
        //        return;
        //    List<string> posStrList = str.Trim().Split('+').ToList();
        //    if (posStrList.Count() > 0)
        //        posStrList.ForEach(s => _interPos.Add(GetPoint3D(s)));
        //}
        // Point3D로 변환
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
        // 길이방향 volume 8개 꼭지점 추출 (연결부재 찾기용)
        private void SetCorners()
        {
            // pad를 고려한 길이방향 확장
            double tol = 21;
            Point3D newPoss;
            Point3D newPose;
            Corners = new List<Point3D>();
            ExtCorners = new List<Point3D>();
            Vector3D xdir = _poss - _pose;
            double xDist = xdir.Length;
            newPoss = _poss;
            newPose = _pose;
            if (xdir.X == 0 && xdir.Y == 0 && xdir.Z == 0)
                return;
            if (xdir.X == 0 ? (xdir.Y != 0 && xdir.Z != 0) : (xdir.Y != 0 || xdir.Z != 0))
                isSlanted = true;
            string[] tempArr = _ori.TrimStart().Split(' ');
            List<string> tempList = new List<string>();
            foreach (string temp in tempArr)
            {
                if (string.IsNullOrEmpty(temp))
                    continue;
                tempList.Add(temp);
            }
            Vector3D ydir = new Vector3D(double.Parse(tempList[0]), double.Parse(tempList[1]), double.Parse(tempList[2]));
            Vector3D zdir = Vector3D.CrossProduct(xdir, ydir);
            double dist = zdir.Length;
            zdir = new Vector3D(zdir.X / dist, zdir.Y / dist, zdir.Z / dist);
            string[] para = _size.Split('_')[1].Split('x');
            double yDist;
            double zDist = double.Parse(para[0]);
            if (_size.Contains("BEAM") || _size.Contains("JISI") || _size.Contains("FBAR") || _size.Contains("BULB"))
            {
                yDist = double.Parse(para[1]);
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / -2 + zdir.X * zDist / -2, newPoss.Y + ydir.Y * yDist / -2 + zdir.Y * zDist / -2, newPoss.Z + ydir.Z * yDist / -2 + zdir.Z * zDist / -2));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / -2 + zdir.X * zDist / 2, newPoss.Y + ydir.Y * yDist / -2 + zdir.Y * zDist / 2, newPoss.Z + ydir.Z * yDist / -2 + zdir.Z * zDist / 2));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / 2 + zdir.X * zDist / 2, newPoss.Y + ydir.Y * yDist / 2 + zdir.Y * zDist / 2, newPoss.Z + ydir.Z * yDist / 2 + zdir.Z * zDist / 2));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / 2 + zdir.X * zDist / -2, newPoss.Y + ydir.Y * yDist / 2 + zdir.Y * zDist / -2, newPoss.Z + ydir.Z * yDist / 2 + zdir.Z * zDist / -2));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / -2 + zdir.X * zDist / -2, newPose.Y + ydir.Y * yDist / -2 + zdir.Y * zDist / -2, newPose.Z + ydir.Z * yDist / -2 + zdir.Z * zDist / -2));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / -2 + zdir.X * zDist / 2, newPose.Y + ydir.Y * yDist / -2 + zdir.Y * zDist / 2, newPose.Z + ydir.Z * yDist / -2 + zdir.Z * zDist / 2));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / 2 + zdir.X * zDist / 2, newPose.Y + ydir.Y * yDist / 2 + zdir.Y * zDist / 2, newPose.Z + ydir.Z * yDist / 2 + zdir.Z * zDist / 2));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / 2 + zdir.X * zDist / -2, newPose.Y + ydir.Y * yDist / 2 + zdir.Y * zDist / -2, newPose.Z + ydir.Z * yDist / 2 + zdir.Z * zDist / -2));
                if (_size.Contains("BEAM") || _size.Contains("JISI"))
                    _rad = Math.Max(yDist, zDist) / 2 * 1.4;
                else
                    _rad = Math.Max(yDist, zDist) / 2;
                Rad2 = _rad;
            }
            else if (_size.Contains("ANG"))
            {
                yDist = double.Parse(para[1]);
                Corners.Add(newPoss);
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist, newPoss.Y + ydir.Y * yDist, newPoss.Z + ydir.Z * yDist));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist - zdir.X * zDist, newPoss.Y + ydir.Y * yDist - zdir.Y * zDist, newPoss.Z + ydir.Z * yDist - zdir.Z * zDist));
                Corners.Add(new Point3D(newPoss.X - zdir.X * zDist, newPoss.Y - zdir.Y * zDist, newPoss.Z - zdir.Z * zDist));
                Corners.Add(newPose);
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist, newPose.Y + ydir.Y * yDist, newPose.Z + ydir.Z * yDist));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist - zdir.X * zDist, newPose.Y + ydir.Y * yDist - zdir.Y * zDist, newPose.Z + ydir.Z * yDist - zdir.Z * zDist));
                Corners.Add(new Point3D(newPose.X - zdir.X * zDist, newPose.Y - zdir.Y * zDist, newPose.Z - zdir.Z * zDist));
                _rad = Math.Max(zDist, yDist);
                Rad2 = _rad / 2;
            }
            else if (_size.Contains("TUBE") || _size.Contains("RBAR"))
            {
                zDist = double.Parse(para[0]) / 2;
                yDist = double.Parse(para[0]) / 2;
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist, newPoss.Y + ydir.Y * yDist, newPoss.Z + ydir.Z * yDist));
                Corners.Add(new Point3D(newPoss.X + zdir.X * zDist, newPoss.Y + zdir.Y * zDist, newPoss.Z + zdir.Z * zDist));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist * -1, newPoss.Y + ydir.Y * yDist * -1, newPoss.Z + ydir.Z * yDist * -1));
                Corners.Add(new Point3D(newPoss.X + zdir.X * zDist * -1, newPoss.Y + zdir.Y * zDist * -1, newPoss.Z + zdir.Z * zDist * -1));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist, newPose.Y + ydir.Y * yDist, newPose.Z + ydir.Z * yDist));
                Corners.Add(new Point3D(newPose.X + zdir.X * zDist, newPose.Y + zdir.Y * zDist, newPose.Z + zdir.Z * zDist));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist * -1, newPose.Y + ydir.Y * yDist * -1, newPose.Z + ydir.Z * yDist * -1));
                Corners.Add(new Point3D(newPose.X + zdir.X * zDist * -1, newPose.Y + zdir.Y * zDist * -1, newPose.Z + zdir.Z * zDist * -1));
                _rad = Math.Max(zDist, yDist);
                Rad2 = _rad;
            }
            else if (_size.Contains("BSC"))
            {
                yDist = double.Parse(para[1]);
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / 2, newPoss.Y + ydir.Y * yDist / 2, newPoss.Z + ydir.Z * yDist / 2));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / 2 + zdir.X * zDist, newPoss.Y + ydir.Y * yDist / 2 + zdir.Y * zDist, newPoss.Z + ydir.Z * yDist / 2 + zdir.Z * zDist));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / -2 + zdir.X * zDist, newPoss.Y + ydir.Y * yDist / -2 + zdir.Y * zDist, newPoss.Z + ydir.Z * yDist / -2 + zdir.Z * zDist));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist / -2, newPoss.Y + ydir.Y * yDist / -2, newPoss.Z + ydir.Z * yDist / -2));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / 2, newPose.Y + ydir.Y * yDist / 2, newPose.Z + ydir.Z * yDist / 2));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / 2 + zdir.X * zDist, newPose.Y + ydir.Y * yDist / 2 + zdir.Y * zDist, newPose.Z + ydir.Z * yDist / 2 + zdir.Z * zDist));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / -2 + zdir.X * zDist, newPose.Y + ydir.Y * yDist / -2 + zdir.Y * zDist, newPose.Z + ydir.Z * yDist / -2 + zdir.Z * zDist));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist / -2, newPose.Y + ydir.Y * yDist / -2, newPose.Z + ydir.Z * yDist / -2));
                _rad = Math.Max(yDist, zDist) / 2 * 1.4;
                Rad2 = _rad;
            }
            else if (_size.Contains("BOX"))
            {
                zDist = double.Parse(para[0]) / 2;
                yDist = double.Parse(para[1]) / 2;
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist, newPoss.Y + ydir.Y * yDist, newPoss.Z + ydir.Z * yDist));
                Corners.Add(new Point3D(newPoss.X + zdir.X * zDist, newPoss.Y + zdir.Y * zDist, newPoss.Z + zdir.Z * zDist));
                Corners.Add(new Point3D(newPoss.X + ydir.X * yDist * -1, newPoss.Y + ydir.Y * yDist * -1, newPoss.Z + ydir.Z * yDist * -1));
                Corners.Add(new Point3D(newPoss.X + zdir.X * zDist * -1, newPoss.Y + zdir.Y * zDist * -1, newPoss.Z + zdir.Z * zDist * -1));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist, newPose.Y + ydir.Y * yDist, newPose.Z + ydir.Z * yDist));
                Corners.Add(new Point3D(newPose.X + zdir.X * zDist, newPose.Y + zdir.Y * zDist, newPose.Z + zdir.Z * zDist));
                Corners.Add(new Point3D(newPose.X + ydir.X * yDist * -1, newPose.Y + ydir.Y * yDist * -1, newPose.Z + ydir.Z * yDist * -1));
                Corners.Add(new Point3D(newPose.X + zdir.X * zDist * -1, newPose.Y + zdir.Y * zDist * -1, newPose.Z + zdir.Z * zDist * -1));
                _rad = Math.Max(zDist, yDist);
                Rad2 = _rad;
            }
            for (int i = 0; i < 8; i++)
            {
                if (i < 4)
                    ExtCorners.Add(new Point3D(Corners[i].X + xdir.X / xDist * tol, Corners[i].Y + xdir.Y / xDist * tol, Corners[i].Z + xdir.Z / xDist * tol));
                else
                    ExtCorners.Add(new Point3D(Corners[i].X - xdir.X / xDist * tol, Corners[i].Y - xdir.Y / xDist * tol, Corners[i].Z - xdir.Z / xDist * tol));
            }
            CenPoss = new Point3D((Corners[0].X + Corners[2].X) / 2, (Corners[0].Y + Corners[2].Y) / 2, (Corners[0].Z + Corners[2].Z) / 2);
            CenPose = new Point3D((Corners[4].X + Corners[6].X) / 2, (Corners[4].Y + Corners[6].Y) / 2, (Corners[4].Z + Corners[6].Z) / 2);
            SetVol(ExtCorners);
        }
        // VOLUME 지정
        private void SetVol(List<Point3D> corners)
        {
            double[] newVol = new double[6];
            newVol[0] = corners.Min(s => s.X);
            newVol[1] = corners.Min(s => s.Y);
            newVol[2] = corners.Min(s => s.Z);
            newVol[3] = corners.Max(s => s.X);
            newVol[4] = corners.Max(s => s.Y);
            newVol[5] = corners.Max(s => s.Z);
            _wvol = newVol.ToList();
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
        }
        public Point3D Poss
        {
            get { return _poss; }
            set { _poss = value; }
        }
        public Point3D Pose
        {
            get { return _pose; }
            set { _pose = value; }
        }
        public Point3D FinalPoss
        {
            get { return _finalPoss; }
            set { _finalPoss = value; }
        }
        public Point3D FinalPose
        {
            get { return _finalPose; }
            set { _finalPose = value; }
        }
        public string Size
        {
            get { return _size; }
        }
        public string Stru
        {
            get { return _stru; }
        }
        public string Ori
        {
            get { return _ori; }
        }
        public List<Point3D> InterPos
        {
            get { return _interPos; }
            set { _interPos = value; }
        }
        public List<double> Wvol
        {
            get { return _wvol; }
        }
        public double Rad
        {
            get { return _rad; }
            set { _rad = value; }
        }
    }
}
