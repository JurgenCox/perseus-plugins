using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Join{
	public class MatchingColumnsByName : IMatrixMultiProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("combineButton.Image.png");
		public string Name => "Matching columns by name";
		public bool IsActive => true;
		public float DisplayRank => -4;
		public string HelpOutput => "";
		public string Description => "Two matrices are merged by matching columns by their names.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public int MinNumInput => 2;
		public int MaxNumInput => 2;
		public string Heading => "Basic";

		public string Url
			=>
				"http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixMultiProcessing:Basic:MatchingColumnsByName"
			;

		public string GetInputName(int index){
			return index == 0 ? "Base matrix" : "Other matrix";
		}

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData[] inputData, ref string errString){
			return new Parameters();
		}

		private static string[] SpecialSort(IList<string> x, IList<string> y, out Dictionary<string, int> xdic,
			out Dictionary<string, int> ydic){
			HashSet<string> hx = ArrayUtils.ToHashSet(x);
			HashSet<string> hy = ArrayUtils.ToHashSet(y);
			HashSet<string> common = new HashSet<string>();
			foreach (string s in hx.Where(hy.Contains)){
				common.Add(s);
			}
			foreach (string s in common){
				if (hx.Contains(s)){
					hx.Remove(s);
				}
				if (hy.Contains(s)){
					hy.Remove(s);
				}
			}
			List<string> result = new List<string>();
			foreach (string t in x){
				if (common.Contains(t)){
					result.Add(t);
				}
			}
			foreach (string t in x){
				if (!common.Contains(t)){
					result.Add(t);
				}
			}
			foreach (string t in y){
				if (!common.Contains(t)){
					result.Add(t);
				}
			}
			xdic = new Dictionary<string, int>();
			for (int i = 0; i < x.Count; i++){
				xdic.Add(x[i], i);
			}
			ydic = new Dictionary<string, int>();
			for (int i = 0; i < y.Count; i++){
				ydic.Add(y[i], i);
			}
			return result.ToArray();
		}

		public IMatrixData ProcessData(IMatrixData[] inputData, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			IMatrixData mdata1 = inputData[0];
			IMatrixData mdata2 = inputData[1];
			int nrows1 = mdata1.RowCount;
			int nrows2 = mdata2.RowCount;
			int nrows = nrows1 + nrows2;
			string[] expColNames = SpecialSort(mdata1.ColumnNames, mdata2.ColumnNames, out Dictionary<string, int> dic1, out Dictionary<string, int> dic2);
			double[,] ex = new double[nrows, expColNames.Length];
			for (int i = 0; i < ex.GetLength(0); i++){
				for (int j = 0; j < ex.GetLength(1); j++){
					ex[i, j] = float.NaN;
				}
			}
			for (int i = 0; i < expColNames.Length; i++){
				if (dic1.ContainsKey(expColNames[i])){
					int ind = dic1[expColNames[i]];
					for (int j = 0; j < nrows1; j++){
						ex[j, i] = mdata1.Values.Get(j, ind);
					}
				}
				if (dic2.ContainsKey(expColNames[i])){
					int ind = dic2[expColNames[i]];
					for (int j = 0; j < nrows2; j++){
						ex[nrows1 + j, i] = mdata2.Values.Get(j, ind);
					}
				}
			}
			string[] numColNames = SpecialSort(mdata1.NumericColumnNames, mdata2.NumericColumnNames, out dic1, out dic2);
			List<double[]> numCols = new List<double[]>();
			for (int i = 0; i < numColNames.Length; i++){
				numCols.Add(new double[nrows]);
				for (int j = 0; j < nrows; j++){
					numCols[numCols.Count - 1][j] = double.NaN;
				}
			}
			for (int i = 0; i < numColNames.Length; i++){
				if (dic1.ContainsKey(numColNames[i])){
					int ind = dic1[numColNames[i]];
					for (int j = 0; j < nrows1; j++){
						numCols[i][j] = mdata1.NumericColumns[ind][j];
					}
				}
				if (dic2.ContainsKey(numColNames[i])){
					int ind = dic2[numColNames[i]];
					for (int j = 0; j < nrows2; j++){
						numCols[i][nrows1 + j] = mdata2.NumericColumns[ind][j];
					}
				}
			}
			string[] stringColNames = SpecialSort(mdata1.StringColumnNames, mdata2.StringColumnNames, out dic1, out dic2);
			List<string[]> stringCols = new List<string[]>();
			for (int i = 0; i < stringColNames.Length; i++){
				stringCols.Add(new string[nrows]);
				for (int j = 0; j < nrows; j++){
					stringCols[stringCols.Count - 1][j] = "";
				}
			}
			for (int i = 0; i < stringColNames.Length; i++){
				if (dic1.ContainsKey(stringColNames[i])){
					int ind = dic1[stringColNames[i]];
					for (int j = 0; j < nrows1; j++){
						stringCols[i][j] = mdata1.StringColumns[ind][j];
					}
				}
				if (dic2.ContainsKey(stringColNames[i])){
					int ind = dic2[stringColNames[i]];
					for (int j = 0; j < nrows2; j++){
						stringCols[i][nrows1 + j] = mdata2.StringColumns[ind][j];
					}
				}
			}
			string[] catColNames = SpecialSort(mdata1.CategoryColumnNames, mdata2.CategoryColumnNames, out dic1, out dic2);
			List<string[][]> catCols = new List<string[][]>();
			for (int i = 0; i < catColNames.Length; i++){
				catCols.Add(new string[nrows][]);
				for (int j = 0; j < nrows; j++){
					catCols[catCols.Count - 1][j] = new string[0];
				}
			}
			for (int i = 0; i < catColNames.Length; i++){
				if (dic1.ContainsKey(catColNames[i])){
					int ind = dic1[stringColNames[i]];
					for (int j = 0; j < nrows1; j++){
						catCols[i][j] = mdata1.GetCategoryColumnEntryAt(ind, j);
					}
				}
				if (dic2.ContainsKey(catColNames[i])){
					int ind = dic2[catColNames[i]];
					for (int j = 0; j < nrows2; j++){
						catCols[i][nrows1 + j] = mdata2.GetCategoryColumnEntryAt(ind, j);
					}
				}
			}
			string[] multiNumColNames = SpecialSort(mdata1.MultiNumericColumnNames, mdata2.MultiNumericColumnNames, out dic1,
				out dic2);
			List<double[][]> multiNumCols = new List<double[][]>();
			for (int i = 0; i < multiNumColNames.Length; i++){
				multiNumCols.Add(new double[nrows][]);
				for (int j = 0; j < nrows; j++){
					multiNumCols[multiNumCols.Count - 1][j] = new double[0];
				}
			}
			for (int i = 0; i < multiNumColNames.Length; i++){
				if (dic1.ContainsKey(multiNumColNames[i])){
					int ind = dic1[multiNumColNames[i]];
					for (int j = 0; j < nrows1; j++){
						multiNumCols[i][j] = mdata1.MultiNumericColumns[ind][j];
					}
				}
				if (dic2.ContainsKey(multiNumColNames[i])){
					int ind = dic2[multiNumColNames[i]];
					for (int j = 0; j < nrows2; j++){
						multiNumCols[i][nrows1 + j] = mdata2.MultiNumericColumns[ind][j];
					}
				}
			}
		    IMatrixData result = PerseusFactory.CreateMatrixData(ex, expColNames.ToList());
			result.ColumnDescriptions = result.ColumnNames;
			result.NumericColumnNames = new List<string>(numColNames);
			result.NumericColumnDescriptions = result.NumericColumnNames;
			result.NumericColumns = numCols;
			result.StringColumnNames = new List<string>(stringColNames);
			result.StringColumnDescriptions = result.StringColumnDescriptions;
			result.StringColumns = stringCols;
			result.CategoryColumnNames = new List<string>(catColNames);
			result.CategoryColumnDescriptions = result.CategoryColumnNames;
			result.CategoryColumns = catCols;
			result.MultiNumericColumnNames = new List<string>(multiNumColNames);
			result.MultiNumericColumnDescriptions = result.MultiNumericColumnNames;
			result.MultiNumericColumns = multiNumCols;
			return result;
		}
	}
}