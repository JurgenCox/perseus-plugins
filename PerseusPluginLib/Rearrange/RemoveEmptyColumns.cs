using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Rearrange{
	public class RemoveEmptyColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string HelpOutput => "Same matrix but with empty columns removed.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Heading => "Rearrange";
		public string Name => "Remove empty columns";
		public bool IsActive => true;
		public float DisplayRank => 3.5f;
		public string Description => "Columns containing no values or only invalid values will be removed.";
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:RemoveEmptyColumns";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData data, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] exColInds = GetValidExCols(data);
			int[] numColInds = GetValidNumCols(data);
			int[] multiNumColInds = GetValidMultiNumCols(data);
			int[] catColInds = GetValidCatCols(data);
			int[] textColInds = GetValidTextCols(data);
			if (exColInds.Length < data.ColumnCount){
				data.ExtractColumns(exColInds);
			}
			if (numColInds.Length < data.NumericColumnCount){
				data.NumericColumns = data.NumericColumns.SubList(numColInds);
				data.NumericColumnNames = data.NumericColumnNames.SubList(numColInds);
				data.NumericColumnDescriptions = data.NumericColumnDescriptions.SubList(numColInds);
			}
			if (multiNumColInds.Length < data.MultiNumericColumnCount){
				data.MultiNumericColumns = data.MultiNumericColumns.SubList(multiNumColInds);
				data.MultiNumericColumnNames = data.MultiNumericColumnNames.SubList(multiNumColInds);
				data.MultiNumericColumnDescriptions = data.MultiNumericColumnDescriptions.SubList(multiNumColInds);
			}
			if (catColInds.Length < data.CategoryColumnCount){
				data.CategoryColumns = PerseusPluginUtils.GetCategoryColumns(data, catColInds);
				data.CategoryColumnNames = data.CategoryColumnNames.SubList(catColInds);
				data.CategoryColumnDescriptions = data.CategoryColumnDescriptions.SubList(catColInds);
			}
			if (textColInds.Length < data.StringColumnCount){
				data.StringColumns = data.StringColumns.SubList(textColInds);
				data.StringColumnNames = data.StringColumnNames.SubList(textColInds);
				data.ColumnDescriptions = data.StringColumnDescriptions.SubList(textColInds);
			}
		}

		private static int[] GetValidTextCols(IMatrixData data){
			List<int> valids = new List<int>();
			for (int i = 0; i < data.StringColumnCount; i++){
				if (!IsInvalidStringColumn(data.StringColumns[i])){
					valids.Add(i);
				}
			}
			return valids.ToArray();
		}

		private static int[] GetValidCatCols(IMatrixData data){
			List<int> valids = new List<int>();
			for (int i = 0; i < data.CategoryColumnCount; i++){
				if (!IsInvalidCatColumn(data.GetCategoryColumnAt(i))){
					valids.Add(i);
				}
			}
			return valids.ToArray();
		}

		private static int[] GetValidMultiNumCols(IMatrixData data){
			List<int> valids = new List<int>();
			for (int i = 0; i < data.MultiNumericColumnCount; i++){
				if (!IsInvalidMultiNumColumn(data.MultiNumericColumns[i])){
					valids.Add(i);
				}
			}
			return valids.ToArray();
		}

		private static int[] GetValidNumCols(IMatrixData data){
			List<int> valids = new List<int>();
			for (int i = 0; i < data.NumericColumnCount; i++){
				if (!IsInvalidNumColumn(data.NumericColumns[i])){
					valids.Add(i);
				}
			}
			return valids.ToArray();
		}

		private static int[] GetValidExCols(IMatrixData data){
			List<int> valids = new List<int>();
			for (int i = 0; i < data.ColumnCount; i++){
				if (!IsInvalidExColumn(data.Values.GetColumn(i))){
					valids.Add(i);
				}
			}
			return valids.ToArray();
		}

		private static bool IsInvalidStringColumn(IEnumerable<string> stringColumn){
			foreach (string s in stringColumn){
				if (!string.IsNullOrEmpty(s)){
					return false;
				}
			}
			return true;
		}

		private static bool IsInvalidCatColumn(IEnumerable<string[]> categoryColumn){
			foreach (string[] s in categoryColumn){
				if (s != null && s.Length > 0){
					return false;
				}
			}
			return true;
		}

		private static bool IsInvalidMultiNumColumn(IEnumerable<double[]> multiNumericColumn){
			foreach (double[] s in multiNumericColumn){
				if (s != null && s.Length > 0){
					return false;
				}
			}
			return true;
		}

		private static bool IsInvalidNumColumn(IEnumerable<double> numericColumn){
			foreach (double d in numericColumn){
				if (!double.IsNaN(d) && !double.IsInfinity(d)){
					return false;
				}
			}
			return true;
		}

		private static bool IsInvalidExColumn(IEnumerable<double> expressionColumn){
			foreach (double d in expressionColumn){
				if (!double.IsNaN(d) && !double.IsInfinity(d)){
					return false;
				}
			}
			return true;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return new Parameters();
		}
	}
}