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

namespace PerseusPluginLib.Rearrange
{
    public class RenameColumnAnnoRow : IMatrixProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Rename the main column names based on categorical row.";
        public string HelpOutput => "The names of the main column will be renamed by the categorical rows.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Rename columns by categorical rows";
        public string Heading => "Rearrange";
        public bool IsActive => true;
        public float DisplayRank => 6;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;
        public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange";

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public void CheckRepeat(string[] newNames)
        {
            List<string> names = new List<string>();
            Dictionary<string, int> repeats = new Dictionary<string, int>();
            for (int i = 0; i < newNames.Length; i++)
            {
                if (!names.Contains(newNames[i]))
                {
                    names.Add(newNames[i]);
                }
                else
                {
                    if (!repeats.ContainsKey(newNames[i]))
                        repeats.Add(newNames[i], 2);
                    else
                        repeats[newNames[i]]++;
                    newNames[i] = newNames[i] + " " + repeats[newNames[i]];
                }
            }
            for (int i = 0; i < newNames.Length; i++)
            {
                if (repeats.ContainsKey(newNames[i]))
                {
                    newNames[i] = newNames[i] + " 1";
                }
            }
        }

        public void IsobaricLabeling(ParameterWithSubParams<int> parmInd, IMatrixData mdata,
            string[] newNames)
        {
            int expInd = parmInd.GetSubParameters().GetParam<int>("Experiment row").Value;
            int channelInd = parmInd.GetSubParameters().GetParam<int>("Channel row").Value;
            bool detect = false;
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                string[] expCols = mdata.GetCategoryRowEntryAt(expInd, i)[0].Split(' ');
                string[] mainCols = mdata.ColumnNames[i].Split(' ');
                if (mainCols.Length > expCols.Length)
                {
                    string[] checkExpName = new string[expCols.Length];
                    Array.Copy(mainCols, (mainCols.Length - expCols.Length), checkExpName, 0, expCols.Length);
                    if ((String.Join(" ", expCols) == String.Join(" ", checkExpName)) &&
                        (mainCols[mainCols.Length - expCols.Length - 1] == mdata.GetCategoryRowEntryAt(channelInd, i)[0]))
                    {
                        string mainString = "";
                        for (int j = 0; j < (mainCols.Length - expCols.Length - 1); j++)
                        {
                            mainString = mainString + " " + mainCols[j];
                        }
                        detect = true;
                        newNames[i] = mainString.Trim() + " " + newNames[i];
                    }
                }
            }
            if (detect)
            {
                CheckRepeat(newNames);
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    mdata.ColumnNames[i] = newNames[i];
                }
            }
            else
            {
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    if (newNames[i] != mdata.ColumnNames[i])
                        mdata.ColumnNames[i] = mdata.ColumnNames[i] + " " + newNames[i];
                }
            }
        }

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            int[] catColInds = param.GetParam<int[]>("Categorical rows for renaming").Value;
            ParameterWithSubParams<int> parmInd = param.GetParamWithSubParams<int>("Rename type");
            int renameType = parmInd.Value;
            string[] newNames = new string[mdata.ColumnCount];
            Dictionary<string, int> repeatNames = new Dictionary<string, int>();
            bool change = false;
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                string newName = "";
                foreach (int catColInd in catColInds)
                {
                    if (mdata.GetCategoryRowEntryAt(catColInd, i)[0] != "NA")
                    {
                        change = true;
                    }
                    newName = newName + " " + mdata.GetCategoryRowEntryAt(catColInd, i)[0];
                }
                if (change)
                {
                    change = false;
                    newNames[i] = newName;
                }
                else
                {
                    newNames[i] = mdata.ColumnNames[i];
                }
            }
            if (renameType == 0)
            {
                IsobaricLabeling(parmInd, mdata, newNames);
            }
            else if (renameType == 1)
            {
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    if (newNames[i] != mdata.ColumnNames[i])
                        mdata.ColumnNames[i] = mdata.ColumnNames[i] + " " + newNames[i];
                }
            }
            else
            {
                CheckRepeat(newNames);
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    mdata.ColumnNames[i] = newNames[i];
                }
            }
        }

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            List<string> catCols = mdata.CategoryRowNames;
            return new Parameters(new MultiChoiceParam("Categorical rows for renaming")
            {
                Value = new int[0],
                Values = catCols,
                Help = "The categorical rows that the renaming are based on."
            }, new SingleChoiceWithSubParams("Rename type")
            {
                Help = "The column names should be modified by extension or replacement of the original name. " +
                "For isobaric labeling columns, the column names can be replaced only for experiment names and " +
                "channel indices, but keep other parts of the original name the same.",
                Values = new[] { "Only replace the experiment names and channel indices (for isobaric labeling)", "Extend", "Replace"},
                SubParams = new[]{
                        new Parameters(new SingleChoiceParam("Experiment row"){
                        Help = "The categorical row of experiment names.",
                        Values = catCols }, new SingleChoiceParam("Channel row"){
                        Help = "The categorical row of channel indices.",
                        Values = catCols }), new Parameters(), new Parameters()
                        },
            });         
        }
    }
}
