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
	/// <summary>
	/// Matching columns by name concatenates the rows from two matrices.
	/// </summary>
	public class MatchingMatrixbyColumnName : IMatrixMultiProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("combineButton.Image.png");
		public string Name => "Matching Matrix";
		public bool IsActive => false;
		public float DisplayRank => -3;
		public string HelpOutput => "";

		public string Description =>
			"Two matrices are merged by matching columns by their names. " +
			"The resulting matrix contains the rows of both matrices and a string column with the matrix names.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public int MinNumInput => 2;
		public int MaxNumInput => 20;
		public string Heading => "Basic";

		public string Url => "";

		public string GetInputName(int index){
			return index == 0 ? "Base matrix" : "Other matrix";
		}

		public int nummatrix;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData[] inputData, ref string errString){
			nummatrix = inputData.Length;
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

		private static string[] SpecialSort3(IList<string> x, IList<string> y, IList<string> z,
			out Dictionary<string, int> xdic, out Dictionary<string, int> ydic, out Dictionary<string, int> zdic){
			HashSet<string> hx = ArrayUtils.ToHashSet(x);
			HashSet<string> hy = ArrayUtils.ToHashSet(y);
			HashSet<string> hz = ArrayUtils.ToHashSet(z);
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
				if (hz.Contains(s)){
					hz.Remove(s);
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
			foreach (string t in z){
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
			zdic = new Dictionary<string, int>();
			for (int i = 0; i < z.Count; i++){
				zdic.Add(z[i], i);
			}
			return result.ToArray();
		}

		public IMatrixData ProcessData(IMatrixData[] inputData, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			IMatrixData resultinput = (IMatrixData) inputData[0].CreateNewInstance(DataType.Matrix);
			if (nummatrix == 2){
				IMatrixData mdata1 = inputData[0];
				IMatrixData mdata2 = inputData[1];
				string[] header1 = new string[mdata1.RowCount];
				for (int i = 0; i < mdata1.RowCount; i++){
					header1[i] = mdata1.Name;
				}
				string[] header2 = new string[mdata2.RowCount];
				for (int i = 0; i < mdata2.RowCount; i++){
					header2[i] = mdata2.Name;
				}
				int nrows1 = mdata1.RowCount;
				int nrows2 = mdata2.RowCount;
				int nrows = nrows1 + nrows2;
				string[] expColNames = SpecialSort(mdata1.ColumnNames, mdata2.ColumnNames,
					out Dictionary<string, int> dic1, out Dictionary<string, int> dic2);
				double[,] ex = new double[nrows, expColNames.Length];
				for (int i = 0; i < ex.GetLength(0); i++){
					for (int j = 0; j < ex.GetLength(1); j++){
						ex[i, j] = double.NaN;
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
				string[] numColNames = SpecialSort(mdata1.NumericColumnNames, mdata2.NumericColumnNames, out dic1,
					out dic2);
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
				string[] stringColNames =
					SpecialSort(mdata1.StringColumnNames, mdata2.StringColumnNames, out dic1, out dic2);
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
				string[] catColNames = SpecialSort(mdata1.CategoryColumnNames, mdata2.CategoryColumnNames, out dic1,
					out dic2);
				List<string[][]> catCols = new List<string[][]>();
				for (int i = 0; i < catColNames.Length; i++){
					catCols.Add(new string[nrows][]);
					for (int j = 0; j < nrows; j++){
						catCols[catCols.Count - 1][j] = new string[0];
					}
				}
				for (int i = 0; i < catColNames.Length; i++){
					if (dic1.ContainsKey(catColNames[i])){
						int ind = dic1[catColNames[i]];
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
				string[] multiNumColNames = SpecialSort(mdata1.MultiNumericColumnNames, mdata2.MultiNumericColumnNames,
					out dic1, out dic2);
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
				string MatrixName = "Matrix Name";
				string MatrixDescription = "Description";
				string[] listnames = header1.Concat(header2).ToArray();
				// string[][] resultarray = catlistnames.Select(x => x.ToArray()).ToArray();
				//IMPORTANT!!!!! TODO: check if the name of the matrix if changed
				IMatrixData result = PerseusUtils.CreateMatrixData(inputData[0], ex, expColNames.ToList());
				result.NumericColumnNames = new List<string>(numColNames);
				result.NumericColumnDescriptions = result.NumericColumnNames;
				result.NumericColumns = numCols;
				result.StringColumnNames = new List<string>(stringColNames);
				result.StringColumns = stringCols;
				result.CategoryColumnNames = new List<string>(catColNames);
				result.CategoryColumnDescriptions = result.CategoryColumnNames;
				result.CategoryColumns = catCols;
				result.MultiNumericColumnNames = new List<string>(multiNumColNames);
				result.MultiNumericColumnDescriptions = result.MultiNumericColumnNames;
				result.MultiNumericColumns = multiNumCols;
				HashSet<string> taken = new HashSet<string>(result.StringColumnNames);
				result.AddStringColumn(MatrixName, MatrixName, listnames);
				taken.Add(MatrixName);
				return result;
			} else if (nummatrix == 3){
				////////////////////////////
				////////////////////////////
				IMatrixData mdata1 = inputData[0];
				IMatrixData mdata2 = inputData[1];
				IMatrixData mdata3 = inputData[2];
				string[] header1 = new string[mdata1.RowCount];
				for (int i = 0; i < mdata1.RowCount; i++){
					header1[i] = mdata1.Name;
				}
				string[] header2 = new string[mdata2.RowCount];
				for (int i = 0; i < mdata2.RowCount; i++){
					header2[i] = mdata2.Name;
				}
				string[] header3 = new string[mdata3.RowCount];
				for (int i = 0; i < mdata3.RowCount; i++){
					header3[i] = mdata3.Name;
				}
				int nrows1 = mdata1.RowCount;
				int nrows2 = mdata2.RowCount;
				int nrows3 = mdata3.RowCount;
				int nrows = nrows1 + nrows2 + nrows3;
				string[] expColNames = SpecialSort3(mdata1.ColumnNames, mdata2.ColumnNames, mdata3.ColumnNames,
					out Dictionary<string, int> dic1, out Dictionary<string, int> dic2,
					out Dictionary<string, int> dic3);
				double[,] ex = new double[nrows, expColNames.Length];
				for (int i = 0; i < ex.GetLength(0); i++){
					for (int j = 0; j < ex.GetLength(1); j++){
						ex[i, j] = double.NaN;
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
					if (dic3.ContainsKey(expColNames[i])){
						int ind = dic3[expColNames[i]];
						for (int j = 0; j < nrows3; j++){
							ex[nrows2 + j, i] = mdata3.Values.Get(j, ind);
						}
					}
				}
				string[] numColNames = SpecialSort3(mdata1.NumericColumnNames, mdata2.NumericColumnNames,
					mdata3.NumericColumnNames, out dic1, out dic2, out dic3);
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
					if (dic3.ContainsKey(numColNames[i])){
						int ind = dic3[numColNames[i]];
						for (int j = 0; j < nrows3; j++){
							numCols[i][nrows2 + j] = mdata3.NumericColumns[ind][j];
						}
					}
				}
				string[] stringColNames = SpecialSort3(mdata1.StringColumnNames, mdata2.StringColumnNames,
					mdata3.StringColumnNames, out dic1, out dic2, out dic3);
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
					if (dic3.ContainsKey(stringColNames[i])){
						int ind = dic3[stringColNames[i]];
						for (int j = 0; j < nrows3; j++){
							stringCols[i][nrows2 + j] = mdata3.StringColumns[ind][j];
						}
					}
				}
				string[] catColNames = SpecialSort3(mdata1.CategoryColumnNames, mdata2.CategoryColumnNames,
					mdata3.CategoryColumnNames, out dic1, out dic2, out dic3);
				List<string[][]> catCols = new List<string[][]>();
				for (int i = 0; i < catColNames.Length; i++){
					catCols.Add(new string[nrows][]);
					for (int j = 0; j < nrows; j++){
						catCols[catCols.Count - 1][j] = new string[0];
					}
				}
				for (int i = 0; i < catColNames.Length; i++){
					if (dic1.ContainsKey(catColNames[i])){
						int ind = dic1[catColNames[i]];
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
					if (dic3.ContainsKey(catColNames[i])){
						int ind = dic3[catColNames[i]];
						for (int j = 0; j < nrows3; j++){
							catCols[i][nrows2 + j] = mdata3.GetCategoryColumnEntryAt(ind, j);
						}
					}
				}
				string[] multiNumColNames = SpecialSort3(mdata1.MultiNumericColumnNames, mdata2.MultiNumericColumnNames,
					mdata3.MultiNumericColumnNames, out dic1, out dic2, out dic3);
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
					if (dic3.ContainsKey(multiNumColNames[i])){
						int ind = dic3[multiNumColNames[i]];
						for (int j = 0; j < nrows3; j++){
							multiNumCols[i][nrows2 + j] = mdata3.MultiNumericColumns[ind][j];
						}
					}
				}
				string MatrixName = "Matrix Name";
				string MatrixDescription = "Description";
				string[] listnames = header1.Concat(header2).ToArray();
				string[] listnames3 = listnames.Concat(header3).ToArray();
				// string[][] resultarray = catlistnames.Select(x => x.ToArray()).ToArray();
				//IMPORTANT!!!!! TODO: check if the name of the matrix if changed
				IMatrixData result = PerseusUtils.CreateMatrixData(inputData[0], ex, expColNames.ToList());
				result.NumericColumnNames = new List<string>(numColNames);
				result.NumericColumnDescriptions = result.NumericColumnNames;
				result.NumericColumns = numCols;
				result.StringColumnNames = new List<string>(stringColNames);
				result.StringColumns = stringCols;
				result.CategoryColumnNames = new List<string>(catColNames);
				result.CategoryColumnDescriptions = result.CategoryColumnNames;
				result.CategoryColumns = catCols;
				result.MultiNumericColumnNames = new List<string>(multiNumColNames);
				result.MultiNumericColumnDescriptions = result.MultiNumericColumnNames;
				result.MultiNumericColumns = multiNumCols;
				HashSet<string> taken = new HashSet<string>(result.StringColumnNames);
				result.AddStringColumn(MatrixName, MatrixName, listnames3);
				taken.Add(MatrixName);
				return result;
			} else
				return resultinput;
		}
	}
}