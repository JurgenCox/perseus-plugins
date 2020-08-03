using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public class SubsampleRows : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Reduce the matrix to a random sub-sample of row indices.";
		public string HelpOutput => "The same matrix but with a random sub-sample of rows.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Sub-sample rows";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 23;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url => "";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int nrows = param.GetParam<int>("Number of rows").Value;
			if (nrows >= mdata.RowCount){
				return;
			}
			Random2 rand = new Random2(7);
			int[] inds = rand.NextPermutation(mdata.RowCount).SubArray(nrows);
			mdata.ExtractRows(inds);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return new Parameters(
				new IntParam("Number of rows", 1000));
		}
	}
}