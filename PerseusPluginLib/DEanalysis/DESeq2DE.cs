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
        public string Name => "Run DE analysis";
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

        public string ExtractCountAndSample(string experiments, Dictionary<string, List<string>> samples,
            Dictionary<string, string> counts, bool importCount, string[][] cats, IMatrixData mdata,
            HashSet<string> value, List<string> pair)
        {
            for (int i = 0; i < cats.Length; i++)
            {
                for (int j = 0; j < cats[i].Length; j++)
                {
                    if (importCount == true)
                    {
                        if (!samples.ContainsKey(cats[i][j]))
                        {
                            samples.Add(cats[i][j], new List<string>());
                        }
                        samples[cats[i][j]].Add(mdata.ColumnNames[i]);
                    }
                    if (value.Contains(cats[i][j]))
                    {
                        if (!pair.Contains(cats[i][j])) pair.Add(cats[i][j]);
                    }
                }
            }
            if (importCount == true)
            {
                foreach (KeyValuePair<string, List<string>> entry in samples)
                {
                    for (int i = 0; i < mdata.ColumnCount; i++)
                    {
                        if (entry.Value.Contains(mdata.ColumnNames[i]))
                        {
                            if (experiments.Length == 0) experiments = mdata.ColumnNames[i];
                            else experiments = experiments + "\t" + mdata.ColumnNames[i];
                            for (int j = 0; j < mdata.RowCount; j++)
                            {
                                string geneID = j.ToString();
                                if (counts.ContainsKey(geneID)) counts[geneID] = counts[geneID] + "\t" + mdata.Values.Get(j, i).ToString();
                                else counts.Add(geneID, mdata.Values.Get(j, i).ToString());
                            }
                        }
                    }
                }
            }
            return experiments;
        }

        public void CheckSignificant(string[][] sigCol, string FDR_value, string LogFC, string Pvalue,
            ParameterWithSubParams<bool> fdrValid, ParameterWithSubParams<bool> pValid, 
            ParameterWithSubParams<bool> lfcValid, int lineNum)
        {
            if (fdrValid.Value)
            {
                double maxFDR = fdrValid.Value ? fdrValid.GetSubParameters().GetParam<double>("Max. Adjusted p-value (FDR)").Value : 0;
                double.TryParse(FDR_value, out double fdr);
                double.TryParse(LogFC, out double lfc);
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
                double.TryParse(LogFC, out double lfc);
                if (lfc > 0 && lfc < ulfc)
                    sigCol[lineNum - 1][0] = "Insignificant";
                else if (lfc < 0 && lfc > dlfc)
                    sigCol[lineNum - 1][0] = "Insignificant";
                if (!fdrValid.Value)
                {
                    if (lfc >= ulfc)
                        sigCol[lineNum - 1][0] = "Up-regulated";
                    else if (lfc <= dlfc)
                        sigCol[lineNum - 1][0] = "Down-regulated";
                }
            }
            if (pValid.Value)
            {
                double maxP = pValid.Value ? pValid.GetSubParameters().GetParam<double>("Max. p-value").Value : 0;
                double.TryParse(Pvalue, out double p);
                double.TryParse(LogFC, out double lfc);
                if (p > maxP)
                    sigCol[lineNum - 1][0] = "Insignificant";
                if ((!fdrValid.Value) && (!fdrValid.Value))
                {
                    if (p <= maxP)
                    {
                        if (lfc > 0)
                            sigCol[lineNum - 1][0] = "Up-regulated";
                        else if (lfc < 0)
                            sigCol[lineNum - 1][0] = "Down-regulated";
                        else
                            sigCol[lineNum - 1][0] = "Equal";
                    }
                }
            }
        }

        public Dictionary<string, string[]> SetInitDict(IMatrixData mdata, string method)
        {
            if (method == "EdgeR")
            {
                return new Dictionary<string, string[]>
                    {
                        { "LR", new string[mdata.Values.RowCount] },
                        { "log2FoldChange", new string[mdata.Values.RowCount] },
                        { "logCPM", new string[mdata.Values.RowCount] },
                        { "p-value", new string[mdata.Values.RowCount] },
                        { "FDR", new string[mdata.Values.RowCount] }
                    };
            }
            else
            {
                return new Dictionary<string, string[]>
                    {
                        { "baseMean", new string[mdata.Values.RowCount] },
                        { "log2FoldChange", new string[mdata.Values.RowCount] },
                        { "lfcSE", new string[mdata.Values.RowCount] },
                        { "stat", new string[mdata.Values.RowCount] },
                        { "p-value", new string[mdata.Values.RowCount] },
                        { "padj", new string[mdata.Values.RowCount] }
                    };
            }
        }

        public void ImportResult(Dictionary<string, string[]> results, IMatrixData mdata,
            string pair1, string pair2, string[][] validCol, string[][] sigCol, string method,
            bool replicate)
        {
            foreach (KeyValuePair<string, string[]> entry in results)
            {
                if ((entry.Key == "LR") && (!replicate))
                { }
                else
                {
                    mdata.AddNumericColumn(pair1 + "_vs_" + pair2 + "_" + entry.Key,
                        pair1 + "_vs_" + pair2 + "_" + entry.Key,
                        Array.ConvertAll(entry.Value, Double.Parse));
                }
                double[] t = new double[entry.Value.Length];
                if (((entry.Key == "p-value" || entry.Key == "padj") && method == "DESeq2") ||
                    ((entry.Key == "p-value" || entry.Key == "FDR") && method == "EdgeR"))
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
            if (method == "DESeq2")
            {
                mdata.AddCategoryColumn(pair1 + "_vs_" + pair2 + "_Valid",
                    pair1 + "_vs_" + pair2 + "_Valid",
                    validCol);
            }
            mdata.AddCategoryColumn(pair1 + "_vs_" + pair2 + "_Significant",
                pair1 + "_vs_" + pair2 + "_Significant",
                sigCol);
        }
        
        public void StoreResult(Dictionary<string, string[]> results, string[][] sigCol,
            int lineNum, ParameterWithSubParams<bool> fdrValid, ParameterWithSubParams<bool> pValid,
            ParameterWithSubParams<bool> lfcValid, string logFC, string logCPM, string pV, 
            string FDR, string LR)
        {
            sigCol[lineNum - 1] = new string[] { "Insignificant" };
            CheckSignificant(sigCol, FDR, logFC, pV, fdrValid,
                pValid, lfcValid, lineNum);
            results["log2FoldChange"][lineNum - 1] = logFC;
            results["logCPM"][lineNum - 1] = logCPM;
            results["LR"][lineNum - 1] = LR;
            results["p-value"][lineNum - 1] = pV;
            results["FDR"][lineNum - 1] = FDR;
        }

        public void ExtractEdgeRResults(IMatrixData mdata, string pair1, string pair2,
            ParameterWithSubParams<bool> fdrValid, ParameterWithSubParams<bool> pValid,
            ParameterWithSubParams<bool> lfcValid, bool replicate, string resultName)
        {
            StreamReader reader = new StreamReader(File.OpenRead(resultName));
            Dictionary<string, string[]> results = SetInitDict(mdata, "EdgeR");
            int lineNum = 0;
            string[][] sigCol = new string[mdata.Values.RowCount][];
            string[][] validCol = new string[mdata.Values.RowCount][];
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!String.IsNullOrWhiteSpace(line))
                {
                    line = line.Replace("\"", "");
                    string[] info = line.Split(',');
                    if (lineNum != 0)
                    {
                        if (replicate)
                        {
                            StoreResult(results, sigCol, lineNum, fdrValid, pValid, lfcValid,
                                info[1], info[2], info[4], info[5], info[3]);
                        }
                        else
                        {
                            StoreResult(results, sigCol, lineNum, fdrValid, pValid, lfcValid,
                                info[1], info[2], info[3], info[4], "NA");
                        }
                    }
                }
                lineNum++;
            }
            reader.Close();
            ImportResult(results, mdata, pair1, pair2, validCol, sigCol, "EdgeR", replicate);
        }

        public void ExtractDESeq2Results(IMatrixData mdata, string pair1, string pair2, 
            ParameterWithSubParams<bool> fdrValid, ParameterWithSubParams<bool> pValid, 
            ParameterWithSubParams<bool> lfcValid, bool replicate)
        {
            StreamReader reader = new StreamReader(File.OpenRead("results.csv"));
            int lineNum = 0;
            string[][] validCol = new string[mdata.Values.RowCount][];
            string[][] sigCol = new string[mdata.Values.RowCount][];
            Dictionary<string, string[]> results = SetInitDict(mdata, "DESeq2");
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
                            CheckSignificant(sigCol, info[6], info[2], info[5], fdrValid,
                                pValid, lfcValid, lineNum);
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
            ImportResult(results, mdata, pair1, pair2, validCol, sigCol, "DESeq2", replicate);
        }

        public void RunDESeq2(Dictionary<string, List<string>> samples, List<string> pairs1, List<string> pairs2,
            IMatrixData mdata, REngine engine, ParameterWithSubParams<bool> fdrValid, 
            ParameterWithSubParams<bool> pValid, ParameterWithSubParams<bool> lfcValid, string fitType)
        {
            engine.Evaluate("library(DESeq2)");
            bool replicate = true;
            foreach (KeyValuePair<string, List<string>> entry in samples)
            {
                if (entry.Value.Count <= 1)
                {
                    replicate = false;
                }
            }
            engine.Evaluate("counts <- read.delim('Count_table.txt')");
            string keys = "", values = "";
            foreach (KeyValuePair<string, List<string>> entry in samples)
            {
                if (keys.Length == 0)
                {
                    keys = "'" + entry.Key + "'";
                    values = entry.Value.Count.ToString();
                }
                else
                {
                    keys = keys + ", " + "'" + entry.Key + "'";
                    values = values + ", " + entry.Value.Count.ToString(); ;
                }
            }
            string commandLine = String.Format("sample<-data.frame(groups = rep(c({0}), time = c({1})))", keys, values);
            engine.Evaluate(commandLine);
            engine.Evaluate("ds<-DESeqDataSetFromMatrix(countData = counts, colData = sample, design = ~groups)");
            engine.Evaluate("colnames(ds)<-colnames(counts)");
            if (replicate)
            {
                engine.Evaluate("ds<-DESeq(ds)");
            }
            else
            {
                MessageBox.Show("Experiments without replicates. It is only used for data exploration, " +
                        "but for generating precisely differential expression, biological replicates are mandatory. ");
                string repString = String.Format("ds<-DESeq(ds, fitType='{0}')", fitType);
                engine.Evaluate(repString);
            }
            foreach (string pair1 in pairs1)
            {
                foreach (string pair2 in pairs2)
                {
                    if (pair1 != pair2)
                    {
                        commandLine = String.Format("res<-results(ds, c('groups', '{0}', '{1}'))", pair1, pair2);
                        engine.Evaluate(commandLine);
                        engine.Evaluate("write.csv(res, file = 'results.csv')");
                        ExtractDESeq2Results(mdata, pair1, pair2, fdrValid, pValid, lfcValid, replicate);
                    }
                }
            }
        }

        public string ImportContrast(List<string> indexs, string pair1, string pair2)
        {
            string contrast = "";
            foreach (string index in indexs)
            {
                if (contrast.Length == 0)
                {
                    if (index == pair1)
                        contrast = "1";
                    else if (index == pair2)
                        contrast = "-1";
                    else
                        contrast = "0";
                }
                else
                {
                    if (index == pair1)
                        contrast = contrast + ", 1";
                    else if (index == pair2)
                        contrast = contrast + ", -1";
                    else
                        contrast = contrast + ", 0";
                }
            }
            return contrast;
        }

        public void RunEdgeRWithReplicates(REngine engine, Dictionary<string, List<string>> samples,
            List<string> pairs1, List<string> pairs2, IMatrixData mdata, ParameterWithSubParams<bool> fdrValid,
            ParameterWithSubParams<bool> pValid, ParameterWithSubParams<bool> lfcValid)
        {
            engine.Evaluate("data_raw <- read.table('Count_table.txt', header = TRUE)");
            string repString = "";
            foreach (KeyValuePair<string, List<string>> entry in samples)
            {
                if (repString.Length == 0)
                    repString = String.Format("rep('{0}', {1})", entry.Key, entry.Value.Count.ToString());
                else
                    repString = String.Format("{0}, rep('{1}', {2})", repString, entry.Key, entry.Value.Count.ToString());
            }
            string commandLine = String.Format("mobDataGroups <- c({0})", repString);
            engine.Evaluate(commandLine);
            engine.Evaluate("d <- DGEList(counts = data_raw, group = factor(mobDataGroups))");
            engine.Evaluate("d <- calcNormFactors(d)");
            engine.Evaluate("design.mat <- model.matrix(~0 + d$samples$group)");
            engine.Evaluate("colnames(design.mat) <- levels(d$samples$group)");
            engine.Evaluate("write.table(design.mat, file = 'group.txt', sep = '\t', quote=FALSE)");
            engine.Evaluate("d2 <- estimateGLMCommonDisp(d, design.mat)");
            engine.Evaluate("d2 <- estimateGLMTrendedDisp(d2,design.mat)");
            engine.Evaluate("d2 <- estimateGLMTagwiseDisp(d2,design.mat)");
            engine.Evaluate("fit <- glmFit(d2, design.mat)");
            List<string> indexs = new List<string>();
            foreach (string line in File.ReadLines("group.txt"))
            {
                string[] groupIndexs = line.Split('\t');
                foreach (string index in groupIndexs)
                    indexs.Add(index);
                break;
            }
            foreach (string pair1 in pairs1)
            {
                foreach (string pair2 in pairs2)
                {
                    if (pair1 != pair2)
                    {
                        string contrastLine = String.Format("lrt <- glmLRT(fit, contrast = c({0}))",
                            ImportContrast(indexs, pair1, pair2));
                        engine.Evaluate(contrastLine);
                        engine.Evaluate("result <- topTags(lrt, n = Inf)");
                        engine.Evaluate("result_table <- as.data.frame(result)");
                        engine.Evaluate("sort_result <- result_table[order(as.numeric(row.names(result_table))), ]");
                        engine.Evaluate("write.csv(sort_result, file = 'results.csv')");
                        ExtractEdgeRResults(mdata, pair1, pair2, fdrValid, pValid, lfcValid, true, "results.csv");
                    }
                }
            }
        }

        public void WritePairCount(string pair, Dictionary<string, List<string>> samples,
            IMatrixData mdata, Dictionary<string, string> counts, List<string> cats)
        {
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                if (samples[pair].Contains(mdata.ColumnNames[i]))
                {
                    if (!cats.Contains(mdata.ColumnNames[i]))
                        cats.Add(mdata.ColumnNames[i]);
                    for (int j = 0; j < mdata.RowCount; j++)
                    {
                        string geneID = j.ToString();
                        if (counts.ContainsKey(geneID)) counts[geneID] = counts[geneID] + "\t" + mdata.Values.Get(j, i).ToString();
                        else counts.Add(geneID, mdata.Values.Get(j, i).ToString());
                    }
                }
            }
        }

        public void RunEdgeRWithoutReplicates(REngine engine, Dictionary<string, List<string>> samples,
            List<string> pairs1, List<string> pairs2, IMatrixData mdata, ParameterWithSubParams<bool> fdrValid,
            ParameterWithSubParams<bool> pValid, ParameterWithSubParams<bool> lfcValid, double dispersion)
        {
            foreach (string pair1 in pairs1)
            {
                foreach (string pair2 in pairs2)
                {
                    if (pair1 != pair2)
                    {
                        Dictionary<string, string> counts = new Dictionary<string, string>();
                        List<string> cats = new List<string>();
                        string fileName = "Count_table_" + pair1 + "_" + pair2 + ".txt";
                        TextWriter tw = new StreamWriter(fileName);
                        WritePairCount(pair1, samples, mdata, counts, cats);
                        WritePairCount(pair2, samples, mdata, counts, cats);
                        tw.WriteLine(String.Join("\t", cats));
                        foreach (KeyValuePair<string, string> entry in counts)
                        {
                            tw.WriteLine(entry.Key + "\t" + entry.Value);
                        }
                        tw.Close();
                        string fileLine = String.Format("data_raw <- read.table('{0}', header = TRUE)", fileName);
                        engine.Evaluate(fileLine);
                        string repString = String.Format("rep('{0}', {1}), rep('{2}', {3})", 
                            pair1, samples[pair1].Count.ToString(), pair2, samples[pair2].Count.ToString());
                        string commandLine = String.Format("mobDataGroups <- c({0})", repString);
                        engine.Evaluate(commandLine);
                        engine.Evaluate("d <- DGEList(counts = data_raw, group = factor(mobDataGroups))");
                        engine.Evaluate("d <- calcNormFactors(d)");
                        string dispersionLine = String.Format("d$common.dispersion <- {0}", dispersion.ToString());
                        engine.Evaluate(dispersionLine);
                        engine.Evaluate("de <- exactTest(d)");
                        engine.Evaluate("result <- topTags(de, n = Inf)");
                        engine.Evaluate("result_table <- as.data.frame(result)");
                        engine.Evaluate("sort_result <- result_table[order(as.numeric(row.names(result_table))), ]");
                        string resultName = "results_" + pair1 + "_" + pair2 + ".csv";
                        string resultString = String.Format("write.csv(sort_result, file = '{0}')", resultName);
                        engine.Evaluate(resultString);
                        ExtractEdgeRResults(mdata, pair1, pair2, fdrValid, pValid, lfcValid, false, resultName);
                    }
                }
            }
        }

        public void RunEdgeR(Dictionary<string, List<string>> samples, List<string> pairs1, List<string> pairs2,
            IMatrixData mdata, REngine engine, ParameterWithSubParams<bool> fdrValid,
            ParameterWithSubParams<bool> pValid, ParameterWithSubParams<bool> lfcValid, 
            double dispersion)
        {
            engine.Evaluate("library(edgeR)");
            bool replicate = true;
            foreach (KeyValuePair<string, List<string>> entry in samples)
            {
                if (entry.Value.Count <= 1)
                    replicate = false;
            }
            if (replicate)
            {
                RunEdgeRWithReplicates(engine, samples, pairs1, pairs2, mdata, fdrValid,
                    pValid, lfcValid);
            }
            else
            {
                MessageBox.Show("Experiments without replicates. It is only used for data exploration, " +
                        "but for generating precisely differential expression, biological replicates are mandatory. ");
                RunEdgeRWithoutReplicates(engine, samples, pairs1, pairs2, mdata, fdrValid,
                    pValid, lfcValid, dispersion);
            }
        }

        public void CheckAndInstallation(REngine engine, int program)
        {
            engine.Evaluate("is.installed <-function(mypkg) is.element(mypkg, installed.packages()[, 1])").AsFunction();
            if (program == 0)
                engine.Evaluate("write(is.installed('DESeq2'), file='packages')");
            else
                engine.Evaluate("write(is.installed('edgeR'), file='packages')");
            StreamReader reader = new StreamReader(File.OpenRead("packages"));
            string line = reader.ReadLine();
            if ((line == "FALSE") && (program == 0))
            {
                engine.Evaluate("source('https://bioconductor.org/biocLite.R')");
                engine.Evaluate("biocLite('DESeq2')");
            }
            else if ((line == "FALSE") && (program == 1))
            {
                engine.Evaluate("source('https://bioconductor.org/biocLite.R')");
                engine.Evaluate("biocLite('edgeR', dependencies = TRUE)");
            }
            reader.Close();
        }

        public void DeleteTempFiles(string[] fileNames, string curPath, string[] multiFileNames)
        {
            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName)) File.Delete(fileName);
            }
            foreach (string mfileName in multiFileNames)
            {
                foreach (string f in Directory.EnumerateFiles(curPath, mfileName))
                {
                    File.Delete(f);
                }
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
            ParameterWithSubParams<int> programInd = param.GetParamWithSubParams<int>("Program");
            int program = programInd.Value;
            string[] fileNames = new string[] { "packages", "group.txt", "results.csv", "test.txt", "Count_table.txt" };
            string[] multiFileNames = new string[] { "Count_table*.txt", "results*.csv" };
            ParameterWithSubParams<int> p = param.GetParamWithSubParams<int>("Group");
            ParameterWithSubParams<bool> fdrValid = param.GetParamWithSubParams<bool>("Adjusted p-value (FDR)");
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
            Dictionary<string, List<string>> samples = new Dictionary<string, List<string>>();
            List<string> pairs1 = new List<string>();
            List<string> pairs2 = new List<string>();
            string experiments = "";
            Dictionary<string, string> counts = new Dictionary<string, string>();
            experiments = ExtractCountAndSample(experiments, samples, counts, true,
                cats, mdata, value1, pairs1);
            experiments = ExtractCountAndSample(experiments, samples, counts, false,
                cats, mdata, value2, pairs2);
            tw.WriteLine(experiments);
            foreach (KeyValuePair<string, string> entry in counts)
            {
                tw.WriteLine(entry.Key + "\t" + entry.Value);
            }
            tw.Close();
            REngine.SetEnvironmentVariables();
            REngine engine = REngine.GetInstance();
            CheckAndInstallation(engine, program);
            bool unValid = CheckUnvaildNum(mdata, processInfo);
            if (unValid)
            {
                DeleteTempFiles(fileNames, curPath, multiFileNames);
                return;
            }
            if (program == 0)
            {
                int fitTypeInd = programInd.GetSubParameters().GetParam<int>("FitType").Value;
                string fitType = "parametric";
                if (fitTypeInd == 1)
                    fitType = "local";
                else if (fitTypeInd == 2)
                    fitType = "mean";
                RunDESeq2(samples, pairs1, pairs2, mdata, engine, fdrValid, pValid, lfcValid, fitType);
            }
            else
            {
                double dispersion = programInd.GetSubParameters().GetParam<double>("Dispersion (without replicates)").Value;
                RunEdgeR(samples, pairs1, pairs2, mdata, engine, fdrValid, pValid, lfcValid, dispersion);
            }
            DeleteTempFiles(fileNames, curPath, multiFileNames);
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
                new Parameters(new SingleChoiceWithSubParams("Program")
                {
                    Values = new[] { "DESeq2", "EdgeR" },
                    SubParams = new[]{
                        new Parameters(new Parameter[] { new SingleChoiceParam("FitType"){
                            Values = new[] { "parametric", "local", "mean" },
                            Help = "This fitType for running DESeq2. If your dataset without replicates, please use local or mean.",
                            } }),
                        new Parameters(new Parameter[]{ new DoubleParam("Dispersion (without replicates)", 0.04){
                            Help = "This dispersion value of EdgeR for the dataset without replicates." } })
                    },
                    Help = "The program for doing differential expression analysis.",
                    ParamNameWidth = 170,
                    TotalWidth = 731
                }, new SingleChoiceWithSubParams("Group")
                {
                    Values = mdata.CategoryRowNames,
                    SubParams = subParams,
                    Help = "The categorical row that the analysis should be based on.",
                    ParamNameWidth = 70,
                    TotalWidth = 731
                }, new BoolWithSubParams("Log2 Fold Change") {
                    Help = "The Log2 Fold Change threshold of the significant features.",
                    Value = true,
                    SubParamsTrue = new Parameters(new DoubleParam("Up-regluation", 2),
                        new DoubleParam("Down-regluation", -2)),
                    ParamNameWidth = 90,
                    TotalWidth = 731
                }, new BoolWithSubParams("Adjusted p-value (FDR)") {
                    Help = "The Adjusted p-value (FDR) threshold of the significant features.",
                    Value = true,
                    SubParamsTrue = new Parameters(new DoubleParam("Max. Adjusted p-value (FDR)", 0.05)),
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