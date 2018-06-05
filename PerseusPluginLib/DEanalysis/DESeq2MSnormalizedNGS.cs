using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Matrix;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PerseusPluginLib.DESeq2
{
    public class MSnormalize : IMatrixProcessing
    {
        private MatrixIndexer a;

        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Normalize MS data based on NGS data for running DESeq2.";
        public string HelpOutput => "Normalize MS data based on NGS data for running DESeq2.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Normalize MS data to NGS scale";
        public string Heading => "DE analysis";
        public bool IsActive => true;
        public float DisplayRank => 100;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public string Url
            => "https://bioconductor.org/packages/release/bioc/html/DESeq2.html";

        public string[] ExtractGroup(IMatrixData mdata, ParameterWithSubParams<int> p, ProcessInfo processInfo,
            string value, int colInd)
        {
            string[] errors = new string[0];
            Parameter<int[]> mcp = p.GetSubParameters().GetParam<int[]>(value);
            int[] inds = mcp.Value;
            if (inds.Length == 0)
            {
                return errors;
            }
            string[] values = new string[inds.Length];
            string[] groupids = new string[inds.Length];
            string[] v = mdata.GetCategoryRowValuesAt(colInd);
            for (int i = 0; i < values.Length; i++)
            {
                groupids[i] = v[inds[i]];
            }
            return groupids;
        }

        public bool CheckGroupIDsValid(string[] groupids1, string[] groupids2, ProcessInfo processInfo)
        {
            if (groupids1.Length == 0 || groupids2.Length == 0)
            {
                processInfo.ErrString = "Please select at least one term for analyzing.";
                return true;
            }
            else if (groupids1.SequenceEqual(groupids2))
            {
                processInfo.ErrString = "Comparing the same groups is unvalid.";
                return true;
            }
            else return false;
        }

        public Tuple<int, int> GetMaxMinofNGS(string[][] cats, IMatrixData mdata,
            HashSet<string> value)
        {
            int MaxNGS = -1;
            int MinNGS = -1;
            for (int i = 0; i < cats.Length; i++)
            {
                for (int j = 0; j < cats[i].Length; j++)
                {
                    if (value.Contains(cats[i][j]))
                    {
                        int CurMax = (int)Math.Round(mdata.Values.GetColumn(i).Max());
                        int CurMin = (int)Math.Round(mdata.Values.GetColumn(i).Min());
                        if (MaxNGS == -1)
                        {
                            MaxNGS = CurMax;
                            MinNGS = CurMin;
                        }
                        else
                        {
                            if (CurMax > MaxNGS)
                                MaxNGS = CurMax;
                            if (CurMin < MinNGS)
                                MinNGS = CurMin;
                        }
                    }
                }
            }
            return Tuple.Create(MaxNGS, MinNGS);
        }

        public void Normalization(string[][] cats, IMatrixData mdata, HashSet<string> value,
            Tuple<int, int> MaxMin)
        {
            for (int i = 0; i < cats.Length; i++)
            {
                for (int j = 0; j < cats[i].Length; j++)
                {
                    if (value.Contains(cats[i][j]))
                    {
                        for (int k = 0; k < mdata.Values.GetColumn(i).Count(); k++)
                        {
                            double CurMax = mdata.Values.GetColumn(i).Max();
                            double CurMin = mdata.Values.GetColumn(i).Min();
                            double std = (mdata.Values.Get(k, i) - CurMin) / (CurMax - CurMin);
                            double scaled = std* (MaxMin.Item1 - MaxMin.Item2) + MaxMin.Item2;
                            mdata.Values.Set(k, i, scaled);
                        }
                    }
                }
            }
        }

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            ParameterWithSubParams<int> p = param.GetParamWithSubParams<int>("Group");
            int colInd = p.Value;
            if (colInd < 0)
            {
                processInfo.ErrString = "No categorical rows available.";
                return;
            }
            string[] groupids1 = ExtractGroup(mdata, p, processInfo, "NGS samples", colInd);
            string[] groupids2 = ExtractGroup(mdata, p, processInfo, "MS samples", colInd);
            bool Unvalid = CheckGroupIDsValid(groupids1, groupids2, processInfo);
            if (Unvalid) return;
            HashSet<string> value1 = new HashSet<string>(groupids1);
            HashSet<string> value2 = new HashSet<string>(groupids2);
            string[][] cats = mdata.GetCategoryRowAt(colInd);
            Tuple<int, int> MaxMin = GetMaxMinofNGS(cats, mdata, value1);
            Normalization(cats, mdata, value2, MaxMin);
        }

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            Parameters[] subParams = new Parameters[mdata.CategoryRowCount];
            for (int i = 0; i < mdata.CategoryRowCount; i++)
            {
                string[] values = mdata.GetCategoryRowValuesAt(i);
                int[] sel = values.Length == 1 ? new[] { 0 } : new int[0];
                subParams[i] =
                    new Parameters(new Parameter[]{
                        new MultiChoiceParam("NGS samples", sel){
                            Values = values,
                            Help = "The groups of NGS data that should be used as reference for normalization.",
                        },new MultiChoiceParam("MS samples", sel){
                            Values = values,
                            Help = "The groups of MS data that should be normalized."
                        }
                    });
            }
            return
                new Parameters(new SingleChoiceWithSubParams("Group")
                {
                    Values = mdata.CategoryRowNames,
                    SubParams = subParams,
                    Help = "The categorical row that the analysis should be based on.",
                    ParamNameWidth = 70,
                    TotalWidth = 731
                });
        }
    }
}
