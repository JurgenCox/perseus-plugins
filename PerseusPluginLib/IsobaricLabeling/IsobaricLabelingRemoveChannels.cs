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

namespace PerseusPluginLib.IsobaricLabeling
{
    public class IsobaricLabelingRemove : IMatrixProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Remove the columns which belongs to specific channels.";
        public string HelpOutput => "The columns will be removed if they belong to specific channels. It will be helpful for removing reference and empty channels.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Remove channels";
        public string Heading => "Isobaric Labeling";
        public bool IsActive => true;
        public float DisplayRank => 6;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;
        public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange";

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            ParameterWithSubParams<int> mode = param.GetParamWithSubParams<int>("Removing mode");
            string channelInds = param.GetParam<string>("Channel Number (split by comma)").Value;
            channelInds = channelInds.Replace(" ", "");
            string[] channels = channelInds.Split(new string[] { "," }, StringSplitOptions.None);
            List<int> remainColInds = new List<int>();
            if (mode.Value == 0)
            {
                string prefixStr = mode.GetSubParameters().GetParam<string>("Prefix string").Value;
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    if (prefixStr.Length < mdata.ColumnNames[i].Length)
                    {
                        string prefixCol = mdata.ColumnNames[i].Substring(0, prefixStr.Length);
                        if (prefixCol == prefixStr)
                        {
                            string colWithChannel = mdata.ColumnNames[i].Replace(prefixCol, "");
                            string channelInd = colWithChannel.Split(new string[] { " " }, StringSplitOptions.None)[1];
                            if (!channels.Contains(channelInd))
                            {
                                remainColInds.Add(i);
                            }
                        }
                    }
                }
            }
            else
            {
                int channelCat = mode.GetSubParameters().GetParam<int>("Group").Value;
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    if (!channels.Contains(mdata.GetCategoryRowAt(channelCat)[i][0]))
                    {
                        remainColInds.Add(i);
                    }
                }
            }
            if (remainColInds.Count != 0)
            {
                mdata.ExtractColumns(remainColInds.ToArray());
            }
        }

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            List<string> exCols = mdata.ColumnNames;
            return new Parameters(new SingleChoiceWithSubParams("Removing mode")
            {
                Help = "The delimiter of the isobaric labeling profile for separating columns. ",
                Values = new[] { "Names of main columns", "Category" },
                SubParams = new[]{
                        new Parameters(new StringParam("Prefix string", "Reporter intensity corrected"){
                        Help = "The prefix string of channel indices."}),
                        new Parameters(new SingleChoiceParam("Group") {Values = mdata.CategoryRowNames})
                        },
                ParamNameWidth = 50,
                TotalWidth = 731
            }, new StringParam("Channel Number (split by comma)", "1")
            {
                Help = "The channels which need to be removed.",
            });
        }
    }
}
