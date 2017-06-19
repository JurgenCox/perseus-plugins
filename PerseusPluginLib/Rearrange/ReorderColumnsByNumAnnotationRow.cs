using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange {
	public class ReorderColumnsByNumAnnotationRow : IMatrixProcessing {
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string HelpOutput => "Same matrix but with columns in the new order implied by a numerical row.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Heading => "Rearrange";
		public string Name => "Reorder columns by numerical annotation row";
		public bool IsActive => true;
		public float DisplayRank => 2.9f;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => null;

		public string Description =>
			"The order of the columns as they appear in the matrix is changed according to the values in a numerical row " +
			"in ascending order. This can be useful for displaying columns in a specific order in a heat map.";

		public int GetMaxThreads(Parameters parameters) {
			return 1;
		}

		public void ProcessData(IMatrixData data, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo) {
			if (data.NumericRowCount == 0) {
				processInfo.ErrString = "Data contains no numerical columns";
			}
			int rowInd = param.GetParam<int>("Numerical row").Value;
			double[] vals = data.NumericRows[rowInd];
			int[] o = ArrayUtils.Order(vals);
			bool descending = param.GetParam<int>("Order").Value > 0;
			if (descending) {
				ArrayUtils.Revert(o);
			}
			data.ExtractColumns(o);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString) {
			return new Parameters(
				new SingleChoiceParam("Numerical row") {
					Value = 0,
					Values = mdata.NumericRowNames,
					Help = "Specify here the numerical row according to which the columns will be reordered."
				}, new SingleChoiceParam("Order") {Values = new List<string>(new[] {"ascending", "descending"})});
		}
	}
}