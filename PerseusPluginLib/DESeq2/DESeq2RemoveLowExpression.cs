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

        public void ExtractValues(IMatrixData mdata, Dictionary<string, List<string>> samples,
            double minValid, Dictionary<string, int> minValidAmount, int type)
        {
            List<int> validRows = new List<int>();
            Dictionary<string, int> validNum = new Dictionary<string, int>();
            foreach (KeyValuePair<string, List<string>> entry in samples)
            {
                validNum.Add(entry.Key, 0);
            }
            for (int i = 0; i < mdata.RowCount; i++)
            {
                int totalValids = 0;
                foreach (KeyValuePair<string, List<string>> entry in samples)
                {
                    validNum[entry.Key] = 0;
                }
                for (int j = 0; j < mdata.ColumnCount; j++)
                {
                    foreach (KeyValuePair<string, List<string>> entry in samples)
                    {
                        if (entry.Value.Contains(mdata.ColumnNames[j]))
                        {
                            if (mdata.Values.Get(i, j) >= minValid)
                            {
                                validNum[entry.Key]++;
                            }
                        }
                    }
                }
                foreach (KeyValuePair<string, int> entry in validNum)
                {
                    if (validNum[entry.Key] >= minValidAmount[entry.Key])
                        totalValids++;
                }
                if ((type == 0) && (totalValids > 0))
                    validRows.Add(i);
                else if ((type == 1) && (totalValids == samples.Count))
                    validRows.Add(i);
            }
            mdata.ExtractRows(validRows.ToArray());
        }

        public bool CheckGroup(ParameterWithSubParams<int> p, ProcessInfo processInfo)
        {
            if (p.Value < 0)
            {
                processInfo.ErrString = "No categorical rows available.";
                return false;
            }
            else
                return true;
        }

        public bool ImportMinAmount(ParameterWithSubParams<int> va, Dictionary<string, List<string>> samples,
            Dictionary<string, int> minValidAmount, ProcessInfo processInfo)
        {
            if (va.Value == 0)
            {
                int minNum = va.GetSubParameters().GetParam<int>("Min. number of samples").Value;
                foreach (KeyValuePair<string, List<string>> entry in samples)
                {
                    if (minNum > entry.Value.Count)
                    {
                        processInfo.ErrString = "Min. number of samples can not be larger than the number of samples.";
                        return false;
                    }
                    else if (minNum <= 0)
                    {
                        processInfo.ErrString = "Min. number of samples can not be negative values or zero.";
                        return false;
                    }
                    minValidAmount.Add(entry.Key, va.GetSubParameters().GetParam<int>("Min. number of samples").Value);
                }
            }
            else
            {
                int minValidPercentage = va.GetSubParameters().GetParam<int>("Min. percentage of samples").Value;
                foreach (KeyValuePair<string, List<string>> entry in samples)
                {
                    if (minValidPercentage > 100)
                    {
                        processInfo.ErrString = "Min. percentage of samples can not be larger than 100.";
                        return false;
                    }
                    else if (minValidPercentage <= 0)
                    {
                        processInfo.ErrString = "Min. percentage of samples can not negative value or zero.";
                        return false;
                    }
                    minValidAmount.Add(entry.Key, entry.Value.Count * minValidPercentage / 100);
                }
            }
            return true;
        }
        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            double minValid = Convert.ToDouble(param.GetParam<string>("Min. expression value").Value);
            Parameter<int> m = param.GetParam<int>("Mode");
            ParameterWithSubParams<int> va = param.GetParamWithSubParams<int>("Min. valid samples in a group");
            if (CheckGroup(va, processInfo) == false)
                return;
            ParameterWithSubParams<int> p = param.GetParamWithSubParams<int>("Group");
            if (CheckGroup(p, processInfo) == false)
                return;
            int colInd = p.Value;
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
            Dictionary<string, int> minValidAmount = new Dictionary<string, int>();
            bool import = ImportMinAmount(va, samples, minValidAmount, processInfo);
            if (import == false)
                return;
            ExtractValues(mdata, samples, minValid, minValidAmount, m.Value);
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
                        Help = "The minimum expression value for filtering the rows of the table. ",
                        Value = "20",
                    }, new SingleChoiceParam("Mode")
                    {
                        Values = new[] { "At least one group", "All groups" },
                        Help =
                            "If 'At least one group' is selected, the row will be kept if at least one selected group can fit the 'Min. expression value.'" +
                            "If 'All groups' is selected, the row will be kept if all selected groups can fit the 'Min. expression value.'.",
                        Value = 0
                    },
                    new SingleChoiceWithSubParams("Min. valid samples in a group")
                    {
                        Help = "The minimum number of values in one row are higher than minimum expression value. ",
                        Values = new[] { "Number", "Percentage" },
                        SubParams = new[]{
                        new Parameters(new IntParam("Min. number of samples", Math.Min(mdata.RowCount, 1)){
                            Help =
                                "If a row has less than the specified number of valid values it will be discarded in the output.",
                        }),
                        new Parameters(new IntParam("Min. percentage of samples", 25){
                            Help =
                                "If a row has less than the specified percentage of valid values it will be discarded in the output."
                        }),
                        },
                        ParamNameWidth = 50,
                        TotalWidth = 731
                    },
                    new SingleChoiceWithSubParams("Group")
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
