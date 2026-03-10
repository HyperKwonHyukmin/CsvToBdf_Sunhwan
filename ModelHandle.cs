using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using CsvToBdf.AMData;
using CsvToBdf.FEData;

// ver.2409

namespace CsvToBdf.Control
{
    public static class ModelHandle
    {
        public static Point3D GetClosestPoint(Point3D target, Point3D poss, Point3D pose)
        {
            Point3D near = new Point3D();
            Vector3D lineVector = pose - poss;
            Vector3D pointVector = target - poss;
            double dotProduct = Vector3D.DotProduct(lineVector, pointVector);
            double lineLengthSquared = lineVector.LengthSquared;
            double parameter = dotProduct / lineLengthSquared;
            near = poss + parameter * lineVector;
            return near;
        }
        
        public static Point3D GetClosestPoint(AMPipe pipe, AMStru stru)
        {
            Point3D p1 = pipe.APos;
            Point3D p2 = pipe.LPos;
            Point3D q1 = stru.Poss;
            Point3D q2 = stru.Pose;
            Vector3D direction1 = p2 - p1;
            Vector3D direction2 = q2 - q1;
            Vector3D cross = Vector3D.CrossProduct(direction1, direction2);
            if (cross.LengthSquared < 0.0001) // parallel lines
                return new Point3D();
            double distance = (Vector3D.DotProduct((q1 - p1), cross)) / cross.Length;
            Point3D closestPoint1 = p1 + direction1 * (Vector3D.DotProduct((q1 - p1), direction1) / direction1.LengthSquared);
            return closestPoint1;
        }
        public static Point3D GetClosestPoint(AMStru stru, AMPipe pipe)
        {
            Point3D p1 = stru.Poss;
            Point3D p2 = stru.Pose;
            Point3D q1 = pipe.APos;
            Point3D q2 = pipe.LPos;
            Vector3D direction1 = p2 - p1;
            Vector3D direction2 = q2 - q1;
            Vector3D cross = Vector3D.CrossProduct(direction1, direction2);
            if (cross.LengthSquared < 0.0001) // parallel lines
                return new Point3D();
            double distance = (Vector3D.DotProduct((q1 - p1), cross)) / cross.Length;
            Point3D closestPoint1 = p1 + direction1 * (Vector3D.DotProduct((q1 - p1), direction1) / direction1.LengthSquared);
            if (!ModelHandle.IsPointOnLineBetweenTwoPoints(closestPoint1, p1, p2))
                return new Point3D();
            return closestPoint1;
        }
        /// <summary>
        /// ver.2409 position list중 target과 가장 가까운 position을 찾아 리턴
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="posList"></param>
        /// <returns></returns>
        public static Point3D GetMinDistPosition(Point3D targetPos, List<Point3D> posList)
        {
            Point3D minDistPos = posList[0];
            double preDist = GetDistance(targetPos,posList[0]);
            double dist;
            foreach(Point3D pos in posList.Skip(1))
            {
                dist = GetDistance(targetPos, pos);
                if (dist < preDist)
                {
                    preDist = dist;
                    minDistPos = pos;
                }
            }
            return minDistPos;
        }
        public static Point3D GetClosestPoint(AMStru stru1, AMStru stru2)
        {
            Point3D p1 = stru1.Poss;
            Point3D p2 = stru1.Pose;
            Point3D q1 = stru2.Poss;
            Point3D q2 = stru2.Pose;
            Vector3D direction1 = p2 - p1;
            Vector3D direction2 = q2 - q1;
            Vector3D cross = Vector3D.CrossProduct(direction1, direction2);
            if (cross.LengthSquared < 0.0001) // parallel lines
                return new Point3D();
            double distance = (Vector3D.DotProduct((q1 - p1), cross)) / cross.Length;
            Point3D closestPoint1 = p1 + direction1 * (Vector3D.DotProduct((q1 - p1), direction1) / direction1.LengthSquared);
            if (!ModelHandle.IsPointOnLineBetweenTwoPoints(closestPoint1, p1, p2))
                return new Point3D();
            return closestPoint1;
        }
        public static double GetDistanceClosestPoint(Point3D target, Point3D poss, Point3D pose)
        {
            Point3D near = new Point3D();
            Vector3D lineVector = pose - poss;
            Vector3D pointVector = target - poss;
            double dotProduct = Vector3D.DotProduct(pointVector, lineVector);
            double lineLengthSquared = lineVector.LengthSquared;
            double parameter = dotProduct / lineLengthSquared;
            if (parameter <= 0)
                near = poss;
            else if (parameter >= 1)
                near = pose;
            else
                near = poss + parameter * lineVector;
            return GetDistance(near, target);
        }

        public static Point3D GetNearestPointOnA(AMStru b, AMPipe a, bool clamp = true)
        {
            var eta = 1e-6;
            var r = b.Poss - a.APos;
            var u = a.LPos - a.APos;
            var v = b.Pose - b.Poss;
            var ru = Vector3D.DotProduct(r, u);
            var rv = Vector3D.DotProduct(r, v);
            var uu = Vector3D.DotProduct(u, u);
            var uv = Vector3D.DotProduct(u, v);
            var vv = Vector3D.DotProduct(v, v);
            var det = uu * vv - uv * uv;

            double s, t;
            if (det < eta * uu * vv)
            {
                s = OptionalClamp01(ru / uu, clamp);
                t = 0;
            }
            else
            {
                s = OptionalClamp01((ru * vv - rv * uv) / det, clamp);
                t = OptionalClamp01((ru * uv - rv * uu) / det, clamp);
            }

            var S = OptionalClamp01((t * uv + ru) / uu, clamp);
            var T = OptionalClamp01((s * uv - rv) / vv, clamp);

            var A = a.APos + S * u;
            var B = b.Poss + T * v;
            return B;
        }
        public static double DistanceBetweenTwoCenLines(AMStru a, AMStru b, bool clamp = true)
        {
            var eta = 1e-6;
            var r = b.CenPoss - a.CenPoss;
            var u = a.CenPose - a.CenPoss;
            var v = b.CenPose - b.CenPoss;
            var ru = Vector3D.DotProduct(r, u);
            var rv = Vector3D.DotProduct(r, v);
            var uu = Vector3D.DotProduct(u, u);
            var uv = Vector3D.DotProduct(u, v);
            var vv = Vector3D.DotProduct(v, v);
            var det = uu * vv - uv * uv;

            double s, t;
            if (det < eta * uu * vv)
            {
                s = OptionalClamp01(ru / uu, clamp);
                t = 0;
            }
            else
            {
                s = OptionalClamp01((ru * vv - rv * uv) / det, clamp);
                t = OptionalClamp01((ru * uv - rv * uu) / det, clamp);
            }

            var S = OptionalClamp01((t * uv + ru) / uu, clamp);
            var T = OptionalClamp01((s * uv - rv) / vv, clamp);

            var A = a.CenPoss + S * u;
            var B = b.CenPoss + T * v;
            return GetDistance(A, B);
        }
        public static Point3D GetNearestPointOnA(Point3D p1, Point3D p2, AMPipe b, bool clamp = true)
        {
            var eta = 1e-6;
            var r = b.APos - p1;
            var u = p2 - p1;
            var v = b.LPos - b.APos;
            var ru = Vector3D.DotProduct(r, u);
            var rv = Vector3D.DotProduct(r, v);
            var uu = Vector3D.DotProduct(u, u);
            var uv = Vector3D.DotProduct(u, v);
            var vv = Vector3D.DotProduct(v, v);
            var det = uu * vv - uv * uv;

            double s, t;
            if (det < eta * uu * vv)
            {
                s = OptionalClamp01(ru / uu, clamp);
                t = 0;
            }
            else
            {
                s = OptionalClamp01((ru * vv - rv * uv) / det, clamp);
                t = OptionalClamp01((ru * uv - rv * uu) / det, clamp);
            }

            var S = OptionalClamp01((t * uv + ru) / uu, clamp);
            var T = OptionalClamp01((s * uv - rv) / vv, clamp);

            var A = p1 + S * u;
            var B = b.APos + T * v;
            return A;
        }
        public static Point3D GetNearestPointOnA(AMStru a, AMStru b, bool clamp = true)
        {
            var eta = 1e-6;
            var r = b.Poss - a.Poss;
            var u = a.Pose - a.Poss;
            var v = b.Pose - b.Poss;
            var ru = Vector3D.DotProduct(r, u);
            var rv = Vector3D.DotProduct(r, v);
            var uu = Vector3D.DotProduct(u, u);
            var uv = Vector3D.DotProduct(u, v);
            var vv = Vector3D.DotProduct(v, v);
            var det = uu * vv - uv * uv;

            double s, t;
            if (det < eta * uu * vv)
            {
                s = OptionalClamp01(ru / uu, clamp);
                t = 0;
            }
            else
            {
                s = OptionalClamp01((ru * vv - rv * uv) / det, clamp);
                t = OptionalClamp01((ru * uv - rv * uu) / det, clamp);
            }

            var S = OptionalClamp01((t * uv + ru) / uu, clamp);
            var T = OptionalClamp01((s * uv - rv) / vv, clamp);

            var A = a.Poss + S * u;
            var B = b.Poss + T * v;
            return A;
        }

        private static double OptionalClamp01(double value, bool clamp)
        {
            if (!clamp) return value;
            if (value > 1) return 1;
            if (value < 0) return 0;
            return value;
        }

        public static bool IsParallel(AMStru stru ,AMPipe pipe)
        {
            Point3D p1 = stru.Poss;
            Point3D p2 = stru.Pose;
            Point3D q1 = pipe.APos;
            Point3D q2 = pipe.LPos;
            Vector3D direction1 = p2 - p1;
            Vector3D direction2 = q2 - q1;
            Vector3D cross = Vector3D.CrossProduct(direction1, direction2);
            if (cross.LengthSquared < 0.0001) // parallel lines
                return true;
            else
                return false;
        }
        public static double GetDistance(Point3D one, Point3D two)
        {
            double dist = Math.Sqrt(Math.Pow(two.X - one.X, 2) + Math.Pow(two.Y - one.Y, 2) + Math.Pow(two.Z - one.Z, 2));
            return dist;
        }
        public static double GetDistance(Node one, Node two)
        {
            double dist = Math.Sqrt(Math.Pow(two.X - one.X, 2) + Math.Pow(two.Y - one.Y, 2) + Math.Pow(two.Z - one.Z, 2));
            return dist;
        }
        public static double GetDistance(Node one, Point3D two)
        {
            double dist = Math.Sqrt(Math.Pow(two.X - one.X, 2) + Math.Pow(two.Y - one.Y, 2) + Math.Pow(two.Z - one.Z, 2));
            return dist;
        }
        public static double GetDistance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double dist = Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2) + Math.Pow(z1 - z2, 2));
            return dist;
        }
        public static bool IsPointOnLineBetweenTwoPoints(Point3D point, Point3D lineStart, Point3D lineEnd)
        {
            double distanceStartToPoint = (point - lineStart).Length;
            double distanceEndToPoint = (point - lineEnd).Length;
            double lineLength = (lineEnd - lineStart).Length;
            double tolerance = 1; // adjust as needed
            if (Math.Abs(distanceStartToPoint + distanceEndToPoint - lineLength) < tolerance)
            {
                return true;
            }
            return false;
        }

        public static bool IsPointOnLineBetweenTwoPoints(Point3D point, Point3D lineStart, Point3D lineEnd, double tolerance)
        {
            double distanceStartToPoint = (point - lineStart).Length;
            double distanceEndToPoint = (point - lineEnd).Length;
            double lineLength = (lineEnd - lineStart).Length;
            if (Math.Abs(distanceStartToPoint + distanceEndToPoint - lineLength) < tolerance)
            {
                return true;
            }
            return false;
        }
        public static bool IsNodeOnLineBetweenTwoNode(Node target, Node poss, Node pose)
        {
            double distStartToTarget = ModelHandle.GetDistance(target, poss);
            double distEndToTarget = ModelHandle.GetDistance(target, pose);
            double lineLength = ModelHandle.GetDistance(poss, pose);
            if (Math.Abs(distStartToTarget + distEndToTarget - lineLength) < 0.01)
                return true;
            return false;
        }

        public static Point3D GetMidPoint3D(Point3D p1, Point3D p2)
        {
            return new Point3D((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);
        }
        public static bool isPosInVol(Point3D pos, List<double> vol)
        {
            double tol = 1;
            if (pos.X >= vol[0] - tol && pos.X <= vol[3] + tol && pos.Y >= vol[1] - tol && pos.Y <= vol[4] + tol && pos.Z >= vol[2] - tol && pos.Z <= vol[5] + tol)
                return true;
            else
                return false;
        }
        public static Point3D GetIntersection(Point3D line1Start, Point3D line1End, Point3D line2Start, Point3D line2End)
        {
            // 첫 번째 직선의 법선 벡터 계산
            double line1NormalX = line1End.Y * line1Start.Z - line1End.Z * line1Start.Y;
            double line1NormalY = line1End.Z * line1Start.X - line1End.X * line1Start.Z;
            double line1NormalZ = line1End.X * line1Start.Y - line1End.Y * line1Start.X;

            // 두 번째 직선의 법선 벡터 계산
            double line2NormalX = line2End.Y * line2Start.Z - line2End.Z * line2Start.Y;
            double line2NormalY = line2End.Z * line2Start.X - line2End.X * line2Start.Z;
            double line2NormalZ = line2End.X * line2Start.Y - line2End.Y * line2Start.X;

            // 두 법선 벡터의 내적 계산
            double dotProduct = line1NormalX * line2NormalX + line1NormalY * line2NormalY + line1NormalZ * line2NormalZ;

            // 두 직선이 평면에 평행한 경우
            if (Math.Abs(dotProduct) < 0.000001)
            {
                return new Point3D(0, 0, 0);
            }

            // 첫 번째 직선과 두 번째 직선 사이의 거리 계산
            double line1StartToEndX = line1End.X - line1Start.X;
            double line1StartToEndY = line1End.Y - line1Start.Y;
            double line1StartToEndZ = line1End.Z - line1Start.Z;

            double line1ToLine2StartX = line2Start.X - line1Start.X;
            double line1ToLine2StartY = line2Start.Y - line1Start.Y;
            double line1ToLine2StartZ = line2Start.Z - line1Start.Z;

            double numerator = line2NormalX * line1ToLine2StartX + line2NormalY * line1ToLine2StartY + line2NormalZ * line1ToLine2StartZ;
            double denominator = line2NormalX * line1StartToEndX + line2NormalY * line1StartToEndY + line2NormalZ * line1StartToEndZ;

            double t = numerator / denominator;

            // 교차점 좌표 계산
            double intersectionX = line1Start.X + line1StartToEndX * t;
            double intersectionY = line1Start.Y + line1StartToEndY * t;
            double intersectionZ = line1Start.Z + line1StartToEndZ * t;

            return new Point3D(intersectionX, intersectionY, intersectionZ);
            //    // Calculate the direction vectors of the two lines
            //    Point3D directionA = new Point3D(
            //    pos1End.X - pos1Start.X,
            //    pos1End.Y - pos1Start.Y,
            //    pos1End.Z - pos1Start.Z
            //);

            //    Point3D directionB = new Point3D(
            //        pos2End.X - pos2Start.X,
            //        pos2End.Y - pos2Start.Y,
            //        pos2End.Z - pos2Start.Z
            //    );
            //    Point3D crossProduct = new Point3D(
            //    directionA.Y * directionB.Z - directionA.Z * directionB.Y,
            //    directionA.Z * directionB.X - directionA.X * directionB.Z,
            //    directionA.X * directionB.Y - directionA.Y * directionB.X
            //    );
            //    Point3D startVector;
            //    // Check if the cross product is zero-length (parallel or collinear lines)
            //    if (crossProduct.X == 0 && crossProduct.Y == 0 && crossProduct.Z == 0)
            //    {
            //        // Calculate the vector between the starting points of the two lines
            //        startVector = new Point3D(
            //            pos2Start.X - pos1Start.X,
            //            pos2Start.Y - pos1Start.Y,
            //            pos2Start.Z - pos1Start.Z
            //        );

            //        // Calculate the dot products
            //        double dotProductA = startVector.X * directionA.X +
            //                             startVector.Y * directionA.Y +
            //                             startVector.Z * directionA.Z;

            //        double dotProductB = startVector.X * directionB.X +
            //                             startVector.Y * directionB.Y +
            //                             startVector.Z * directionB.Z;

            //        // Calculate the scalar values
            //        double scalarA = dotProductA / (directionA.X * directionA.X +
            //                                        directionA.Y * directionA.Y +
            //                                        directionA.Z * directionA.Z);

            //        double scalarB = dotProductB / (directionB.X * directionB.X +
            //                                        directionB.Y * directionB.Y +
            //                                        directionB.Z * directionB.Z);

            //        // Check if the scalar values are within the line segments
            //        if (scalarA >= 0 && scalarA <= 1 && scalarB >= 0 && scalarB <= 1)
            //        {
            //            // Calculate the coordinates of the intersection point
            //            double intersectionX = pos1Start.X + scalarA * directionA.X;
            //            double intersectionY = pos1Start.Y + scalarA * directionA.Y;
            //            double intersectionZ = pos1Start.Z + scalarA * directionA.Z;

            //            // Create and return the intersection point
            //            return new Point3D(intersectionX, intersectionY, intersectionZ);
            //        }
            //    }
            //    return new Point3D(0, 0, 0);

            //// Calculate the dot product of the direction vectors
            //double dotProduct = directionA.X * directionB.X +
            //                    directionA.Y * directionB.Y +
            //                    directionA.Z * directionB.Z;

            //// Check if the lines are parallel (dot product = 1 or -1 for parallel lines)
            //if (Math.Abs(dotProduct) == 1)
            //{
            //    return new Point3D(0,0,0); // Lines are parallel, return null
            //}

            //// Calculate the vector between the starting points of the two lines
            //Point3D startVector = new Point3D(
            //    pos2Start.X - pos1Start.X,
            //    pos2Start.Y - pos1Start.Y,
            //    pos2Start.Z - pos1Start.Z
            //);

            //// Calculate the projection scalar
            //double projectionScalar = (startVector.X * directionA.X +
            //                           startVector.Y * directionA.Y +
            //                           startVector.Z * directionA.Z) / dotProduct;

            //// Calculate the coordinates of the projected point on lineA
            //double projectedX = pos1Start.X + projectionScalar * directionA.X;
            //double projectedY = pos1Start.Y + projectionScalar * directionA.Y;
            //double projectedZ = pos1Start.Z + projectionScalar * directionA.Z;

            //// Create and return the projected point
            //return new Point3D(projectedX, projectedY, projectedZ);
        }
        public static Point3D GetIntersection1(Point3D pos1Start, Point3D pos1End, Point3D pos2Start, Point3D pos2End)
        {
            // 선분 1의 시작점과 끝점을 좌표로 분리합니다.
            double x1 = pos1Start.X;
            double y1 = pos1Start.Y;
            double z1 = pos1Start.Z;

            double x2 = pos1End.X;
            double y2 = pos1End.Y;
            double z2 = pos1End.Z;

            // 선분 2의 시작점과 끝점을 좌표로 분리합니다.
            double x3 = pos2Start.X;
            double y3 = pos2Start.Y;
            double z3 = pos2Start.Z;

            double x4 = pos2End.X;
            double y4 = pos2End.Y;
            double z4 = pos2End.Z;

            // 첫 번째 선분의 방향 벡터와 두 번째 선분의 방향 벡터를 계산합니다.
            double dx1 = x2 - x1;
            double dy1 = y2 - y1;
            double dz1 = z2 - z1;

            double dx2 = x4 - x3;
            double dy2 = y4 - y3;
            double dz2 = z4 - z3;
            double crossProduct = dx1 * dy2 - dx2 * dy1;
            if (Math.Abs(crossProduct) < double.Epsilon)
                return new Point3D(0, 0, 0);
            double t = ((x3 - x1) * dy2 - (y3 - y1) * dx2) / crossProduct;
            double u = -((x3 - x1) * dy1 - (y3 - y1) * dx1) / crossProduct;
            double intersectionX = x1 + t * dx1;
            double intersectionY = y1 + t * dy1;
            double intersectionZ = z1 + t * dz1;
            return new Point3D { X = intersectionX, Y = intersectionY, Z = intersectionZ };
        }
    }
}
