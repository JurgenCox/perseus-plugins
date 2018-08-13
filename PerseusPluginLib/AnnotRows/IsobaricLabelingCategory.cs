using System;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Vector;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace PerseusPluginLib.AnnotRows
{
    public class IsobaricLabelingRename : IMatrixProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Annotate the column names by the isobaric labeling profiles.";
        public string HelpOutput => "The column names will be grouped by the information of the isobaric labeling profiles.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Isobaric labeling from profile";
        public string Heading => "Annot. rows";
        public bool IsActive => true;
        public float DisplayRank => 6;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;
        public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange";

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public string GetDelimiter(Parameters param)
        {
            string delimiter = "NA";
            ParameterWithSubParams<int> parmInd = param.GetParamWithSubParams<int>("Delimiter of the profile");
            int delimiterInd = parmInd.Value;
            if (delimiterInd == 0)
            {
                delimiter = "\t";
            }
            else if (delimiterInd == 1)
            {
                delimiter = " ";
            }
            else if (delimiterInd == 2)
            {
                delimiter = ",";
            }
            else if (delimiterInd == 3)
            {
                delimiter = ";";
            }
            else
            {
                delimiter = parmInd.GetSubParameters().GetParam<string>("Keyin the delimiter").Value;
            }
            return delimiter;
        }

        public bool setColumnDict(Dictionary<string, int> requireTitles,
            string[] exps, ProcessInfo processInfo, List<string> uniColumnNames)
        {
            bool repeat = false;
            int colNum = 0;
            foreach (string exp in exps)
            {
                if (!uniColumnNames.Contains(exp))
                {
                    if ((exp.ToLower() == "experiments") || (exp.ToLower() == "experiment"))
                    {
                        requireTitles.Add("experiments", colNum);
                    }
                    else if ((exp.ToLower() == "channel") || (exp.ToLower() == "channels"))
                    {
                        requireTitles.Add("channels", colNum);
                    }
                    else if ((exp.ToLower() == "name") || (exp.ToLower() == "names"))
                    {
                        requireTitles.Add("names", colNum);
                    }
                    uniColumnNames.Add(exp);
                }
                else
                {
                    processInfo.ErrString = "Repeated column names are found.";
                    repeat = true;
                }
                colNum++;
            }
            if ((!requireTitles.ContainsKey("experiments")) || (!requireTitles.ContainsKey("channels")))
            {
                processInfo.ErrString = "The columns of \"Experiments\" or \"Channels\" are not found.";
                repeat = true;
            }
            return repeat;
        }

        public bool ImportData(Dictionary<string, List<string[]>> samples, string[] exps, int expIndex,
            int channelIndex, ProcessInfo processInfo, bool repeat, Dictionary<string, int> requireTitles,
            Dictionary<string, int> repeatNames)
        {
            if (!samples.ContainsKey(exps[expIndex]))
            {
                samples.Add(exps[expIndex], new List<string[]>());
            }
            else
            {
                int sampleNum = 0;
                foreach (string[] info in samples[exps[expIndex]])
                {
                    if (info[channelIndex] == exps[channelIndex])
                    {
                        processInfo.ErrString = "Repeated channal indices are found in the same experiments.";
                        repeat = true;
                    }
                    if (requireTitles.ContainsKey("names"))
                    {
                        int nameIndex = requireTitles["names"];
                        if (info[nameIndex] == exps[nameIndex])
                        {
                            if (!repeatNames.ContainsKey(exps[nameIndex]))
                            {
                                repeatNames.Add(exps[nameIndex], 2);
                                samples[exps[expIndex]][sampleNum][nameIndex] = exps[nameIndex] + "_1";
                                exps[nameIndex] = exps[nameIndex] + "_2";
                            }
                            else
                            {
                                repeatNames[exps[nameIndex]]++;
                                exps[nameIndex] = exps[nameIndex] + "_" + repeatNames[exps[nameIndex]];
                            }
                        }
                    }
                    sampleNum++;
                }
            }
            samples[exps[0]].Add(exps);
            return repeat;
        }

        public bool ReadProfile(string filename, Parameters param,
            Dictionary<string, List<string[]>> samples, ProcessInfo processInfo,
            List<string> uniColumnNames, Dictionary<string, int> requireTitles)
        {
            bool repeat = false;
            bool start = true;
            Dictionary<string, int> repeatNames = new Dictionary<string, int>();
            using (var reader = new StreamReader(@filename))
            {
                string delimiter = GetDelimiter(param);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] exps = line.Split(new string[] { delimiter }, StringSplitOptions.None);
                    if (start)
                    {
                        repeat = setColumnDict(requireTitles, exps, processInfo, uniColumnNames);
                        start = false;
                    }
                    else
                    {
                        int channelIndex = requireTitles["channels"];
                        int expIndex = requireTitles["experiments"];
                        int.TryParse(exps[channelIndex], out int channelInd);
                        if ((channelInd == 0) || (channelInd <= 0))
                        {
                            processInfo.ErrString = "Channal indices should be larger than 1.";
                            repeat = true;
                        }
                        else
                        {
                            repeat = ImportData(samples, exps, expIndex, channelIndex, processInfo,
                                repeat, requireTitles, repeatNames);
                        }
                    }
                }
            }
            return repeat;
        }

        public void CheckChannelAndAssignValue(IMatrixData mdata,
            KeyValuePair<string, List<string[]>> entry, int channelIndex,
            string[] mainColInfoName, Dictionary<string, int> requireTitles,
            int renameType, int mainColChannl, List<string[][]> catRows, int k)
        {
            foreach (string[] sampleData in entry.Value)
            {
                int.TryParse(sampleData[channelIndex], out int channelInd);
                if (mainColChannl == channelInd)
                {
                    for (int j = 0; j < sampleData.Length; j++)
                    {
                        if ((requireTitles.ContainsKey("names")) && (j == requireTitles["names"]))
                        {
                            if (renameType == 0)
                                catRows[j][k][0] = String.Join(" ", mainColInfoName) + " " + sampleData[j];
                            else if (renameType == 1)
                                catRows[j][k][0] = mdata.ColumnNames[k] + " " + sampleData[j];
                            else
                                catRows[j][k][0] = sampleData[j];
                        }
                        else
                            catRows[j][k][0] = sampleData[j];
                    }
                }
            }
        }

        public void AssignCategory(IMatrixData mdata, Dictionary<string, List<string[]>> samples,
            List<string[][]> catRows, Dictionary<string, int> requireTitles, int renameType)
        {
            int channelIndex = requireTitles["channels"];
            int expIndex = requireTitles["experiments"];
            for (int k = 0; k < mdata.ColumnCount; k++)
            {
                foreach (KeyValuePair<string, List<string[]>> entry in samples)
                {
                    int expNameSplit = entry.Key.Split(' ').Length;
                    string[] mainColInfo = mdata.ColumnNames[k].Split(' ');
                    if (mainColInfo.Length > expNameSplit)
                    {
                        string[] checkExpName = new string[expNameSplit];
                        Array.Copy(mainColInfo, (mainColInfo.Length - expNameSplit), checkExpName, 0, expNameSplit);
                        string[] mainColInfoName = new string[mainColInfo.Length - expNameSplit - 1];
                        Array.Copy(mainColInfo, 0, mainColInfoName, 0, mainColInfo.Length - expNameSplit - 1);
                        int.TryParse(mainColInfo[mainColInfo.Length - expNameSplit - 1], out int mainColChannl);
                        if ((entry.Key == String.Join(" ", checkExpName)) &&
                            (mainColChannl != 0)) // (mainColInfo[mainColInfo.Length - expNameSplit - 1] == "0")
                        {
                            CheckChannelAndAssignValue(mdata, entry, channelIndex, mainColInfoName, requireTitles,
                                 renameType, mainColChannl, catRows, k);
                        }
                    }
                }
            }
        }

        public void CheckValidCat(List<string[][]> catRows)
        {
            bool valid = false;
            foreach (string[][] catRow in catRows)
            {
                foreach (string[] catInfo in catRow)
                {
                    foreach (string cat in catInfo)
                        if (cat != "")
                        {
                            valid = true;
                        }
                }
            }
            if (!valid)
            {
                MessageBox.Show("The format of some input columns may be not correct. " +
                    "The format should be \"SomeInformation ChannelIndex ExperimentName\". For " +
                    "example, Reporter intensity 6 Mouse brain Tem Cortex - 6 is the channel index " +
                    "and Mouse brain Tem Cortex is the experiment name.");
            }
        }

        public void CheckRepeat(string[][] catRow)
        {
            List<string> names = new List<string>();
            Dictionary<string, int> repeats = new Dictionary<string, int>();
            int numRow = 0;
            foreach (string[] catInfo in catRow)
            {
                if (catInfo[0] != "")
                {
                    if (!names.Contains(catInfo[0]))
                    {
                        names.Add(catInfo[0]);
                    }
                    else
                    {
                        if (!repeats.ContainsKey(catInfo[0]))
                        {
                            repeats.Add(catInfo[0], 2);
                            for (int i = 0; i < names.Count; i++)
                            {
                                if (names[i] == catInfo[0])
                                {
                                    catRow[i][0] = catInfo[0] + "_1";
                                }
                            }
                        }
                        else
                            repeats[catInfo[0]]++;
                        catRow[numRow][0] = catInfo[0] + "_" + repeats[catInfo[0]];
                    }
                }
                else
                {
                    names.Add(catInfo[0]);
                }
                numRow++;
            }
        }

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            List<string> uniColumnNames = new List<string>();
            Dictionary<string, int> requireTitles = new Dictionary<string, int>();
            string filename = param.GetParam<string>("Isobaric labeling profile").Value;
            int renameType = param.GetParam<int>("The type for renaming main columns").Value;
            if (filename.Length == 0)
            {
                processInfo.ErrString = "Input file is empty.";
                return;
            }
            if (!File.Exists(filename))
            {
                processInfo.ErrString = "Input file does not exist.";
                return;
            }
            Dictionary<string, List<string[]>> samples = new Dictionary<string, List<string[]>>();
            bool repeat = ReadProfile(filename, param, samples, processInfo, uniColumnNames, requireTitles);
            if (repeat) return;
            List<string[][]> catRows = new List<string[][]>();
            for (int i = 0; i < uniColumnNames.Count; i++)
            {
                catRows.Add(new string[mdata.ColumnCount][]);
                for (int j = 0; j < mdata.ColumnCount; j++)
                {
                    catRows[i][j] = new string[1] { "" };
                }
            }
            AssignCategory(mdata, samples, catRows, requireTitles, renameType);
            CheckValidCat(catRows);
            for (int i = 0; i < catRows.Count; i++)
            {
                if ((uniColumnNames[i].ToLower() != "names") && (uniColumnNames[i].ToLower() != "name"))
                {
                    mdata.AddCategoryRow(uniColumnNames[i], uniColumnNames[i], catRows[i]);
                }
                else
                {
                    CheckRepeat(catRows[i]);
                    for (int j = 0; j < mdata.ColumnCount; j++)
                    {
                        if (catRows[i][j][0] != "")
                            mdata.ColumnNames[j] = catRows[i][j][0];
                    }
                }
            }
        }

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            List<string> exCols = mdata.ColumnNames;
            return new Parameters(new FileParam("Isobaric labeling profile")
            {
                Help = "Please specify here the name of the isobaric labeling profiles. " +
                "The file should contain at least the names of experiments called \"Experiments\", " +
                "channel index called \"Channels\" (the order, which is from lowest mass to highest is 1 - 11). " +
                "If the profile contain a column called \"Names\", the name of main columns will be replaced by " +
                "the given names. The other information can be also extended by additional columns."
            }, new SingleChoiceWithSubParams("Delimiter of the profile")
            {
                Help = "The delimiter of the isobaric labeling profile for separating columns. ",
                Values = new[] { "Tab", "Space", "Comma", "Semicolon", "Other" },
                SubParams = new[]{
                        new Parameters(), new Parameters(), new Parameters(), new Parameters(),
                        new Parameters(new StringParam("Keyin the delimiter", ""){
                        Help = "Typing the delimiter of the isobaric labeling profile."})
                        },
                ParamNameWidth = 50,
                TotalWidth = 731
            }, new SingleChoiceParam("The type for renaming main columns")
            {
                Help = "If the profile contains a column called \"Names\", The names of the main columns can be " +
                "renamed by extending or replacing the information of \"Names\" to the original names. " +
                "Additionally, the main column names can be only replaced their experiment names " +
                "and channel indices, but keep the other part of the original names.",
                Values = new[] { "Only replace the experiment names and channel indices", "Extend", "Replace" }
            });
        }
    }
}
