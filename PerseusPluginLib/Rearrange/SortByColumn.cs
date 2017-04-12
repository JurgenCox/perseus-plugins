using System;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Vector;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public class SortByColumn : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Simple sorting by a column.";
		public string HelpOutput => "The same matrix but sorted by the specified column.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Sort by column";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 6;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:SortByColumn";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int ind = param.GetParam<int>("Column").Value;
			bool descending = param.GetParam<bool>("Descending").Value;
            if (ind < mdata.ColumnCount)
            {
                BaseVector v = mdata.Values.GetColumn(ind);
                SortByValues(mdata, v.ToArray(), descending);
            }
            else if (ind < mdata.ColumnCount + mdata.NumericColumnCount)
            {
                double[] v = mdata.NumericColumns[ind - mdata.ColumnCount];
                SortByValues(mdata, v, descending);
            }
            else
            {
                string[] v = mdata.StringColumns[ind - mdata.ColumnCount - mdata.NumericColumnCount];
                SortByValues(mdata, v, descending);
            }
        }

	    private static void SortByValues<T>(IMatrixData mdata, T[] v, bool descending) where T : IComparable<T>
	    {
	        int[] o = ArrayUtils.Order(v);
	        if (descending)
	        {
	            ArrayUtils.Revert(o);
	        }
	        mdata.ExtractRows(o);
	    }

	    public Parameters GetParameters(IMatrixData mdata, ref string errorString)
		{
            string[] choice = new[] { mdata.ColumnNames, mdata.NumericColumnNames, mdata.StringColumnNames }.SelectMany(x => x).ToArray();
            return
                new Parameters(
                    new SingleChoiceParam("Column") { Values = choice, Help = "Select here the column that should be used for sorting." },
                    new BoolParam("Descending") { Help = "If checked the values will be sorted largest to smallest." }
                );
        }
    }
}