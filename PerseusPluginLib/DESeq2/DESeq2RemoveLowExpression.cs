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
using System.Windows.Forms;

namespace PerseusPluginLib.DESeq2
{
    public class RemoveLowExpression : IMatrixProcessing
    {
        private MatrixIndexer a;

        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "For removing the low expressed genomic features based on the values across all samples.";
        public string HelpOutput => "Remove low expressed genomic features";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Remove low expressed genes";
        public string Heading => "DESeq2";
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

        public void RemoveLowValuesInRow(IMatrixData mdata, double[] minValids)
        {
            List<int> validRows = new List<int>();
            for (int i = 0; i < mdata.RowCount; i++)
            {
                bool validNum = false;
                for (int j = 0; j < mdata.ColumnCount; j++)
                {
                    if (mdata.Values.Get(i, j) >= minValids[j])
                    {
                        validNum = true;
                    }
                }
                if (validNum)
                {
                    validRows.Add(i);
                }
            }
            mdata.ExtractRows(validRows.ToArray());
        }

        public string[] ExtractGroup(IMatrixData mdata, ParameterWithSubParams<int> p, ProcessInfo processInfo,
            int colInd)
        {
            string[] errors = new string[0];
            Parameter<int[]> mcp = p.GetSubParameters().GetParam<int[]>("Values");
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

        public bool CheckGroupIDsValid(string[] groupids, ProcessInfo processInfo, int mode)
        {
            if (groupids.Length == 0)
            {
                processInfo.ErrString = "Please select at least one term for analyzing.";
                return true;
            }
            else if (groupids.Length == 1 && mode == 2)
            {
                processInfo.ErrString = "Need at least two groups";
                return true;
            }
            else return false;
        }

        public void ModeAtLeastOneGroup(IMatrixData mdata, Dictionary<string, List<string>> samples,
            double minValid)
        {
            List<int> validRows = new List<int>();
            for (int i = 0; i < mdata.RowCount; i++)
            {
                bool validNum = false;
                for (int j = 0; j < mdata.ColumnCount; j++)
                {
                    foreach (KeyValuePair<string, List<string>> entry in samples)
                    {
                        if (entry.Value.Contains(mdata.ColumnNames[j]))
                            if (mdata.Values.Get(i, j) >= minValid)
                                validNum = true;
                    }
                }
                if (validNum)
                {
                    validRows.Add(i);
                }
            }
            mdata.ExtractRows(validRows.ToArray());
        }

        public void ModeAllGroup(IMatrixData mdata, Dictionary<string, List<string>> samples,
            double minValid)
        {
            List<int> validRows = new List<int>();
            for (int i = 0; i < mdata.RowCount; i++)
            {
                Dictionary<string, int> detectNum = new Dictionary<string, int>();
                for (int j = 0; j < mdata.ColumnCount; j++)
                {
                    string preKey = "";
                    bool validNum = false;
                    foreach (KeyValuePair<string, List<string>> entry in samples)
                    {
                        if (!detectNum.ContainsKey(entry.Key))
                            detectNum.Add(entry.Key, 0);
                        if (entry.Value.Contains(mdata.ColumnNames[j]))
                        {
                            if (preKey == "")
                                preKey = entry.Key;
                            else if (preKey != "" && preKey != entry.Key)
                            {
                                if (validNum)
                                {
                                    detectNum[entry.Key]++;
                                    validNum = false;
                                }
                            }
                            if (mdata.Values.Get(i, j) >= minValid)
                                validNum = true;
                            preKey = entry.Key;
                        }
                    }
                    if (validNum)
                        detectNum[preKey]++;
                }
                bool fail = false;
                foreach (KeyValuePair<string, int> entry in detectNum)
                {
                    if (entry.Value == 0)
                        fail = true;
                }
                if (!fail)
                    validRows.Add(i);
            }
            mdata.ExtractRows(validRows.ToArray());
        }

            public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            double minValid = Convert.ToDouble(param.GetParam<string>("Min. expression value").Value);
            Parameter<int> m = param.GetParam<int>("Mode");
            ParameterWithSubParams<int> p = param.GetParamWithSubParams<int>("Group");
            int colInd = p.Value;
            if (colInd < 0)
            {
                processInfo.ErrString = "No categorical rows available.";
                return;
            }
            string[] groupids = ExtractGroup(mdata, p, processInfo, colInd);
            bool Unvalid = CheckGroupIDsValid(groupids, processInfo, m.Value);
            if (Unvalid) return;
            HashSet<string> value = new HashSet<string>(groupids);
            string[][] cats = mdata.GetCategoryRowAt(colInd);
            Dictionary<string, List<string>> samples = new Dictionary<string, List<string>>();
            for (int i = 0; i < cats.Length; i++)
            {
                for (int j = 0; j < cats[i].Length; j++)
                {
                    if (value.Contains(cats[i][j]))
                    {
                        if (!samples.ContainsKey(cats[i][j]))
                        {
                            samples.Add(cats[i][j], new List<string>());
                        }
                        samples[cats[i][j]].Add(mdata.ColumnNames[i]);
                        int len = i * cats[i].Length + j;
                    }
                }
            }
            if (m.Value == 0)
            {
                ModeAtLeastOneGroup(mdata, samples, minValid);
            }
            else if (m.Value == 1)
            {
                ModeAllGroup(mdata, samples, minValid);
            }
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
                        new MultiChoiceParam("Values", sel){
                            Values = values,
                            Help = "The group that should be performed the filtering of low expression values."
                        }
                    });
            }
            return
                new Parameters(
                    new StringParam("Min. expression value")
                    {
                        Help = "The minimum expression value for filtering the rows of the table. " +
                        "At least one entry of a row needs to be higher than this given value.",
                        Value = "20",
                    }, new SingleChoiceParam("Mode")
                    {
                        Values = new[] { "At least one group", "All groups" },
                        Help =
                            "If 'At least one group' is selected, the row will be kept if at least one selected group can fit the 'Min. expression value.'" +
                            "If 'All groups' is selected, the row will be kept if all selected groups can fit the 'Min. expression value.'.",
                        Value = 0
                    }, new SingleChoiceWithSubParams("Group")
                    {
                        Values = mdata.CategoryRowNames,
                        SubParams = subParams,
                        Help = "The categorical row that the analysis should be based on.",
                        ParamNameWidth = 50,
                        TotalWidth = 731
                    }
                );
        }
    }
}
