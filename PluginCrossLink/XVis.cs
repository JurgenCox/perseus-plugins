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

/// <summary>
/// Generate input for XVis, by LMU. Also contain the option of generating the necessary fields for 
/// XLinkAnalyzer. This class more or less rearranges the columns and (in the case the the Id field)
/// combines them and puts them in a different name. 
/// </summary>
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
        public string Heading => "Cross link";
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
                    Value = "Please filter your rows for only valid cross links before using this feature.",
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
            Parameter[] fieldParams = {
                new StringParam("Proteins1", "Proteins1"),
                new StringParam("Proteins2", "Proteins2"),
                new StringParam("AbsPos1", "Pro_InterLink1"),
                new StringParam("AbsPos2", "Pro_InterLink2"),
                new StringParam("Score", "Score"),
            };
            Parameter[] idParams = {
                new BoolParam("Generate ID Field for XLinkAnalyzer Specs?", true),
                new StringParam("Peptide1", "Sequence1"),
                new StringParam("Peptide2", "Sequence2"),
                new StringParam("Relative Position 1", "Pep_InterLink1"),
                new StringParam("Relative Position 2", "Pep_InterLink2"),
            };
            Parameters ret = new Parameters(normalParams);
            ret.AddParameterGroup(fieldParams, "Matrix Columns Used", false);
            ret.AddParameterGroup(idParams, "Columns Used To Generate XLinkAnalyzer ID field", false);
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
        private string[] CreateIdCol(Dictionary<string, string[]> cols, int numRows)
        {
            string[] ret = new string[numRows];
            for (int i = 0; i < numRows; i++)
            {
                ret[i] = cols["Peptide1"][i] + '-' + cols["Peptide2"][i] + "-a" + cols["Relative Position 1"][i] + "-b" + cols["Relative Position 2"][i];
            }
            return ret;
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

            // Map the old col names to the new ones, incude idColNameParams only if specified
            Dictionary<string, string> colNameMappings = new Dictionary<string, string>();
            string[] colNameParams = { "Proteins1", "Proteins2", "AbsPos1", "AbsPos2", "Score" };
            string[] idColNameParams = { "Peptide1", "Peptide2", "Relative Position 1", "Relative Position 2" };
            foreach (string destColName in colNameParams)
            {
                colNameMappings.Add(param.GetParam(destColName).StringValue, destColName);
            }
            if (param.GetParam<bool>("Generate ID Field for XLinkAnalyzer Specs?").Value)
            {
                // Process each relative ordering column as usual, and combine at the end into one col
                foreach (string destColName in idColNameParams)
                {
                    colNameMappings.Add(param.GetParam(destColName).StringValue, destColName);
                }
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
                    string outCol = colNameMappings[colName];
                    string[] colOutput = new string[mdata.RowCount];
                    bool processAsNumeric = outCol.Contains("AbsPos") || outCol.Contains("Relative Position");
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
                        if (colOutput[i].Contains("REV_"))
                        {
                            processInfo.ErrString = $"Found REV_ in {colName}. This is a decoy and not a valid Crosslink! " +
                                $"Please filter your rows for valid crosslink entries!";
                            return false;
                        }
                    }
                    outputCols.Add(outCol, colOutput);
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
            if (outputCols.Count != colNameMappings.Count())
            {
                processInfo.ErrString = $"We were unable to find one of the columns " +
                    $"specified. Please check your spelling and try again.";
                return;
            }
            if (param.GetParam<bool>("Generate ID Field for XLinkAnalyzer Specs?").Value)
            {
                string[] idCol = CreateIdCol(outputCols, mdata.RowCount);
                outputCols.Add("Id", idCol);
                foreach (string usedIdCol in idColNameParams)
                {
                    outputCols.Remove(usedIdCol);
                }
            }
            MatrixToCSV(outputCols, outPath);
            processInfo.ErrString = $"File successfully written to {outPath}.";
            //NOTE: violation of good programing principles. Using ErrString to deliver message when not an error.
        }
    }
}
