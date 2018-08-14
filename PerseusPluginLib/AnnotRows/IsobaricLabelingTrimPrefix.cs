﻿using System;
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
    public class IsobaricLabelingTrim : IMatrixProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Annotate the column names by trimming the prefix of isobaric labeling columns.";
        public string HelpOutput => "The category will be generated by trimming the prefix of the isobaric labeling columns.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Isobaric labeling annotation - prefix trimming";
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

        public string CheckChannelIndex(string[] colWords, int Channel, int colAfterChannel)
        {
            string newStr;
            if (int.TryParse(colWords[Channel], out int isInt))
            {
                int skipInd = Channel + 1;
                newStr = string.Join(" ", colWords.Skip(skipInd));
            }
            else
            {
                newStr = string.Join(" ", colWords.Skip(Channel));
            }
            return newStr;
        }

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            string prefixStr = param.GetParam<string>("Trimming prefix").Value;
            int[] mainColInd = param.GetParam<int[]>("Columns").Value;
            string[][] mainCols = new string[mdata.ColumnCount][];
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                mainCols[i] = new string[1];
                if (mainColInd.Contains(i))
                {
                    string prefixCol = mdata.ColumnNames[i].Substring(0, prefixStr.Length);
                    if (prefixCol == prefixStr)
                    {
                        prefixCol = mdata.ColumnNames[i].Substring(prefixStr.Length, mdata.ColumnNames[i].Length - prefixStr.Length);
                        string[] colWords = prefixCol.Split(' ');
                        if (colWords.Length > 0)
                        {
                            if (colWords[0].Length == 0)
                            {
                                prefixCol = CheckChannelIndex(colWords, 1, 2);
                            }
                            else
                            {
                                prefixCol = CheckChannelIndex(colWords, 0, 1);
                            }
                        }
                        mainCols[i][0] = prefixCol;
                    }
                }
            }
            mdata.AddCategoryRow("Group", "prefix trimming", mainCols);
        }

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            List<string> exCols = mdata.ColumnNames;
            return new Parameters(new StringParam("Trimming prefix", "Reporter intensity corrected")
            {
                Help = "The prefix string which need to be trimmed."
            }, new MultiChoiceParam("Columns")
            {
                Help = "The columns for doing prefix trimming.",
                Value = ArrayUtils.ConsecutiveInts(exCols.Count),
                Values = exCols
            });
        }
    }
}
