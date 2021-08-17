using System;
using System.Collections.Generic;
using BaseLibS.Param;
using System.IO;
using PluginInterop;
using System.Text;
using PluginXVis.Properties; // replace PluginTutorial to your project or solution name
using PerseusApi.Matrix;
using BaseLibS.Graph;
using PerseusApi.Document;
using PerseusApi.Utils;
using PerseusApi.Generic;
using System.Text.RegularExpressions;
using BaseLibS.Util;
using System.Linq;

namespace PluginXVis
{
    public class XVis : IMatrixProcessing
    {
        public bool HasButton => true;
        public Bitmap2 DisplayImage => null;
        public string Description => "Turn MaxQuant crosslinking output tables into input for XVis or XLink Analyzer.";
        public string HelpOutput => "Input compatible with XVis/XLink Analyzer.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "XLink Analyzer Input";
        public string Heading => "Crosslink";
        public bool IsActive => true;
        public float DisplayRank => 100;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;
        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }
        enum colTypes
        {
            Numeric,
            String,
            Category,
            MultiNumeric,
        }
        public string Url
            => "https://xvis.genzentrum.lmu.de/ ";


        public Parameters GetParameters(IMatrixData mdata, ref string errString)
        {
            Dictionary<string, string> requiredFields= new Dictionary<string, string>();
            requiredFields.Add("test", "val");
            Parameter[] normalParams = new Parameter[]
            {
                new LabelParam("Instructions")
                {
                    Value = "Please filter your rows for only valid crosslinks before using this feature.",
                },
                new BoolParam("Overwrite existing file?")
                {
                    Help = "If checked, overwrite the existing file found in the specified directory",
                },
                new FolderParam("Output directory")
                {
                    Help = "Select your output folder",
                },
                new StringParam("File Name", "xvis-input")
                {
                    Help = "A file name. Do not include the file extension (.csv).",
                },
            };
            Parameter[] fieldParams = new Parameter[]
            {
                new StringParam("Proteins1")
                {
                    Value = "Proteins1",
                },
                new StringParam("Proteins2")
                {
                    Value = "Proteins2",
                },
                new StringParam("AbsPos1")
                {
                    Value = "Pro_InterLink1",
                },
                new StringParam("AbsPos2")
                {
                    Value = "Pro_InterLink2",
                },
                new StringParam("ID")
                {
                    Value = "InterLinks",
                },
                new StringParam("Score")
                {
                    Value = "Score",
                }
            };
            Parameters ret = new Parameters(normalParams);
            ret.AddParameterGroup(fieldParams, "Matrix Columns Used", false);
            return ret;
        }

        private void MatrixToCSV(Dictionary<string, string[]> outputCols, string fileName)
        {
            using (StreamWriter writer = new StreamWriter(File.Create(fileName)))
            {
                string sep = ",";
                List<string> colNames = new List<string>(outputCols.Keys);
                writer.WriteLine(StringUtils.Concat(sep, colNames));
                int numRows = outputCols[colNames[0]].Length;
                for (int j = 0; j < numRows; j++)
                {
                    List<string> row = new List<string>();
                    foreach(string colName in colNames)
                    {
                        row.Add(outputCols[colName][j]);
                    }
                    writer.WriteLine(StringUtils.Concat(sep, row));
                }
            }
        }
        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        { 
            if (!Directory.Exists(param.GetParam("Output directory").StringValue)) {
                processInfo.ErrString = "The specified directory could not be found";
            }
            string outDir = param.GetParam("Output directory").StringValue;
            if (!outDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                outDir = outDir + Path.DirectorySeparatorChar;
            }
            string outPath = outDir + param.GetParam("File Name").StringValue + ".csv";
            if (File.Exists(outPath) && !param.GetParam<bool>("Overwrite existing file?").Value)
            {
                processInfo.ErrString = "Found existing file. To overwrite it, check the \"Overwrite\" box.";
                return;
            }
            Dictionary<string, string> colNameMappings = new Dictionary<string, string>();
            string[] colNameParams = { "Proteins1", "Proteins2", "AbsPos1", "AbsPos2", "ID", "Score" };
            foreach (string destColName in colNameParams)
            {
                colNameMappings.Add(param.GetParam(destColName).StringValue, destColName);
            }
            Dictionary<string, string[]> outputCols = new Dictionary<string, string[]>();
            /// <summary>
            /// If the column in question is one of the ones specified, we add them to the 
            /// @code{outputCols} Dictionary. Return false and abandon if an empty string is encountered.
            /// </summary>
            bool ProcessCol<T>(string colName, T[] colData)
            {
                if (colNameMappings.ContainsKey(colName))
                {
                    string[] colOutput = new string[mdata.RowCount];
                    bool processAsNumeric = colNameMappings[colName].Contains("AbsPos");
                    for (int i = 0; i < mdata.RowCount; i++)
                    {
                        if (processAsNumeric)
                        {
                            colOutput[i] = Regex.Replace(colData[i].ToString(), @"[\D]", "");
                        }
                        else
                        {
                            colOutput[i] = colData[i].ToString();
                        }
                        if (String.IsNullOrWhiteSpace(colOutput[i]))
                        {
                            processInfo.ErrString = $"Found empty/whitespce string in {colName}. Please " +
                                $"filter your rows for valid crosslink entries!";
                            return false;
                        }
                    }
                    outputCols.Add(colNameMappings[colName], colOutput);
                }
                return true;
            }
            for (int i = mdata.StringColumnCount - 1; i >=  0; i--)
            {
                string colName = mdata.StringColumnNames[i];
                if (!ProcessCol(colName, mdata.GetStringColumn(colName))) return;
                mdata.RemoveStringColumnAt(i);
            }
            for (int i = mdata.NumericColumnCount - 1; i >= 0; i--)
            {
                string colName = mdata.NumericColumnNames[i];
                if (!ProcessCol(colName, mdata.GetNumericColumn(colName))) return;
                mdata.RemoveNumericColumnAt(i);
            }
            for (int i = mdata.CategoryColumnCount - 1; i >= 0; i--)
            {
                string colName = mdata.CategoryColumnNames[i];
                if (!ProcessCol(colName, mdata.GetCategoryColumn(colName))) return;
                mdata.RemoveCategoryColumnAt(i);
            }
            if (outputCols.Count != colNameParams.Count())
            {
                processInfo.ErrString = $"We were unable to find one of the columns " +
                    $"specified. Please check your spelling and try again.";
                return;
            }
            MatrixToCSV(outputCols, outPath);
            processInfo.ErrString = $"File successfully written to {outPath}."; 
            //NOTE: violation of good programing principles. Thumbs up given by Dr. Sinitcyn
        }
    }
}
