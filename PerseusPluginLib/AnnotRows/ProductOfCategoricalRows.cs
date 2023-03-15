using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using BaseLibS.Num;
using BaseLibS.Util;
namespace PerseusPluginLib.AnnotRows {
	public class ProductOfCategoricalRows : IMatrixProcessing {
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Two or more categorical rows are combined into one by using product terms.";
		public string HelpOutput => "Matrix with the product categorical row added.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Product of categorical rows";
		public string Heading => "Annot. rows";
		public bool IsActive => true;
		public float DisplayRank => 22;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url => null;

		public int GetMaxThreads(Parameters parameters) {
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return new Parameters(new MultiChoiceParam("Rows"){
				Values = mdata.CategoryRowNames,
				Help = "The categorical rows that should be combined"
			});
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] rowInds = param.GetParam<int[]>("Rows").Value;
			if (rowInds.Length == 0){
				processInfo.ErrString = "Please select at least one categorical row.";
				return;
			}
			int ncols = mdata.ColumnCount;
			string[][] catRowData = new string[rowInds.Length][];
			for (int i = 0; i < rowInds.Length; i++){
				string[][] x = mdata.GetCategoryRowAt(rowInds[i]);
				catRowData[i] = new string[ncols];
				for (int j = 0; j < ncols; j++){
					string[] u = x[j];
					if (u.Length != 1){
						processInfo.ErrString = "One of the category rows does not gave exactly one entry somewhere.";
						return;
					}
					catRowData[i][j] = u[0];
				}
			}
			string[][] newRow = new string[ncols][];
			for (int i = 0; i < ncols; i++){
				string[] vals = new string[rowInds.Length];
				for (int j = 0; j < vals.Length; j++){
					vals[j] = catRowData[j][i];
				}
				string newVal = StringUtils.Concat("_", vals);
				newRow[i] = new[]{newVal};
			}
			string newRowName = StringUtils.Concat("_", mdata.CategoryRowNames.SubArray(rowInds));
			mdata.AddCategoryRow(newRowName, newRowName, newRow);
		}
	}
}
