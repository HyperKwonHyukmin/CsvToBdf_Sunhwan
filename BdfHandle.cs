using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsvToBdf.FEData;

namespace CsvToBdf.Control
{
    public static class BdfHandle
    {
        public static void SaveBdfFile(FEModel feModel, string fileName)
        {
            try
            {
                //string fileName = System.IO.Path.Combine(path, "FEModel.bdf");
                //string fileName = @"C:\temp\AM_FEA\PipeModel.bdf";
                FileStream bdfFile = File.Create(fileName);
                bdfFile.Close();
                StringBuilder bdf = SetBdf(feModel);
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.Write(bdf.ToString());
                }
            }
            catch (Exception ex)
            {
                string esStr = ex.ToString();
            }
        }

        public static StringBuilder SetBdf(FEModel feModel)
        {
            StringBuilder bdf = new StringBuilder();
            bdf.AppendLine("SOL 101");
            bdf.AppendLine("CEND");
            bdf.AppendLine("$");
            bdf.AppendLine("DISPLACEMENT(SORT1, PRINT, PUNCH) = ALL");
            bdf.AppendLine("ELFORCE(SORT1, PRINT, REAL, CORNER) = ALL");
            bdf.AppendLine("SPCFORCES(SORT1, PRINT, PUNCH) = ALL");
            bdf.AppendLine("STRESS(SORT1, PRINT, PUNCH) = ALL");
            bdf.AppendLine("$");
            bdf.AppendLine("SUBCASE       1");
            bdf.AppendLine("LABEL = LC1");
            bdf.AppendLine("SPC = 1");
            bdf.AppendLine("LOAD = 2");
            bdf.AppendLine("ANALYSIS = STATICS");
            bdf.AppendLine("$");
            bdf.AppendLine("BEGIN BULK");
            bdf.AppendLine("PARAM,POST,-1");
            bdf.AppendLine("$");
            feModel.NodeList.ForEach(s => bdf.AppendLine(SetGridToStr(s)));
            feModel.ElemList.ForEach(s => bdf.AppendLine(SetElemToStr(s)));
            SetRbeToStrList(feModel.RbeList).ForEach(s => bdf.AppendLine(s));
            SetMassToStrList(feModel.MassList).ForEach(s => bdf.AppendLine(s));
            SetPropToStrList(feModel.PropList).ForEach(s => bdf.AppendLine(s));
            bdf.AppendLine("MAT1           1206000.0        0.3     7.85E-9                                  ");
            SetMatToStrList(feModel.MatList).ForEach(s => bdf.AppendLine(s));
            SetSPCToStrList(feModel.SPCList).ForEach(s => bdf.AppendLine(s));
            bdf.AppendLine("GRAV*                  2                          9800.0             0.0*");
            bdf.AppendLine("*                    0.0            -1.2                                 ");
            bdf.AppendLine("ENDDATA");


            return bdf;
        }
        public static string SetGridToStr(Node node)
        {
            string str = $"{"GRID",-8}{node.nodeID,8}{null,8}{Math.Round(node.X).ToString("F1"),8}{Math.Round(node.Y).ToString("F1"),8}{Math.Round(node.Z).ToString("F1"),8}";
            return str;
        }
        private static string SetElemToStr(Element elem)
        {
            string elemStr = $"{"CBEAM",-8}{elem.ElemID,8}{elem.PropID,8}{elem.Poss.nodeID,8}{elem.Pose.nodeID,8}{elem.Cood,8}";
            return elemStr;
        }
        private static List<string> SetRbeToStrList(List<Rbe> rbeList)
        {
            List<string> rbeStrList = new List<string>();
            string row1;
            string row2;
            string row3;
            foreach (Rbe rbe in rbeList)
            {
                if (rbe.DepNodes.Count() == 0)
                    continue;
                row1 = $"{"RBE2",-8}{rbe.ElemID,8}{rbe.Pos.nodeID,8}{rbe.Rest,8}";
                if (rbe.DepNodes.Count() < 6)
                {
                    rbe.DepNodes.ForEach(s => row1 += $"{s.nodeID,8}");
                    rbeStrList.Add(row1);
                }
                else if (rbe.DepNodes.Count() < 14)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        row1 += $"{rbe.DepNodes[i].nodeID,8}";
                    }
                    row1 += "+";
                    row2 = $"{"+",-8}";
                    for (int i = 5; i < rbe.DepNodes.Count; i++)
                    {
                        row2 += $"{rbe.DepNodes[i].nodeID,8}";
                    }
                    rbeStrList.Add(row1);
                    rbeStrList.Add(row2);
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        row1 += $"{rbe.DepNodes[i].nodeID,8}";
                    }
                    row1 += "+";
                    row2 = $"{"+",-8}";
                    for (int i = 5; i < 13; i++)
                    {
                        row2 += $"{rbe.DepNodes[i].nodeID,8}";
                    }
                    row2 += "+";
                    row3 = $"{"+",-8}";
                    for (int i = 13; i < rbe.DepNodes.Count; i++)
                    {
                        row3 += $"{rbe.DepNodes[i].nodeID,8}";
                    }
                    rbeStrList.Add(row1);
                    rbeStrList.Add(row2);
                    rbeStrList.Add(row3);
                }
            }
            return rbeStrList;
        }
        private static List<string> SetMassToStrList(List<Mass> massList)
        {
            List<string> massStrList = new List<string>();
            if (massList.Count == 0)
                return massStrList;
            int id = massList.Min(s => s.ElemID);
            List<int> tempNodeNumList = massList.Select(s => s.Pos.nodeID).Distinct().ToList();
            double mass = 0;
            string row = string.Empty;
            foreach (int nodeNum in tempNodeNumList)
            {
                id++;
                massList.FindAll(s => s.Pos.nodeID == nodeNum).ForEach(s => mass += s.massVal);
                mass = mass / 1000;
                row = $"{"CONM2",-8}{id,8}{nodeNum,8}{"0",8}{mass.ToString("F3"),8}{"0.0",8}{"0.0",8}{"0.0",8}";
                massStrList.Add(row);
            }
            return massStrList;
        }
        private static List<string> SetPropToStrList(List<Prop> propList)
        {
            List<string> propStrList = new List<string>();
            string row1;
            string row2;
            foreach (Prop prop in propList)
            {
                row1 = $"{"PBEAML",-8}{prop.PropID,8}{prop.MatID,8}{"MSCBML0",8}{prop.Type,8}                                +";
                row2 = $"{"+",-8}{prop.Dim1,8}{prop.Dim2,8}{prop.Dim3,8}{prop.Dim4,8}";
                propStrList.Add(row1);
                propStrList.Add(row2);
            }
            return propStrList;
        }
        private static List<string> SetMatToStrList(List<Mat> matList)
        {
            List<string> matStrList = new List<string>();
            string row1;
            foreach (Mat mat in matList)
            {
                row1 = $"{"MAT1",-8}{mat.Id,8}{"206000.0",8}{"",8}{"0.3",-8}{(mat.Density / 1000000000000).ToString("E2").Replace("E",""),-8}";
                matStrList.Add(row1);
            }
            return matStrList;
        }
        private static List<string> SetSPCToStrList(List<int> spcList)
        {
            spcList = spcList.Distinct().ToList();
            List<string> spcStrList = new List<string>();
            string row1;
            foreach (int node in spcList)
            {
                row1 = $"{"SPC",-8}{"1",8}{node,8}{"123456",8}{"0.0",8}";
                spcStrList.Add(row1);
            }
            return spcStrList;
        }
    }
}
