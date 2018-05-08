using BaseLibS.Graph;
using BaseLibS.Num.Matrix;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;
using RDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PerseusPluginLib.DESeq2
{
    public class DEAnalysis : IMatrixProcessing
    {
        private MatrixIndexer a;

        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Using DESeq2 to run differential expression analysis. " +
            "Based on the setting and filtering, the significant differential expressed features can be found.";
        public string HelpOutput => "Running differential expression analysis";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Differential analysis";
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

        public void ExtractCountAndSample(string experiments, Dictionary<string, int> samples,
            Dictionary<string, string> counts, bool importCount, string[][] cats, IMatrixData mdata,
            HashSet<string> value, List<string> pair)
        {
            for (int i = 0; i < cats.Length; i++)
            {
                for (int j = 0; j < cats[i].Length; j++)
                {
                    if (importCount == true)
                    {
                        if (samples.ContainsKey(cats[i][j])) samples[cats[i][j]] = samples[cats[i][j]] + 1;
                        else samples.Add(cats[i][j], 1);
                    }
                    if (value.Contains(cats[i][j]))
                    {
                        if (experiments.Length == 0) experiments = mdata.ColumnNames[i];
                        else experiments = experiments + "\t" + mdata.ColumnNames[i];
                        if (!pair.Contains(cats[i][j])) pair.Add(cats[i][j]);
                    }
                }
            }
            if (experiments.Split('\t').Length <= 1)
                MessageBox.Show("Experiments without replicates. It is only used for data exploration, " +
                    "but for generating precisely differential expression, biological replicates are mandatory. ");
            if (importCount == true)
            {
                for (int i = 0; i < mdata.RowCount; i++)
                {
                    for (int j = 0; j < mdata.ColumnCount; j++)
                    {
                        string geneID = "GENE" + i.ToString();
                        if (counts.ContainsKey(geneID)) counts[geneID] = counts[geneID] + "\t" + mdata.Values.Get(i, j).ToString();
                        else counts.Add(geneID, mdata.Values.Get(i, j).ToString());
                    }
                }
            }
        }

        public void CheckSignificant(string[][] sigCol, string[] info, ParameterWithSubParams<bool> fdrValid, 
            ParameterWithSubParams<bool> pValid, ParameterWithSubParams<bool> lfcValid, int lineNum)
        {
            if (fdrValid.Value)
            {
                double maxFDR = fdrValid.Value ? fdrValid.GetSubParameters().GetParam<double>("Max. FDR").Value : 0;
                double.TryParse(info[6], out double fdr);
                double.TryParse(info[2], out double lfc);
                if (fdr <= maxFDR)
                {
                    if (lfc > 0)
                        sigCol[lineNum - 1][0] = "Up-regulated";
                    else if (lfc < 0)
                        sigCol[lineNum - 1][0] = "Down-regulated";
                    else
                        sigCol[lineNum - 1][0] = "Equal";
                }
                else
                    sigCol[lineNum - 1][0] = "Insignificant";
            }
            if (lfcValid.Value)
            {
                double ulfc = lfcValid.Value ? lfcValid.GetSubParameters().GetParam<double>("Up-regluation").Value : 0;
                double dlfc = lfcValid.Value ? lfcValid.GetSubParameters().GetParam<double>("Down-regluation").Value : 0;
                double.TryParse(info[2], out double lfc);
                if (lfc > 0 && lfc < ulfc)
                    sigCol[lineNum - 1][0] = "Insignificant";
                else if (lfc < 0 && lfc > dlfc)
                    sigCol[lineNum - 1][0] = "Insignificant";
            }
            if (pValid.Value)
            {
                double maxP = pValid.Value ? pValid.GetSubParameters().GetParam<double>("Max. p-value").Value : 0;
                double.TryParse(info[5], out double p);
                if (p > maxP)
                    sigCol[lineNum - 1][0] = "Insignificant";
            }
        }

        public void ExtractDESeq2Results(IMatrixData mdata, string pair1, string pair2, 
            ParameterWithSubParams<bool> fdrValid, ParameterWithSubParams<bool> pValid, 
            ParameterWithSubParams<bool> lfcValid)
        {
            StreamReader reader = new StreamReader(File.OpenRead("results.csv"));
            int lineNum = 0;
            string[][] validCol = new string[mdata.Values.RowCount][];
            string[][] sigCol = new string[mdata.Values.RowCount][];
            Dictionary<string, string[]> results = new Dictionary<string, string[]>
                    {
                        { "baseMean", new string[mdata.Values.RowCount] },
                        { "log2FoldChange", new string[mdata.Values.RowCount] },
                        { "lfcSE", new string[mdata.Values.RowCount] },
                        { "stat", new string[mdata.Values.RowCount] },
                        { "p-value", new string[mdata.Values.RowCount] },
                        { "padj", new string[mdata.Values.RowCount] }
                    };
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    line = line.Replace("\"", "");
                    string[] info = line.Split(',');
                    if (lineNum != 0)
                    {
                        validCol[lineNum - 1] = new string[] { "+" };
                        sigCol[lineNum - 1] = new string[] { "Not Valid" };
                        for (int v = 0; v < info.Length; v++)
                        {
                            if (info[v] == "NA")
                            {
                                if (v == 3 || v == 5 || v == 6)
                                    info[v] = "1";
                                else if (v == 2 || v == 4)
                                    info[v] = "0";
                                validCol[lineNum - 1][0] = "-";
                            }
                        }
                        if (validCol[lineNum - 1][0] == "+")
                            CheckSignificant(sigCol, info, fdrValid, pValid, lfcValid, lineNum);
                        results["baseMean"][lineNum - 1] = info[1];
                        results["log2FoldChange"][lineNum - 1] = info[2];
                        results["lfcSE"][lineNum - 1] = info[3];
                        results["stat"][lineNum - 1] = info[4];
                        results["p-value"][lineNum - 1] = info[5];
                        results["padj"][lineNum - 1] = info[6];
                    }
                }
                lineNum++;
            }
            reader.Close();
            foreach (KeyValuePair<string, string[]> entry in results)
            {
                mdata.AddNumericColumn(pair1 + "_vs_" + pair2 + "_" + entry.Key,
                    pair1 + "_vs_" + pair2 + "_" + entry.Key,
                    Array.ConvertAll(entry.Value, Double.Parse));
                double[] t = new double[entry.Value.Length];
                if (entry.Key == "p-value" || entry.Key == "padj")
                {
                    for (int i = 0; i < entry.Value.Length; i++)
                    {
                        double.TryParse(entry.Value[i], out double p);
                        if (p == 0)
                            t[i] = Math.Log10(1 / Double.MaxValue) * -1;
                        else
                            t[i] = Math.Log10(p) * -1;
                    }
                    mdata.AddNumericColumn(pair1 + "_vs_" + pair2 + "_-log10" + entry.Key,
                    pair1 + "_vs_" + pair2 + "_-log10" + entry.Key, t);
                }
            }
            mdata.AddCategoryColumn(pair1 + "_vs_" + pair2 + "_Valid",
                pair1 + "_vs_" + pair2 + "_Valid",
                validCol);
            mdata.AddCategoryColumn(pair1 + "_vs_" + pair2 + "_Significant",
                pair1 + "_vs_" + pair2 + "_Significant",
                sigCol);
        }

        public void RunDESeq2(Dictionary<string, int> samples, List<string> pairs1, List<string> pairs2,
            IMatrixData mdata, REngine engine, ParameterWithSubParams<bool> fdrValid, 
            ParameterWithSubParams<bool> pValid, ParameterWithSubParams<bool> lfcValid)
        {
            engine.Evaluate("library(DESeq2)");
            engine.Evaluate("counts <- read.delim('Count_table.txt')");
            string keys = "", values = "";
            foreach (KeyValuePair<string, int> entry in samples)
            {
                if (keys.Length == 0)
                {
                    keys = "'" + entry.Key + "'";
                    values = entry.Value.ToString();
                }
                else
                {
                    keys = keys + ", " + "'" + entry.Key + "'";
                    values = values + ", " + entry.Value.ToString();
                }
            }
            string commandLine = String.Format("sample<-data.frame(groups = rep(c({0}), time = c({1})))", keys, values);
            engine.Evaluate(commandLine);
            engine.Evaluate("ds<-DESeqDataSetFromMatrix(countData = counts, colData = sample, design = ~groups)");
            engine.Evaluate("colnames(ds)<-colnames(counts)");
            engine.Evaluate("ds<-DESeq(ds)");
            foreach (string pair1 in pairs1)
            {
                foreach (string pair2 in pairs2)
                {
                    if (pair1 != pair2)
                    {
                        commandLine = String.Format("res<-results(ds, c('groups', '{0}', '{1}'))", pair1, pair2);
                        engine.Evaluate(commandLine);
                        engine.Evaluate("write.csv(res, file = 'results.csv')");
                        ExtractDESeq2Results(mdata, pair1, pair2, fdrValid, pValid, lfcValid);
                    }
                }
            }
        }

        public void CheckAndInstallDESeq2(REngine engine)
        {
            engine.Evaluate("is.installed <-function(mypkg) is.element(mypkg, installed.packages()[, 1])").AsFunction();
            engine.Evaluate("write(is.installed('DESeq2'), file='packages')");
            StreamReader reader = new StreamReader(File.OpenRead("packages"));
            string line = reader.ReadLine();
            if (line == "FALSE")
            {
                engine.Evaluate("source('https://bioconductor.org/biocLite.R')");
                engine.Evaluate("biocLite('DESeq2')");
            }
            reader.Close();
        }

        public void DeleteTempFiles(string[] fileNames)
        {
            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName)) File.Delete(fileName);
            }
        }

        public bool CheckUnvaildNum(IMatrixData mdata, ProcessInfo processInfo)
        {
            int zeroAmount = 0;
            for (int i = 0; i < mdata.Values.RowCount; i++)
            {
                bool zeroExist = false;
                for (int j = 0; j < mdata.Values.ColumnCount; j++)
                {
                    if (mdata.Values.Get(i, j) < 0)
                    {
                        processInfo.ErrString = "The counts/hits contain negative values. " +
                            "If the table is correct (even with negative values), please run \"Adjust values\" first.";
                        return true;
                    }
                    else if ((mdata.Values.Get(i, j) % 1) > 0)
                    {
                        processInfo.ErrString = "The counts/hits contain Decimal values. Please run \"Adjust values\" first.";
                        return true;
                    }
                    else if (mdata.Values.Get(i, j) == 0)
                        zeroExist = true;
                }
                if (zeroExist)
                    zeroAmount++;
            }
            if (zeroAmount == mdata.Values.RowCount)
            {
                processInfo.ErrString = "Every gene in this table contain at least one zero count/hit.";
                return true;
            }
            return false;
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

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            string curPath = Directory.GetCurrentDirectory();
            string[] fileNames = new string[] { "packages", "Count_table.txt", "results.csv", "test.txt" };
            ParameterWithSubParams<int> p = param.GetParamWithSubParams<int>("Group");
            ParameterWithSubParams<bool> fdrValid = param.GetParamWithSubParams<bool>("FDR");
            ParameterWithSubParams<bool> pValid = param.GetParamWithSubParams<bool>("P-value");
            ParameterWithSubParams<bool> lfcValid = param.GetParamWithSubParams<bool>("Log2 Fold Change");
            int colInd = p.Value;
            if (colInd < 0)
            {
                processInfo.ErrString = "No categorical rows available.";
                return;
            }
            string[] groupids1 = ExtractGroup(mdata, p, processInfo, "Values1", colInd);
            string[] groupids2 = ExtractGroup(mdata, p, processInfo, "Values2", colInd);
            bool Unvalid = CheckGroupIDsValid(groupids1, groupids2, processInfo);
            if (Unvalid) return;
            HashSet<string> value1 = new HashSet<string>(groupids1);
            HashSet<string> value2 = new HashSet<string>(groupids2);
            TextWriter tw = new StreamWriter("Count_table.txt");
            string[][] cats = mdata.GetCategoryRowAt(colInd);
            string experiments1 = "", experiments2 = "";
            Dictionary<string, int> samples = new Dictionary<string, int>();
            List<string> pairs1 = new List<string>();
            List<string> pairs2 = new List<string>();
            Dictionary<string, string> counts = new Dictionary<string, string>();
            ExtractCountAndSample(experiments1, samples, counts, true,
                cats, mdata, value1, pairs1);
            ExtractCountAndSample(experiments2, samples, counts, false,
                cats, mdata, value2, pairs2);
            string experiments = "";
            foreach (string exp in mdata.ColumnNames)
            {
                if (experiments.Length == 0)
                    experiments = exp;
                else
                    experiments = experiments + "\t" + exp;
            }
            tw.WriteLine(experiments);
            foreach (KeyValuePair<string, string> entry in counts)
            {
                tw.WriteLine(entry.Key + "\t" + entry.Value);
            }
            tw.Close();
            REngine.SetEnvironmentVariables();
            REngine engine = REngine.GetInstance();
            CheckAndInstallDESeq2(engine);
            bool unValid = CheckUnvaildNum(mdata, processInfo);
            if (unValid)
            {
                DeleteTempFiles(fileNames);
                return;
            }
            RunDESeq2(samples, pairs1, pairs2, mdata, engine, fdrValid, pValid, lfcValid);
            DeleteTempFiles(fileNames);
            Directory.SetCurrentDirectory(@curPath);
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
                        new MultiChoiceParam("Values1", sel){
                            Values = values,
                            Help = "The groups that should be present to compute differential expression analysis.",
                        },new MultiChoiceParam("Values2", sel){
                            Values = values,
                            Help = "The groups that should be present to compute differential expression analysis."
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
                }, new BoolWithSubParams("Log2 Fold Change") {
                    Help = "The Log2 Fold Change threshold of the significant features.",
                    Value = true,
                    SubParamsTrue = new Parameters(new DoubleParam("Up-regluation", 1.5),
                        new DoubleParam("Down-regluation", -1.5)),
                    ParamNameWidth = 90,
                    TotalWidth = 731
                }, new BoolWithSubParams("FDR") {
                    Help = "The FDR threshold of the significant features.",
                    Value = true,
                    SubParamsTrue = new Parameters(new DoubleParam("Max. FDR", 0.05)),
                    ParamNameWidth = 90,
                    TotalWidth = 731
                }, new BoolWithSubParams("P-value")
                {
                    Help = "The p-value threshold of the significant features.",
                    Value = false,
                    SubParamsTrue = new Parameters(new DoubleParam("Max. p-value", 0.05)),
                    ParamNameWidth = 90,
                    TotalWidth = 731
                }
                );
        }
    }
}