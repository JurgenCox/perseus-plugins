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
    public class IsobaricLabelingFilter : IMatrixProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "Filter the columns which contain few values.";
        public string HelpOutput => "The columns will be removed if they contains fewer value than the given cutoff.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Filter columns";
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
            double cutoff = param.GetParam<double>("Cutoff of percentage").Value;
            List<int> remainColInds = new List<int>();
            if (cutoff <= 0)
            {
                processInfo.ErrString = "The given value needs to be larger 0.";
                return;
            }
            else
            {
                for (int i = 0; i < mdata.ColumnCount; i++)
                {
                    int zero = 0;
                    for (int j = 0; j < mdata.RowCount; j++)
                    {
                        if (mdata.Values.Get(j, i) == 0)
                        {
                            zero++;
                        }
                    }
                    if ((((double)zero/mdata.RowCount)*100) < cutoff)
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
            return new Parameters(new DoubleParam("Cutoff of percentage", 70)
            {
                Help = "The columns will be removed if they contain fewer values than this cutoff. "
            });
        }
    }
}
