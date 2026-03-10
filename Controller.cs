using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsvToBdf.AMData;
using CsvToBdf.FEData;
//using HHI.COMM.FEA_INTERFACE.Control;
using System.Windows.Media.Media3D;
using CsvToBdf;
using System.Windows.Forms;

// ver.2410 ubolt용 평행 부재 삭제(x방향)
// ver.2409 spectacle flange 연결, ubolt용 평행 부재 삭제(y방향), 적층 구조 연결, x방향 bolting 부재 연결

namespace CsvToBdf.Control
{
    public class Controller
    {
        private AMModel amModel;
        private FEModel feModel;
        // ver.2409 Cable Tray 연결용 임시 Stru List
        private List<AMStru> tempStruRBEList;
        public Controller()
        {
            amModel = new AMModel();
            feModel = new FEModel();
        }
        public AMModel AmModel
        {
            get { return amModel; }
            set { amModel = value; }
        }
        public FEModel FeModel
        {
            get { return feModel; }
            set { feModel = value; }

        }
        /// <summary>
        /// CSV 파일 로드
        /// </summary>
        public void LoadFromCsv()
        {
            if ( Int32.Parse(Form1.mainForm.tBoxMeshSize.Text) < 50)
            {
                DialogResult result = MessageBox.Show("Mesh Size가 50보다 작습니다. 계속하시겠습니까?", "Message", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                    return;
            }
            //Application.DoEvents();
            //Form1.mainForm.labelStatus.Text = "Status : Proceeding";
            string pipeFilePath = System.IO.Path.GetFullPath(Form1.mainForm.tBoxPipePath.Text);
            string struFilePath = System.IO.Path.GetFullPath(Form1.mainForm.tBoxStruPath.Text);
            string equiFilePath = System.IO.Path.GetFullPath(Form1.mainForm.tBoxEquiPath.Text);
            // Pipe Model Load
            if (File.Exists(pipeFilePath))
                LoadPipeModelFromCsv(pipeFilePath);
            // Stru Model Load
            if (File.Exists(struFilePath))
                LoadStruModelFromCsv(struFilePath);
            // Equi Model Load
            if (File.Exists(equiFilePath))
                LoadEquiModelFromCsv(equiFilePath);

            // stru
            if (Form1.mainForm.cBoxConversion.Checked)
            {
                // 각 Member끝단의 주연결부재를 찾아 지정
                SetConnections();
                SetInterConnections();
                // stru 위치 조정
                TransformPositions();
                AddInterPos();

                // ver.2409 Spectacle Flange 반영
                ModifySpectacleFlange();

                ModifyTubiPosToFlange();
                ModifyUboltPos();

                // ver.2409
                if (Form1.mainForm.cBoxStrongConnection.Checked)
                {
                    // Main Support와 평행한 Ubolt 연결용 부재 삭제
                    AmModel.StruModels = AmModel.StruModels.FindAll(s => s.Division != "Remove");

                    tempStruRBEList = new List<AMStru>();
                    // Cable Tray 종방향 연결
                    ConnectCableTrayXForLPG();
                    // Cable Tray 적층 구조 연결
                    ConnectCableTrayZForLPG();
                }

            }

            FeModel.Init();
            FeModel.AddStruGrid(AmModel.StruModels);
            FeModel.AddProp(AmModel.StruModels);
            FeModel.AddStruElem(AmModel.StruModels);

            // ver.2409
            if (Form1.mainForm.cBoxStrongConnection.Checked)
            {
                FeModel.AddStruRbes(tempStruRBEList);
            }

            FeModel.AddProp(AmModel.PipeModels, Form1.mainForm.cBoxFluidDensity.Checked);
            FeModel.AddPipeGrid(AmModel.PipeModels);
            FeModel.AddPipeElem(AmModel.PipeModels);

            FeModel.AddEquiGridElement(AmModel.EquiModels);

            FeModel.CombineRbes();
            FeModel.RemoveDuplicatedElems();
            FeModel.Renumbering();

            // mesh 추가
            if (Form1.mainForm.cBoxMesh.Checked)
                FeModel.Meshing(Int32.Parse(Form1.mainForm.tBoxMeshSize.Text));

            // bdf
            BdfHandle.SaveBdfFile(feModel, System.IO.Path.GetFullPath(Form1.mainForm.tBoxBdfPath.Text));
            Form1.mainForm.labelStatus.Text = "Status : Complete";
        }

        /// <summary>
        /// ver.2409 LPGC Cable Tray의 종방향 연결
        /// </summary>
        private void ConnectCableTrayXForLPG()
        {
            List<AMStru> xDirSuppList = AmModel.StruModels.FindAll(s => s.Division == "SUPP" && s.OriPoss.Y == s.OriPose.Y && s.OriPoss.Z == s.OriPose.Z);
            AMStru struA;
            AMStru struB;
            AMStru tempStru;
            if (xDirSuppList.Count() < 1)
                return;
            for (int i = 0; i < xDirSuppList.Count()-1; i++)
            {
                struA = xDirSuppList[i];
                for (int j = i+1; j < xDirSuppList.Count();j++)
                {
                    struB = xDirSuppList[j];
                    if (isNearStru(struA, struB))
                        continue;
                    if (Math.Abs(struA.OriPoss.Y - struB.OriPoss.Y) > 2)
                        continue;
                    if (Math.Abs(struA.OriPoss.Z - struB.OriPoss.Z) > 2)
                        continue;
                    if (struA.Size != struB.Size)
                        continue;
                    if (Math.Abs(Math.Abs(struA.OriPoss.X - struB.OriPoss.X) - 50) < 5)
                    {
                        tempStru = new AMStru();
                        tempStru.Division = $"{struA.Name} {struB.Name}";
                        tempStru.Poss = struA.FinalPoss;
                        tempStru.Pose = struB.FinalPoss;
                        tempStruRBEList.Add(tempStru);
                    }
                    else if (Math.Abs(Math.Abs(struA.OriPoss.X - struB.OriPose.X) - 50) < 5)
                    {
                        tempStru = new AMStru();
                        tempStru.Division = $"{struA.Name} {struB.Name}";
                        tempStru.Poss = struA.FinalPoss;
                        tempStru.Pose = struB.FinalPose;
                        tempStruRBEList.Add(tempStru);
                    }
                    else if (Math.Abs(Math.Abs(struA.OriPose.X - struB.OriPoss.X) - 50) < 5)
                    {
                        tempStru = new AMStru();
                        tempStru.Division = $"{struA.Name} {struB.Name}";
                        tempStru.Poss = struA.FinalPose;
                        tempStru.Pose = struB.FinalPoss;
                        tempStruRBEList.Add(tempStru);
                    }
                    else if (Math.Abs(Math.Abs(struA.OriPose.X - struB.OriPose.X) - 50) < 5)
                    {
                        tempStru = new AMStru();
                        tempStru.Division = $"{struA.Name} {struB.Name}";
                        tempStru.Poss = struA.FinalPose;
                        tempStru.Pose = struB.FinalPose;
                        tempStruRBEList.Add(tempStru);
                    }
                }
            }
        }

        /// <summary>
        /// ver.2409 LPGC Cable Tray의 적층구조 연결
        /// </summary>
        private void ConnectCableTrayZForLPG()
        {
            List<AMStru> xDirSuppList = AmModel.StruModels.FindAll(s => s.Division == "SUPP" && s.OriPoss.Y == s.OriPose.Y && s.OriPoss.Z == s.OriPose.Z);
            List<AMStru> yDirSuppList = AmModel.StruModels.FindAll(s => s.Division == "SUPP" && s.OriPoss.X == s.OriPose.X && s.OriPoss.Z == s.OriPose.Z);
            //List<AMStru> tempSuppList;
            //AMStru struA;
            //AMStru struB;
            Point3D posA;
            Point3D posB;
            AMStru tempStru;
            if (xDirSuppList.Count() < 1 || yDirSuppList.Count() < 1)
                return;
            foreach (AMStru xDirSupp in xDirSuppList)
            {
                foreach (AMStru yDirSupp in yDirSuppList)
                {
                    // y방향 범위를 넘을시 skip
                    if (xDirSupp.Wvol[1] < yDirSupp.Wvol[1] || xDirSupp.Wvol[1] > yDirSupp.Wvol[4])
                        continue;
                    // x방향 범위를 넘을시 skip
                    if (yDirSupp.Wvol[0] < xDirSupp.Wvol[0] || yDirSupp.Wvol[0] > xDirSupp.Wvol[3])
                        continue;
                    // -z방향 9~15 밖에 있는 경우 skip
                    if ((xDirSupp.Wvol[2] - yDirSupp.Wvol[5]) < 9 || (xDirSupp.Wvol[2] - yDirSupp.Wvol[5]) > 15)
                        continue;
                    posA = ModelHandle.GetNearestPointOnA(xDirSupp, yDirSupp);
                    posB = ModelHandle.GetNearestPointOnA(yDirSupp, xDirSupp);
                    xDirSupp.InterPos.Add(posA);
                    yDirSupp.InterPos.Add(posB);
                    tempStru = new AMStru();
                    tempStru.Division = $"{xDirSupp} {yDirSupp}";
                    tempStru.Poss = posA;
                    tempStru.Pose = posB;
                    tempStruRBEList.Add(tempStru);
                }
            }
        }

        /// <summary>
        /// Pipe 모델 로드
        /// </summary>
        /// <param name="filePath"></param>
        private void LoadPipeModelFromCsv(string filePath)
        {
            string csvData;
            using (StreamReader reader = new StreamReader(filePath))
            {
                csvData = reader.ReadToEnd();
            }
            string[] rows = csvData.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows.Skip(1))
            {
                AMPipe pipeModel = new AMPipe(row);
                amModel.PipeModels.Add(pipeModel);
            }
        }

        /// <summary>
        /// (ver.2409) Spectacle Flange와 같이 Flange 3개일 경우, 일부 연결되지 않는 문제 해결
        /// 중간 Flange를 찾아 apos/pos을 인근 flange로 이동
        /// </summary>
        private void ModifySpectacleFlange()
        {
            List<AMPipe> flangeList = amModel.PipeModels.FindAll(s => s.Type == "FLAN");
            List<AMPipe> nearFlanges;
            List<Point3D> posList;
            foreach (AMPipe targetFlange in flangeList)
            {
                nearFlanges = flangeList.FindAll(s => s.Name != targetFlange.Name && (ModelHandle.GetDistance(targetFlange.LPos, s.Pos) < 10 || ModelHandle.GetDistance(targetFlange.APos, s.Pos) < 10));
                if (nearFlanges.Count() == 2)
                {
                    posList = new List<Point3D> { nearFlanges[0].APos, nearFlanges[0].LPos, nearFlanges[1].APos, nearFlanges[1].LPos };
                    targetFlange.APos = ModelHandle.GetMinDistPosition(targetFlange.APos, posList);
                    targetFlange.LPos = ModelHandle.GetMinDistPosition(targetFlange.LPos, posList);
                }
            }
        }

        /// <summary>
        /// TUBI를 Flange 위치까지 이동
        /// </summary>
        private void ModifyTubiPosToFlange()
        {
            //System.Diagnostics.Debugger.Launch();
            List<AMPipe> tubiList = amModel.PipeModels.FindAll(s => s.Type == "TUBI");
            List<AMPipe> flanList = amModel.PipeModels.FindAll(s => s.Type == "FLAN");
            List<Point3D> newPosList = new List<Point3D>();
            Point3D newPos;
            AMPipe tubi;
            for (int i = 0; i < flanList.Count; i++)
            {
                newPos = flanList[i].Pos;
                for (int j = 0; j < flanList.Count; j++)
                {
                    if (j <= i)
                        continue;
                    if (flanList[i].Pos == flanList[j].Pos)
                        continue;
                    if (ModelHandle.GetDistance(flanList[i].Pos, flanList[j].Pos) <= 20)
                    {
                        newPos = ModelHandle.GetMidPoint3D(flanList[i].Pos, flanList[j].Pos);
                        flanList[i].Pos = newPos;
                        flanList[j].Pos = newPos;
                    }
                }
                tubi = tubiList.Find(s => ModelHandle.GetDistance(s.LPos, newPos) <= 20);
                if (tubi != null)
                    tubi.LPos = newPos;
                tubi = tubiList.Find(s => ModelHandle.GetDistance(s.APos, newPos) <= 20);
                if (tubi != null)
                    tubi.APos = newPos;
            }
        }
    /// <summary>
    /// Ubolt 위치를 이동
    /// </summary>
    private void ModifyUboltPos()
    {
      List<AMPipe> uboltList = amModel.PipeModels.FindAll(s => s.Type == "UBOLT");
      List<AMPipe> tubiList = amModel.PipeModels.FindAll(s => s.Type == "TUBI");
      AMPipe nearTubi;
      List<AMStru> nearStruList;
      Point3D tempUboltPos = new Point3D();
      Point3D tempStruPos = new Point3D();
      List<Point3D> tempPosList;

      foreach (AMPipe ubolt in uboltList)
      {
        // 1. 배관(TUBI) 찾기
        nearTubi = tubiList.Find(s => s.Bran == ubolt.Bran && ModelHandle.IsPointOnLineBetweenTwoPoints(ubolt.Pos, s.APos, s.LPos) && ModelHandle.GetDistanceClosestPoint(ubolt.Pos, s.APos, s.LPos) <= 20);

        // 2. 구조물(Stru) 찾기
        nearStruList = GetNearStruToUbolt(ubolt);

        // 관경이 큰 경우 범위 넓혀서 다시 찾기
        if (nearStruList.Count == 0 && nearTubi != null && nearTubi.OutDia > 650)
        {
          nearStruList = amModel.StruModels.FindAll(s => isNearStru(ubolt, s, 20));
        }

        // [실패 원인 1 & 2] 배관이나 구조물을 아예 못 찾은 경우
        if (nearTubi == null || nearStruList.Count == 0)
        {
          System.Diagnostics.Debug.WriteLine($"[RBE 실패] UBOLT({ubolt.Name}): " +
              $"{(nearTubi == null ? "배관(TUBI) 매칭 실패" : "")} " +
              $"{(nearStruList.Count == 0 ? "주변 구조물(Stru) 찾기 실패" : "")}");
          continue;
        }

        // 3. 배관과 구조물 사이의 가장 가까운 Ubolt 위치 찾기
        foreach (AMStru nearStru in nearStruList)
        {
          tempUboltPos = ModelHandle.GetClosestPoint(nearTubi, nearStru);
          if (tempUboltPos != null && tempUboltPos != new Point3D(0, 0, 0))
            break;
        }

        // [실패 원인 3] 기하학적으로 가장 가까운 교차점을 계산하지 못한 경우
        if (tempUboltPos == null || tempUboltPos == new Point3D(0, 0, 0))
        {
          System.Diagnostics.Debug.WriteLine($"[RBE 실패] UBOLT({ubolt.Name}): 배관과 구조물 사이의 교차점(ClosestPoint) 계산 실패");
          continue;
        }
        else
        {
          ubolt.Pos = tempUboltPos;
        }

        bool isConnected = false; // 연결 성공 여부 체크 플래그

        if (ubolt.Remark == "BOX")
        {
          foreach (AMStru stru in nearStruList)
          {
            // [실패 원인 4] 배관과 구조물이 평행하면 연결 불가
            if (ModelHandle.IsParallel(stru, nearTubi))
            {
              System.Diagnostics.Debug.WriteLine($"[RBE 스킵] UBOLT({ubolt.Name}): 구조물({stru.Name})과 배관이 평행하여 연결 안 함");
              continue;
            }

            tempStruPos = ModelHandle.GetNearestPointOnA(stru.OriPoss, stru.OriPose, nearTubi, false);

            if (isInUbolt(stru.Rad, ubolt.Wvol, ModelHandle.GetDistance(ubolt.Pos, tempStruPos)) && ModelHandle.IsPointOnLineBetweenTwoPoints(tempStruPos, stru.OriPoss, stru.OriPose))
            {
              tempStruPos = ModelHandle.GetNearestPointOnA(stru, nearTubi);
              tempPosList = ubolt.InterPos.ToList();
              tempPosList.Add(tempStruPos);
              ubolt.InterPos = tempPosList;
              tempPosList = stru.InterPos.ToList();
              tempPosList.Add(tempStruPos);
              stru.InterPos = tempPosList;
              isConnected = true;
            }
          }
          if (!isConnected)
            System.Diagnostics.Debug.WriteLine($"[RBE 실패] UBOLT({ubolt.Name}): BOX 조건(거리 및 선상 여부)을 만족하는 구조물을 못 찾음");
        }
        else
        {
          double minDist = 10000;
          Point3D minDistStruPos = new Point3D(0, 0, 0);
          int minIdx = 100;

          for (int i = 0; i < nearStruList.Count; i++)
          {
            tempStruPos = ModelHandle.GetClosestPoint(nearStruList[i], nearTubi);
            if (tempStruPos == null || tempStruPos == new Point3D(0, 0, 0))
              continue;

            if (ModelHandle.GetDistance(tempStruPos, tempUboltPos) < minDist)
            {
              minDistStruPos = tempStruPos;
              minDist = ModelHandle.GetDistance(tempStruPos, tempUboltPos);
              minIdx = i;
            }
          }

          // [실패 원인 5] 거리 계산 실패
          if (minDist == 10000 || minIdx == 100)
          {
            System.Diagnostics.Debug.WriteLine($"[RBE 실패] UBOLT({ubolt.Name}): 일반 형태 연결을 위한 최소 거리 구조물 계산 실패");
            continue;
          }

          // 연결 성공 시 처리
          tempPosList = ubolt.InterPos.ToList();
          tempPosList.Add(minDistStruPos);
          ubolt.InterPos = tempPosList;

          tempPosList = nearStruList[minIdx].InterPos.ToList();
          tempPosList.Add(minDistStruPos);
          nearStruList[minIdx].InterPos = tempPosList;

          // 좌표 보정 로직
          if (!ModelHandle.IsPointOnLineBetweenTwoPoints(tempUboltPos, nearTubi.APos, nearTubi.LPos))
          {
            if (ModelHandle.GetDistance(tempUboltPos, nearTubi.APos) <= ModelHandle.GetDistance(tempUboltPos, nearTubi.LPos))
              ubolt.Pos = nearTubi.APos;
            else
              ubolt.Pos = nearTubi.LPos;
          }
        }
      }
    }



    /// <summary>
    /// ubolt volume 중 가장 큰 영역의 절반과 stru의 영역을 더한 것보다 실제 거리가 가까워야 한다. tol = 5
    /// </summary>
    /// <param name="rad"></param>
    /// <param name="wvol"></param>
    /// <param name="dist"></param>
    /// <returns></returns>
    private bool isInUbolt(double rad, List<double> wvol, double dist)
        {
            double max = new List<double> { Math.Abs(wvol[0] - wvol[3]), Math.Abs(wvol[1] - wvol[4]), Math.Abs(wvol[2] - wvol[5]) }.Max();
            if ((max / 2 + rad) + 5 > dist)
                return true;
            else
                return false;
        }
        /// <summary>
        /// U-bolt에 가까운 Stru 찾기
        /// </summary>
        /// <param name="ubolt"></param>
        /// <returns></returns>
        private List<AMStru> GetNearStruToUbolt(AMPipe ubolt)
        {
            List<AMStru> nearStru;
            List<AMStru> nearNearStru;
            // ver.2410 아래 항목 관련 조건 추가(진행방향이 X방향일 경우도 제거)
            // ver.2409 Main Support 하부 ubolt 설치용 평행 Support 제거
            // 조건 1. 대상 부재는 Main Support외 연결지점이 없다 (start/end Connection으로 인한 이동 없음)
            // 조건 2. 진행방향이 Y방향(ver.2410 또는 X방향) 부재로 제한함 (Angle type main support의 하단 ubolt 설치용이므로)
            // 조건 3. 대상 부재와 접한 Main Support 역시 진행방향이 Y방향(ver.2410 또는 x방향)
            // 조건 4. Main Support가 대상 부재를 y방향(ver.2410 또는 x방향)으로 모두 포함하고 있어야 함(교집합일 경우 처리가 힘듦) 
            if (Form1.mainForm.cBoxStrongConnection.Checked)
            {
                // 20250416 psh - Ubolt 인근 stru를 찾을때 tol을 15 -> 5로 줄임. ubolt는 stru와 붙어있어서 괜찮을듯
                nearStru = amModel.StruModels.FindAll(s => isNearStru(ubolt, s, 5));
                // ubolt 인접 stru 1개인 것
                if (nearStru.Count() == 1)
                {
                    // Position 변경이 없다 (eg. 다른부재 연결(start/end connection)이 없다)
                    if (nearStru[0].FinalPoss == nearStru[0].OriPoss && nearStru[0].FinalPose == nearStru[0].OriPose  && string.IsNullOrEmpty(nearStru[0].StartConnection[0]) && string.IsNullOrEmpty(nearStru[0].EndConnection[0]))
                    {
                        if (nearStru[0].Poss.Z != nearStru[0].Pose.Z)
                            return nearStru;
                        // 진행 방향이 Y방향인 것 
                        if (nearStru[0].Poss.X == nearStru[0].Pose.X)
                        {
                            nearNearStru = amModel.StruModels.FindAll(s => isNearStru(nearStru[0], s) && nearStru[0].Name != s.Name);
                            foreach (AMStru nearNear in nearNearStru)
                            {
                                // 인접 부재 진행 방향이 Y방향 인것
                                if (nearNear.Poss.X == nearNear.Pose.X && nearNear.Poss.Z == nearNear.Pose.Z)
                                {
                                    if(nearStru[0].Poss.Y < nearNear.Poss.Y && nearStru[0].Pose.Y < nearNear.Poss.Y && nearStru[0].Poss.Y > nearNear.Pose.Y && nearStru[0].Pose.Y > nearNear.Pose.Y)
                                    {
                                        nearStru[0].Division = "Remove";
                                        nearStru = new List<AMStru> { nearNear };
                                    }
                                    else if (nearStru[0].Poss.Y > nearNear.Poss.Y && nearStru[0].Pose.Y > nearNear.Poss.Y && nearStru[0].Poss.Y < nearNear.Pose.Y && nearStru[0].Pose.Y < nearNear.Pose.Y)
                                    {
                                        nearStru[0].Division = "Remove";
                                        nearStru = new List<AMStru> { nearNear };
                                    }
                                }
                            }
                        }
                        // ver.2410 진행 방향이 X방향인 평행 부재 삭제
                        else if (nearStru[0].Poss.Y == nearStru[0].Pose.Y)
                        {
                            nearNearStru = amModel.StruModels.FindAll(s => isNearStru(nearStru[0], s) && nearStru[0].Name != s.Name);
                            foreach (AMStru nearNear in nearNearStru)
                            {
                                // 인접 부재 진행 방향이 X방향 인것
                                if (nearNear.Poss.Y == nearNear.Pose.Y && nearNear.Poss.Z == nearNear.Pose.Z)
                                {
                                    if (nearStru[0].Poss.X < nearNear.Poss.X && nearStru[0].Pose.X < nearNear.Poss.X && nearStru[0].Poss.X > nearNear.Pose.X && nearStru[0].Pose.X > nearNear.Pose.X)
                                    {
                                        nearStru[0].Division = "Remove";
                                        nearStru = new List<AMStru> { nearNear };
                                    }
                                    else if (nearStru[0].Poss.X > nearNear.Poss.X && nearStru[0].Pose.X > nearNear.Poss.X && nearStru[0].Poss.X < nearNear.Pose.X && nearStru[0].Pose.X < nearNear.Pose.X)
                                    {
                                        nearStru[0].Division = "Remove";
                                        nearStru = new List<AMStru> { nearNear };
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                nearStru = amModel.StruModels.FindAll(s => isNearStru(ubolt, s, 15));
            }
            return nearStru;
        }
        private string GetNearStruToUbolt1(AMPipe ubolt)
        {
            List<AMStru> nearStru;
            List<AMStru> nearNearStru;
            // ver.2409 Main Support 하부 ubolt 설치용 평행 Support 제거
            if (Form1.mainForm.cBoxStrongConnection.Checked)
            {
                nearStru = amModel.StruModels.FindAll(s => isNearStru(ubolt, s, 15));
                if (nearStru.Count() == 1)
                {
                    if (nearStru[0].FinalPoss == nearStru[0].OriPoss && nearStru[0].FinalPose == nearStru[0].OriPose && string.IsNullOrEmpty(nearStru[0].StartConnection[0]) && string.IsNullOrEmpty(nearStru[0].EndConnection[0]))
                    {
                        if (nearStru[0].Poss.X == nearStru[0].Pose.X && nearStru[0].Poss.Z == nearStru[0].Pose.Z)
                        {
                            nearNearStru = amModel.StruModels.FindAll(s => isNearStru(nearStru[0], s) && nearStru[0].Name != s.Name);
                            foreach (AMStru nearNear in nearNearStru)
                            {
                                if (nearNear.Poss.X == nearNear.Pose.X && nearNear.Poss.Z == nearNear.Pose.Z)
                                {
                                    if (nearStru[0].Poss.Y < nearNear.Poss.Y && nearStru[0].Pose.Y < nearNear.Poss.Y && nearStru[0].Poss.Y > nearNear.Pose.Y && nearStru[0].Pose.Y > nearNear.Pose.Y)
                                    {
                                        return nearStru[0].Name;
                                    }
                                    else if (nearStru[0].Poss.Y > nearNear.Poss.Y && nearStru[0].Pose.Y > nearNear.Poss.Y && nearStru[0].Poss.Y < nearNear.Pose.Y && nearStru[0].Pose.Y < nearNear.Pose.Y)
                                    {
                                        return nearStru[0].Name;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }
        //private List<AMStru> GetNearStruToStru(AMStru target)
        //{
        //    List<AMStru> nearStru = amModel.StruModels.FindAll(s => isNearStru(target, s));
        //    return nearStru;
        //}

        /// <summary>
        /// Euipment CSV 로드
        /// </summary>
        /// <param name="filePath"></param>
        private void LoadEquiModelFromCsv(string filePath)
        {
            string csvData;
            using (StreamReader reader = new StreamReader(filePath))
            {
                csvData = reader.ReadToEnd();
            }
            string[] rows = csvData.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows.Skip(1))
            {
                AMEqui equiModel = new AMEqui(row);
                amModel.EquiModels.Add(equiModel);
            }
        }
        /// <summary>
        /// Stru CSV 파일 로드
        /// </summary>
        /// <param name="filePath"></param>
        private void LoadStruModelFromCsv(string filePath)
        {
            string csvData;
            using (StreamReader reader = new StreamReader(filePath))
            {
                csvData = reader.ReadToEnd();
            }
            string[] rows = csvData.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows.Skip(1))
            {
                AMStru struModel = new AMStru(row);
                if ((struModel.Type == "SCTN" || struModel.Type == "GENSEC") && struModel.Poss != struModel.Pose)
                    amModel.StruModels.Add(struModel);
            }
        }

        /// <summary>
        /// 각 STRU의 EndConnection 지정
        /// </summary>
        private void SetConnections()
        {
            AMStru struA;
            AMStru struB;
            bool isEndConnectedA = false;
            bool isEndConnectedB = false;
            double distance;
            // Stru 모델 두개를 순차적으로 비교하면서 연결관계를 설정
            for (int i = 0; i < amModel.StruModels.Count; i++)
            {
                struA = amModel.StruModels[i];
                string StartConnection = string.Empty;
                string EndConnection = string.Empty;
                if (i == amModel.StruModels.Count - 1)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        if (string.IsNullOrEmpty(StartConnection) && !string.IsNullOrEmpty(struA.StartConnection[k]))
                        {
                            StartConnection = struA.StartConnection[k];
                        }
                        if (string.IsNullOrEmpty(EndConnection) && !string.IsNullOrEmpty(struA.EndConnection[k]))
                        {
                            EndConnection = struA.EndConnection[k];
                        }
                    }
                    struA.StartConnection[0] = StartConnection;
                    struA.EndConnection[0] = EndConnection;
                }
                for (int j = i; j < amModel.StruModels.Count; j++)
                {
                    if (i == j)
                        continue;
                    struB = amModel.StruModels[j];
                    
                    //// struA와 struB 모두 Z 평면상에 있어야 한다.
                    //if (Form1.mainForm.cBoxStrongConnection.Checked && struA.OriPoss.Z == struA.OriPose.Z && struB.OriPoss.Z == struB.OriPose.Z)
                    //{
                    //    // 두 부재 Z방향 tolerance를 늘려서 인접 stru 확인
                    //    if (!isNearStruZtol(struA, struB, 15))
                    //        continue;
                    //    distance = ModelHandle.DistanceBetweenTwoCenLines(struA, struB);
                    //    if (distance > struA.Rad2 + struB.Rad2 + 5 + 12)
                    //    {
                    //        continue;
                    //    }
                    //}
                    //else
                    //{

                        // 두 부재의 volume이 떨어져 있으면 제외
                        if (!isNearStru(struA, struB))
                            continue;
                        distance = ModelHandle.DistanceBetweenTwoCenLines(struA, struB);
                        if (distance > struA.Rad2 + struB.Rad2 + 5)
                        {
                            continue;
                        }
                    
                    //}
                        
                    //상호간의 End연결 확인
                    isEndConnectedA = SetAEndConnectionByB(struA, struB);
                    isEndConnectedB = SetAEndConnectionByB(struB, struA);
                    // End연결이 아니라면 Cross 연결
                    if (!isEndConnectedA && !isEndConnectedB)
                    {
                        struA.InterConnections.Add(struB.Name);
                        struB.InterConnections.Add(struA.Name);
                    }
                }
                // 나중에 찾기 쉽도록 최상위 Connection Priority를 0으로 맞춤 
                for (int k = 0; k < 5; k++)
                {
                    if (string.IsNullOrEmpty(StartConnection) && !string.IsNullOrEmpty(struA.StartConnection[k]))
                    {
                        StartConnection = struA.StartConnection[k];
                    }
                    if (string.IsNullOrEmpty(EndConnection) && !string.IsNullOrEmpty(struA.EndConnection[k]))
                    {
                        EndConnection = struA.EndConnection[k];
                    }
                }
                struA.StartConnection[0] = StartConnection;
                struA.EndConnection[0] = EndConnection;
            }
        }
        /// <summary>
        /// A의 끝단에 B가 있는지 확인  
        /// </summary>
        /// <param name="struA"></param>
        /// <param name="struB"></param>
        /// <returns></returns>
        private bool SetAEndConnectionByB(AMStru struA, AMStru struB)
        {
            bool isEndConnected = false;
            bool reverseEndConnected = false;
            if ((ModelHandle.GetDistanceClosestPoint(struB.CenPoss, struA.CenPoss, struA.CenPose) <= struA.Rad + struB.Rad) && ModelHandle.isPosInVol(struB.Poss, struA.Wvol))
                reverseEndConnected = true;
            if ((ModelHandle.GetDistanceClosestPoint(struB.CenPose, struA.CenPoss, struA.CenPose) <= struA.Rad + struB.Rad) && ModelHandle.isPosInVol(struB.Pose, struA.Wvol))
                reverseEndConnected = true;
            // struA의 Start끝단 연결성 찾기
            if (!string.IsNullOrEmpty(struA.StartConnection[0]))
            { }
            // 울어진 volume에서 포함되지 않도록
            else if (ModelHandle.GetDistanceClosestPoint(struA.CenPoss, struB.CenPoss, struB.CenPose) > struA.Rad + struB.Rad)
            { }
            //else if (ModelHandle.GetDistanceClosestPoint(struA.Corners[0], struB.Poss, struB.Pose) >  struB.Rad + 10 && ModelHandle.GetDistanceClosestPoint(struA.Corners[2], struB.Poss, struB.Pose) > struB.Rad + 10 && ModelHandle.GetDistanceClosestPoint(struA.Corners[1], struB.Poss, struB.Pose) > struB.Rad + 10 && ModelHandle.GetDistanceClosestPoint(struA.Corners[3], struB.Poss, struB.Pose) > struB.Rad + 10)
            //{ }
            // near의 중심축 line위에 있을 경우
            //else if (ModelHandle.IsPointOnLineBetweenTwoPoints(struA.Poss, struB.Poss, struB.Pose, 2))
            //{
            //    if (struB.Size.Contains("BEAM"))
            //        struA.StartConnection[0] = struB.Name;
            //    else if (struB.Size.Contains("TUBE"))
            //        struA.StartConnection[1] = struB.Name;
            //    else
            //    {

            //        if (Vector3D.CrossProduct(struA.Pose - struA.Poss, struB.Pose - struB.Poss).LengthSquared < 0.0001)
            //            struA.StartConnection[3] = struB.Name;
            //        else if (ModelHandle.GetDistance(struA.Poss, struB.Poss) < 21 || ModelHandle.GetDistance(struA.Poss, struB.Pose) < 21)
            //            struA.StartConnection[2] = struB.Name;
            //        else
            //            struA.StartConnection[1] = struB.Name;
            //    }
            //    isEndConnected = true;
            //}
            else if (ModelHandle.isPosInVol(struA.Poss, struB.Wvol))
            {
                if (struB.Size.Contains("BEAM"))
                    struA.StartConnection[0] = struB.Name;
                else if (struB.Size.Contains("TUBE"))
                    struA.StartConnection[1] = struB.Name;
                else
                {
                    // parrarell일 경우, priority가 낮음
                    if (Vector3D.CrossProduct(struA.Pose - struA.Poss, struB.Pose - struB.Poss).LengthSquared < 0.0001)
                        struA.StartConnection[4] = struB.Name;
                    else if (ModelHandle.GetDistance(struA.Poss, struB.Poss) < 21 || ModelHandle.GetDistance(struA.Poss, struB.Pose) < 21)
                        struA.StartConnection[2] = struB.Name;
                    else if (reverseEndConnected)
                        struA.StartConnection[3] = struB.Name;
                    else
                        struA.StartConnection[1] = struB.Name;
                }
                isEndConnected = true;
            }
            else if (ModelHandle.isPosInVol(struA.Corners[0], struB.Wvol) || ModelHandle.isPosInVol(struA.Corners[1], struB.Wvol) || ModelHandle.isPosInVol(struA.Corners[2], struB.Wvol) || ModelHandle.isPosInVol(struA.Corners[3], struB.Wvol))
            {
                if (struB.Size.Contains("BEAM"))
                    struA.StartConnection[0] = struB.Name;
                else if (struB.Size.Contains("TUBE"))
                    struA.StartConnection[1] = struB.Name;
                else
                    struA.StartConnection[4] = struB.Name;
                isEndConnected = true;
            }
            // pad 고려한 value
            else if (ModelHandle.isPosInVol(struA.ExtCorners[0], struB.Wvol) || ModelHandle.isPosInVol(struA.ExtCorners[1], struB.Wvol) || ModelHandle.isPosInVol(struA.ExtCorners[2], struB.Wvol) || ModelHandle.isPosInVol(struA.ExtCorners[3], struB.Wvol))
            {
                if (struB.Size.Contains("BEAM"))
                    struA.StartConnection[0] = struB.Name;
                else if (struB.Size.Contains("TUBE"))
                    struA.StartConnection[1] = struB.Name;
                else
                    struA.StartConnection[4] = struB.Name;
                isEndConnected = true;
            }

            // struA의 End끝 연결성 찾기
            if (!string.IsNullOrEmpty(struA.EndConnection[0]))
            { }
            // 기울어진 volume에서 포함되지 않도록
            else if (ModelHandle.GetDistanceClosestPoint(struA.CenPose, struB.CenPoss, struB.CenPose) > struA.Rad + struB.Rad)
            { }
            //else if (ModelHandle.GetDistanceClosestPoint(struA.Corners[4], struB.Poss, struB.Pose) > struB.Rad + 10 && ModelHandle.GetDistanceClosestPoint(struA.Corners[5], struB.Poss, struB.Pose) > struB.Rad + 10 && ModelHandle.GetDistanceClosestPoint(struA.Corners[6], struB.Poss, struB.Pose) > struB.Rad + 10 && ModelHandle.GetDistanceClosestPoint(struA.Corners[7], struB.Poss, struB.Pose) > struB.Rad + 10)
            //{ }
            // near의 중심축 line위에 있을 경우
            //else if (ModelHandle.IsPointOnLineBetweenTwoPoints(struA.Pose, struB.Poss, struB.Pose, 2))
            //{
            //    if (struB.Size.Contains("BEAM"))
            //        struA.EndConnection[0] = struB.Name;
            //    else if (struB.Size.Contains("TUBE"))
            //        struA.EndConnection[1] = struB.Name;
            //    else
            //    {
            //        // parrarell일 경우, priority가 낮음
            //        if (Vector3D.CrossProduct(struA.Pose - struA.Poss, struB.Pose - struB.Poss).LengthSquared < 0.0001)
            //            struA.EndConnection[3] = struB.Name;
            //        else if (ModelHandle.GetDistance(struA.Pose, struB.Poss) < 21 || ModelHandle.GetDistance(struA.Pose, struB.Pose) < 21)
            //            struA.EndConnection[2] = struB.Name;
            //        else
            //            struA.EndConnection[1] = struB.Name;
            //    }
            //    isEndConnected = true;
            //}
            else if (ModelHandle.isPosInVol(struA.Pose, struB.Wvol))
            {
                if (struB.Size.Contains("BEAM"))
                    struA.EndConnection[0] = struB.Name;
                else if (struB.Size.Contains("TUBE"))
                    struA.EndConnection[1] = struB.Name;
                else
                {
                    // parrarell일 경우, priority가 낮음
                    if (Vector3D.CrossProduct(struA.Pose - struA.Poss, struB.Pose - struB.Poss).LengthSquared < 0.0001)
                        struA.EndConnection[4] = struB.Name;
                    else if (ModelHandle.GetDistance(struA.Pose, struB.Poss) < 21 || ModelHandle.GetDistance(struA.Pose, struB.Pose) < 21)
                        struA.EndConnection[2] = struB.Name;
                    else if (reverseEndConnected)
                        struA.EndConnection[3] = struB.Name;
                    else
                        struA.EndConnection[1] = struB.Name;
                }
                isEndConnected = true;
            }
            else if (ModelHandle.isPosInVol(struA.Corners[4], struB.Wvol) || ModelHandle.isPosInVol(struA.Corners[5], struB.Wvol) || ModelHandle.isPosInVol(struA.Corners[6], struB.Wvol) || ModelHandle.isPosInVol(struA.Corners[7], struB.Wvol))
            {
                if (struB.Size.Contains("BEAM"))
                    struA.EndConnection[0] = struB.Name;
                else if (struB.Size.Contains("TUBE"))
                    struA.EndConnection[1] = struB.Name;
                else
                    struA.EndConnection[4] = struB.Name;
                isEndConnected = true;
            }
            // pad 고려한 value
            else if (ModelHandle.isPosInVol(struA.ExtCorners[4], struB.Wvol) || ModelHandle.isPosInVol(struA.ExtCorners[5], struB.Wvol) || ModelHandle.isPosInVol(struA.ExtCorners[6], struB.Wvol) || ModelHandle.isPosInVol(struA.ExtCorners[7], struB.Wvol))
            {
                if (struB.Size.Contains("BEAM"))
                    struA.EndConnection[0] = struB.Name;
                else if (struB.Size.Contains("TUBE"))
                    struA.EndConnection[1] = struB.Name;
                else
                    struA.EndConnection[4] = struB.Name;
                isEndConnected = true;
            }

            return isEndConnected;
        }
        /// <summary>
        /// Stru 중간에 다른 Stru 끝단이 붙는 경우를 찾아 InterConnections 속성에 추가
        /// </summary>
        private void SetInterConnections()
        {
            AMStru tempStru;
            foreach (var stru in amModel.StruModels)
            {
                if (!string.IsNullOrEmpty(stru.StartConnection[0]))
                {
                    tempStru = amModel.StruModels.Find(s => s.Name == stru.StartConnection[0]);
                    if (tempStru.StartConnection[0] != stru.Name && tempStru.EndConnection[0] != stru.Name)
                        tempStru.InterConnections.Add(stru.Name);
                }
                if (!string.IsNullOrEmpty(stru.EndConnection[0]))
                {
                    tempStru = amModel.StruModels.Find(s => s.Name == stru.EndConnection[0]);
                    if (tempStru.StartConnection[0] != stru.Name && tempStru.EndConnection[0] != stru.Name)
                        tempStru.InterConnections.Add(stru.Name);
                }
            }
        }


        /// Stru를 연결점에 맞게 이동
        public void TransformPositions()
        {
            List<AMStru> beams = new List<AMStru>();
            List<AMStru> tempBeams = new List<AMStru>();
            List<AMStru> tempOthers = new List<AMStru>();
            List<AMStru> etc = new List<AMStru>();
            List<AMStru> nears = new List<AMStru>();
            List<AMStru> others = new List<AMStru>();
            double maxRad = 0;

            if (amModel.StruModels.Count() == 0)
                return;
            foreach (AMStru stru in amModel.StruModels)
            {
                if (stru.Size.Contains("BEAM_") || stru.Size.Contains("BSC_"))
                {
                    stru.HasBeenMoved = true;
                    stru.HasBeenChecked = true;
                    beams.Add(stru);
                }
                else
                    others.Add(stru);
            }
            // Beam이 있을 경우, Beam을 기준으로 한다.
            if (beams.Count() > 0)
            {
                maxRad = beams.Max(s => s.Rad);
                tempBeams = beams.FindAll(s => s.Rad == maxRad);
                tempOthers = beams.FindAll(s => s.Rad != maxRad);

                // Beam끼리 먼저 Projection 한다
                ProjectTIntoEachDir(tempBeams.Take(1).ToList(), tempBeams.Skip(1).ToList());

                beams = tempBeams;
                others.AddRange(tempOthers);
                ProjectTIntoEachDir(beams, others);
            }
            // Beam이 없을 경우, 첫번째를 기준으로 한다.
            else
            {
                others = others.OrderByDescending(s => s.Rad).ThenByDescending(s => ModelHandle.GetDistance(s.Poss, s.Pose)).ToList();
                others[0].HasBeenMoved = true;
                others[0].HasBeenChecked = true;
                ProjectTIntoEachDir(others.Take(1).ToList(), others.Skip(1).ToList());
            }
            ExtendToBaseElem();
        }

        /// <summary>
        /// x,y,z 방향별로 기준부재로 인접부재를 Projection
        /// </summary>
        /// <param name="baseElems"></param>
        /// <param name="targets"></param>
        public void ProjectTIntoEachDir(List<AMStru> baseElems, List<AMStru> targets)
        {
            ProjectIntoBaseElement(baseElems, targets, "x");
            targets.ForEach((s) => s.HasBeenMoved = false);
            targets.ForEach((s) => s.HasBeenChecked = false);
            ProjectIntoBaseElement(baseElems, targets, "y");
            targets.ForEach((s) => s.HasBeenMoved = false);
            targets.ForEach((s) => s.HasBeenChecked = false);
            ProjectIntoBaseElement(baseElems, targets, "z");
            targets.ForEach((s) => s.HasBeenMoved = false);
            targets.ForEach((s) => s.HasBeenChecked = false);
        }
        /// <summary>
        /// 기준 부재로 인접 부재로 projection 하고
        /// </summary>
        /// <param name="baseElems"></param>
        /// <param name="targets"></param>
        /// <param name="dir"></param>
        public void ProjectIntoBaseElement(List<AMStru> baseElems, List<AMStru> targets, string dir)
        {
            List<AMStru> nonMovedNears = new List<AMStru>();
            List<AMStru> notChecked;
            double numberOfElems = amModel.StruModels.Count();
            //double numberOfNonMovedElems = amModel.StruModels.FindAll(s => s.HasBeenMoved).Count();
            List<AMStru> temp = amModel.StruModels.FindAll(s => s.HasBeenChecked);
            // 전체 stru 모델중 check된 부재 개수
            double numberOfCheckedElems = amModel.StruModels.FindAll(s => s.HasBeenChecked).Count();
            // 전체 stru 모델이 다 check되면 종료
            if (numberOfElems != numberOfCheckedElems)
            {
                // 기준 모델에 인접한 모델을 조건에 맞으면 이동시키고 이동하지 않은 부재 리턴
                nonMovedNears = MoveAndReturnNonMovedElems(baseElems, targets, dir);
                // 앞의 메서드로 check된 모델 개수가 달라졌는지 확인. 달라 졌으면 이동하지 않은 인접 모델을 기준으로 이동하지 않은 모델을 Projection수행
                if (numberOfCheckedElems != amModel.StruModels.FindAll(s => s.HasBeenChecked).Count())
                {
                    ProjectIntoBaseElement(nonMovedNears, amModel.StruModels.FindAll(s => !s.HasBeenMoved), dir);
                }
                // 앞의 진행에도 개수가 변화없다면, 남은 stru와 더이상 연결되지 않은것.
                // check되지 않은 모델 1개를 선정해 projection 재수행
                else
                {
                    notChecked = amModel.StruModels.FindAll(s => !s.HasBeenChecked);
                    ProjectIntoBaseElement(notChecked.Take(1).ToList(), notChecked.Skip(1).ToList(), dir);
                }
            }
        }
        /// <summary>
        /// 기준 모델과 이동할 모델들을 비교하여 이동 조건이 되면 이동, 이동조건이 되지 않으면 모델들을 리턴.
        /// </summary>
        /// <param name="baseElems"></param>
        /// <param name="targets"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public List<AMStru> MoveAndReturnNonMovedElems(List<AMStru> baseElems, List<AMStru> targets, string dir)
        {
            List<string> nears = new List<string>();
            List<AMStru> movedNears = new List<AMStru>();
            List<AMStru> nonMovedNears = new List<AMStru>();
            AMStru nearElem;
            foreach (AMStru baseElem in baseElems)
            {
                nears = baseElem.InterConnections.ToList();
                if (!string.IsNullOrEmpty(baseElem.StartConnection[0]))
                    nears.Add(baseElem.StartConnection[0]);
                if (!string.IsNullOrEmpty(baseElem.EndConnection[0]))
                    nears.Add(baseElem.EndConnection[0]);
                foreach (var near in nears)
                {
                    nearElem = targets.Find(s => s.Name == near);
                    if (nearElem == null)
                        continue;
                    if (nearElem.HasBeenMoved)
                        continue;
                    nearElem.HasBeenChecked = true;
                    if (IsMoved(baseElem, nearElem, dir))
                        movedNears.Add(nearElem);
                    else
                        nonMovedNears.Add(nearElem);
                }
            }
            nonMovedNears = nonMovedNears.Distinct().ToList();
            movedNears = movedNears.Distinct().ToList();
            // 이동한 부재가 있다면 이동한 부를 기준으로 해당 메서드 실행.
            if (movedNears.Count() > 0)
                nonMovedNears.AddRange(MoveAndReturnNonMovedElems(movedNears, amModel.StruModels.FindAll(s => !s.HasBeenMoved), dir));
            // nonMovedNear이 없으면 base항목도 check한것으로 변경(없으면 무한 루프)
            else
                baseElems.ForEach(s => s.HasBeenChecked = true);

            return nonMovedNears;
        }

        /// <summary>
        /// 이동했는지 확인
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="target"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public bool IsMoved(AMStru reference, AMStru target, string dir)
        {
            //if (target.Name == "=268454733/1413687")
            //{
            //    int a = 1;
            //}
            // x좌표 projection은 두 모델 모두 yz좌표내에 있어야 함. 
            if (dir == "x" && Math.Abs(reference.Poss.X - reference.Pose.X) < 2 && Math.Abs(target.Poss.X - target.Pose.X) < 2 && reference.Poss.X != target.Poss.X)
            {
                target._poss.X = reference.Poss.X;
                target._pose.X = reference.Pose.X;
                target.HasBeenMoved = true;
            }
            else if (dir == "y" && Math.Abs(reference.Poss.Y - reference.Pose.Y) < 2 && Math.Abs(target.Poss.Y - target.Pose.Y) < 2 && reference.Poss.Y != target.Poss.Y)
            {
                target._poss.Y = reference.Poss.Y;
                target._pose.Y = reference.Pose.Y;
                target.HasBeenMoved = true;
            }
            else if (dir == "z" && Math.Abs(reference.Poss.Z - reference.Pose.Z) < 2 && Math.Abs(target.Poss.Z - target.Pose.Z) < 2 && reference.Poss.Z != target.Poss.Z)
            {
                target._poss.Z = reference.Poss.Z;
                target._pose.Z = reference.Pose.Z;
                target.HasBeenMoved = true;
            }

            return target.HasBeenMoved;
        }

        
        /// <summary>
        /// 근처 stru인지 확인
        /// </summary>
        /// <param name="target"></param>
        /// <param name="another"></param>
        /// <returns></returns>
        public bool isNearStru(AMStru target, AMStru another)
        {
            //진행 방향으로만 tol 20 부여(pad 고려), 나머지 5
            double tolX = 5;
            double tolY = 5;
            double tolZ = 5;
            if (target.Wvol[3] + tolX < another.Wvol[0] || another.Wvol[3] + tolX < target.Wvol[0])
                return false;
            else if (target.Wvol[4] + tolY < another.Wvol[1] || another.Wvol[4] + tolY < target.Wvol[1])
                return false;
            else if (target.Wvol[5] + tolZ < another.Wvol[2] || another.Wvol[5] + tolZ < target.Wvol[2])
                return false;
            return true;
        }
        public bool isNearStru(AMStru target, AMStru another, double tol)
        {
            //진행 방향으로만 tol 20 부여(pad 고려), 나머지 5
            double tolX = tol;
            double tolY = tol;
            double tolZ = tol;
            if (target.Wvol[3] + tolX < another.Wvol[0] || another.Wvol[3] + tolX < target.Wvol[0])
                return false;
            else if (target.Wvol[4] + tolY < another.Wvol[1] || another.Wvol[4] + tolY < target.Wvol[1])
                return false;
            else if (target.Wvol[5] + tolZ < another.Wvol[2] || another.Wvol[5] + tolZ < target.Wvol[2])
                return false;
            return true;
        }
        /// <summary>
        /// ver.2409 taget의 -z방향으로 tol내에 있는 stru 찾는 메소드
        /// </summary>
        /// <param name="target"></param>
        /// <param name="another"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public bool isNearStruZtol(AMStru target, AMStru another, double tol)
        {
            //진행 방향으로만 tol 20 부여(pad 고려), 나머지 5
            double tolX = 0;
            double tolY = 0;
            double tolZ = tol;
            if (target.Wvol[3] + tolX < another.Wvol[0] || another.Wvol[3] + tolX < target.Wvol[0])
                return false;
            else if (target.Wvol[4] + tolY < another.Wvol[1] || another.Wvol[4] + tolY < target.Wvol[1])
                return false;
            else if (target.Wvol[5] + tolZ < another.Wvol[2] || another.Wvol[5] + tolZ < target.Wvol[2])
                return false;
            return true;
        }
        public bool isNearStru(AMPipe target, AMStru another, double tol)
        {

            double tolX = tol;
            double tolY = tol;
            double tolZ = tol;
            if (target.Wvol[3] + tolX < another.Wvol[0] || another.Wvol[3] + tolX < target.Wvol[0])
                return false;
            else if (target.Wvol[4] + tolY < another.Wvol[1] || another.Wvol[4] + tolY < target.Wvol[1])
                return false;
            else if (target.Wvol[5] + tolZ < another.Wvol[2] || another.Wvol[5] + tolZ < target.Wvol[2])
                return false;
            return true;
        }


        /// <summary>
        /// 기준 부재로 연장
        /// </summary>
        private void ExtendToBaseElem()
        {
            foreach (AMStru target in amModel.StruModels)
            {
                //if (target.Name == "=268454733/1413675")
                //if (target.Name == "=268454733/1413134")
                //if (target.Name == "=268454733/1413687")
                //{
                //    int a = 1;
                //}
                ExtendEndPoint(target);
            }
        }
        /// <summary>
        /// 기준 부재로 끝단을 연장
        /// </summary>
        /// <param name="target"></param>
        private void ExtendEndPoint(AMStru target)
        {
            AMStru baseElem;
            Vector3D line1;
            Vector3D line2;
            Vector3D p1p2;
            Vector3D normal;
            double s1;
            Point3D intersection;

            line2 = target.Pose - target.Poss;
            bool isSlanted;
            if (Math.Abs(line2.X / line2.Length) > 0.98 || Math.Abs(line2.Y / line2.Length) > 0.98 || Math.Abs(line2.Z / line2.Length) > 0.98)
                isSlanted = false;
            else
                isSlanted = true;
            if (!string.IsNullOrEmpty(target.StartConnection[0]))
            {
                baseElem = amModel.StruModels.Find(s => s.Name == target.StartConnection[0]);
                line1 = baseElem.Pose - baseElem.Poss;

                // 방향이 일치하면 gap 30이내 붙인다
                if (Vector3D.Divide(line1, line1.Length) == Vector3D.Divide(line2, line2.Length))
                {
                    if (ModelHandle.GetDistance(target.Poss, baseElem.Poss) < 30)
                        target.Poss = baseElem.Poss;
                    else if (ModelHandle.GetDistance(target.Poss, baseElem.Pose) < 30)
                        target.Poss = baseElem.Pose;
                }
                else if (isSlanted)
                {
                    target.Poss = ModelHandle.GetClosestPoint(target.Poss, baseElem.Poss, baseElem.Pose);
                    ExtendInterConnections(target);
                }
                else
                {
                    p1p2 = target.Poss - baseElem.Poss;
                    normal = Vector3D.CrossProduct(line1, line2);
                    if (Vector3D.DotProduct(p1p2, normal) < 0.001)
                    {
                        s1 = Vector3D.DotProduct(p1p2, Vector3D.CrossProduct(line2, normal)) / normal.LengthSquared;
                        intersection = baseElem.Poss + (s1 * line1);
                        if (intersection.X > -1)
                        {
                            if (s1 >= 0 && s1 <= 1)
                                target.Poss = intersection;
                            // angle stanchion rail 안붙는거 때문에 해봄
                            else if (ModelHandle.GetDistance(target.Poss,intersection) < 30)
                                target.Poss = intersection;

                        }

                    }
                }
            }
            if (!string.IsNullOrEmpty(target.EndConnection[0]))
            {
                baseElem = amModel.StruModels.Find(s => s.Name == target.EndConnection[0]);
                line1 = baseElem.Pose - baseElem.Poss;

                // 방향이 일치하면 gap 30이내 붙인다
                if (Vector3D.Divide(line1, line1.Length) == Vector3D.Divide(line2, line2.Length))
                {
                    if (ModelHandle.GetDistance(target.Pose, baseElem.Poss) < 30)
                        target.Pose = baseElem.Poss;
                    else if (ModelHandle.GetDistance(target.Pose, baseElem.Pose) < 30)
                        target.Pose = baseElem.Pose;
                }
                else if (isSlanted)
                {
                    target.Pose = ModelHandle.GetClosestPoint(target.Pose, baseElem.Poss, baseElem.Pose);
                    ExtendInterConnections(target);
                }
                else
                {
                    p1p2 = target.Poss - baseElem.Poss;
                    normal = Vector3D.CrossProduct(line1, line2);
                    if (Vector3D.DotProduct(p1p2, normal) < 0.001)
                    {
                        s1 = Vector3D.DotProduct(p1p2, Vector3D.CrossProduct(line2, normal)) / normal.LengthSquared;
                        intersection = baseElem.Poss + (s1 * line1);
                        if (intersection.X > -1)
                        {
                            if (s1 >= 0 && s1 <= 1)
                                target.Pose = intersection;
                            // angle stanchion rail 안붙는거 때문에 해봄
                            else if (ModelHandle.GetDistance(target.Pose, intersection) < 35)
                                target.Pose = intersection;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 앞서 이동한 좌표로 인한 관련 부재의 끝단 이동
        /// </summary>
        /// <param name="baseElem"></param>
        private void ExtendInterConnections(AMStru baseElem)
        {
            AMStru target;
            Vector3D line1;
            Vector3D line2;
            Vector3D p1p2;
            Vector3D normal;

            double s1;
            Point3D intersection;
            foreach (var struStr in baseElem.InterConnections)
            {
                line1 = baseElem.Pose - baseElem.Poss;
                target = amModel.StruModels.Find(s => s.Name == struStr);
                line2 = target.Pose - target.Poss;
                p1p2 = target.Poss - baseElem.Poss;
                normal = Vector3D.CrossProduct(line1, line2);
                if (Vector3D.DotProduct(p1p2, normal) < 0.001)
                {
                    s1 = Vector3D.DotProduct(p1p2, Vector3D.CrossProduct(line2, normal)) / normal.LengthSquared;
                    intersection = baseElem.Poss + (s1 * line1);
                    if (intersection.X > -1)
                    {
                        if (s1 >= 0 && s1 <= 1)
                        {
                            if (ModelHandle.GetDistance(intersection, target.Poss) <= ModelHandle.GetDistance(intersection, target.Pose) && ModelHandle.GetDistance(intersection, target.Poss) < 200)
                                target.Poss = intersection;
                            else if (ModelHandle.GetDistance(intersection, target.Pose) <= ModelHandle.GetDistance(intersection, target.Poss) && ModelHandle.GetDistance(intersection, target.Pose) < 200)
                                target.Pose = intersection;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 좌표 이동된 끝단을 연결부재로 이동하여 최종 위치로 설정
        /// </summary>
        private void AddInterPos()
        {
            AMStru inter;
            Point3D near;
            Vector3D struDir;
            Vector3D interDir;
            foreach (var stru in amModel.StruModels)
            {
                stru.FinalPoss = stru.Poss;
                stru.FinalPose = stru.Pose;
            }
            foreach (var stru in amModel.StruModels)
            {
                
                if (!(stru.InterConnections.Count > 0))
                    continue;
                foreach (var interStr in stru.InterConnections)
                {
                    inter = amModel.StruModels.Find(s => s.Name == interStr);
                    
                    // intersection의 시작점이 target과 만나는 경우, intersection과 target이 만나는 점이 각각 추가된다
                    if (inter.StartConnection[0] == stru.Name)
                    {
                        near = ModelHandle.GetNearestPointOnA(stru, inter, true);
                        if (near != null && near.X > -1)
                        {
                            stru.InterPos.Add(near);
                            inter.FinalPoss = near;
                        }
                    }
                    else if (inter.EndConnection[0] == stru.Name)
                    {
                        near = ModelHandle.GetNearestPointOnA(stru, inter, true);
                        if (near != null && near.X > -1)
                        {
                            stru.InterPos.Add(near);
                            inter.FinalPose = near;
                        }
                    }
                    else
                    {
                        struDir = stru.Pose - stru.Poss;
                        interDir = inter.Pose - inter.Poss;
                        if (struDir.Length > interDir.Length)
                        {
                            near = ModelHandle.GetNearestPointOnA(stru, inter, true);
                            if (near != null && near.X > -1)
                            {
                                stru.InterPos.Add(near);
                                inter.InterPos.Add(near);
                            }
                        }
                    }
                }

            }
        }

        public void testModel(AMStru baseModel, AMStru target, string dir)
        {
            //HHI.COMM.FEA_INTERFACE.Control.CmdHandle.Message(baseModel.Name);
            string testDir = "z";
            string testBaseModel = "=268454633/265141";
            string testTargetModel = "=268454633/265099";
            if (testDir != dir)
                return;
            if (baseModel.Name != testBaseModel)
                return;
            if (target.Name != testTargetModel)
                return;
            string message1 = $"base: {testBaseModel} / moved: {baseModel.HasBeenMoved}";
            string message2 = $"target: {testTargetModel} / moved: {target.HasBeenMoved} / isNear: {isNearStru(baseModel, target)}";

            //HHI.COMM.FEA_INTERFACE.Control.CmdHandle.Message(message1);
            //HHI.COMM.FEA_INTERFACE.Control.CmdHandle.Message(message2);
        }

        public void testGetBaseModel(AMStru baseModel, AMStru target, string dir)
        {
            string testDir = "y";
            string testTargetModel = "=22045/1149845";
            if (testDir != dir)
                return;
            if (target.Name != testTargetModel)
                return;
            //HHI.COMM.FEA_INTERFACE.Control.CmdHandle.Message(baseModel.Name);

        }

    }

}
