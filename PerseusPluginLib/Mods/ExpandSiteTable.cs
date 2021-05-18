using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Mods {
	public class ExpandSiteTable : IMatrixProcessing {
		private const int maxInd = 3;
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description =>
			"The '___1', '___2' and '___3' versions of MaxQuant output table columns are rearranged in the matrix " +
			"to become a single column each.";

		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Expand site table";
		public string Heading => "Modifications";
		public bool IsActive => true;
		public float DisplayRank => -5;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url =>
			"http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Modifications:ExpandSiteTable";

		public int GetMaxThreads(Parameters parameters) {
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo) {
			List<string> expNames = mdata.ColumnNames;
			string errorString = null;
			int[,] colInds = SortNumberedNames(expNames, mdata.ColumnDescriptions, maxInd, ref errorString,
				out string[] allSuffixes, out string[] allPrefixes, out List<string> allDescriptions);
			if (errorString != null) {
				processInfo.ErrString = errorString;
				return;
			}
			int[] normalIndices = ArrayUtils.Complement(To1DArray(colInds), expNames.Count);
			normalIndices = FilterExpressionColIndices(normalIndices, mdata.ColumnNames, allPrefixes);
			int[] validNumCols = GetValidNumCols(mdata.NumericColumnNames, allPrefixes);
			int nrows = mdata.RowCount * allSuffixes.Length;
			int ncols = normalIndices.Length + allPrefixes.Length;
			double[,] data = new double[nrows, ncols];
			double[,] quality = new double[nrows, ncols];
			bool[,] imputed = new bool[nrows, ncols];
			List<double[]> numCols = new List<double[]>();
			for (int i = 0; i < validNumCols.Length; i++) {
				numCols.Add(new double[nrows]);
			}
			List<string[]> stringCols = new List<string[]>();
			for (int i = 0; i < mdata.StringColumnCount + 1; i++) {
				stringCols.Add(new string[nrows]);
			}
			List<string[][]> catCols = new List<string[][]>();
			for (int i = 0; i < mdata.CategoryColumnCount + 1; i++) {
				catCols.Add(new string[nrows][]);
			}
			List<double[][]> multiNumCols = new List<double[][]>();
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++) {
				multiNumCols.Add(new double[nrows][]);
			}
			List<string> expColNames = new List<string>();
			List<string> expColDescriptions = new List<string>();
			foreach (int t in normalIndices) {
				expColNames.Add(expNames[t]);
				expColDescriptions.Add(mdata.ColumnDescriptions[t]);
			}
			foreach (Tuple<string, string> t in allPrefixes.Zip(allDescriptions, Tuple.Create)) {
				expColNames.Add(t.Item1);
				expColDescriptions.Add(t.Item2);
			}
			int count = 0;
			for (int i = 0; i < allSuffixes.Length; i++) {
				for (int j = 0; j < mdata.RowCount; j++) {
					count++;
					int rowInd = i * mdata.RowCount + j;
					for (int k = 0; k < normalIndices.Length; k++) {
						data[rowInd, k] = mdata.Values.Get(j, normalIndices[k]);
						quality[rowInd, k] = mdata.Quality.Get(j, normalIndices[k]);
						imputed[rowInd, k] = mdata.IsImputed[j, normalIndices[k]];
					}
					for (int k = 0; k < allPrefixes.Length; k++) {
						data[rowInd, normalIndices.Length + k] = mdata.Values.Get(j, colInds[k, i]);
						quality[rowInd, normalIndices.Length + k] = mdata.Quality.Get(j, colInds[k, i]);
						imputed[rowInd, normalIndices.Length + k] = mdata.IsImputed[j, colInds[k, i]];
					}
					for (int k = 0; k < validNumCols.Length; k++) {
						numCols[k][rowInd] = mdata.NumericColumns[validNumCols[k]][j];
					}
					for (int k = 0; k < mdata.StringColumnCount; k++) {
						stringCols[k][rowInd] = mdata.StringColumns[k][j];
					}
					for (int k = 0; k < mdata.CategoryColumnCount; k++) {
						catCols[k][rowInd] = mdata.GetCategoryColumnEntryAt(k, j);
					}
					for (int k = 0; k < mdata.MultiNumericColumnCount; k++) {
						multiNumCols[k][rowInd] = mdata.MultiNumericColumns[k][j];
					}
					catCols[mdata.CategoryColumnCount][rowInd] = new[] {allSuffixes[i]};
					stringCols[stringCols.Count - 1][count - 1] = "UID" + count;
				}
			}
			string[] catColNames = ArrayUtils.Concat(mdata.CategoryColumnNames, new[] {"Multiplicity"});
			mdata.ColumnNames = expColNames;
			mdata.ColumnDescriptions = expColDescriptions;
			mdata.Values.Set(data);
			mdata.Quality.Set(quality);
			mdata.IsImputed.Set(imputed);
			mdata.SetAnnotationColumns(new List<string>(ArrayUtils.Concat(mdata.StringColumnNames, new[] {"Unique identifier"})),
				stringCols, new List<string>(catColNames), catCols, mdata.NumericColumnNames.SubList(validNumCols),
				numCols, mdata.MultiNumericColumnNames, multiNumCols);
		}

		public static T[] To1DArray<T>(T[,] m) {
			T[] array = new T[m.GetLength(0) * m.GetLength(1)];
			int c = 0;
			for (int i = 0; i < m.GetLength(0); i++) {
				for (int j = 0; j < m.GetLength(1); j++) {
					array[c++] = m[i, j];
				}
			}
			return array;
		}

		private static int[] GetValidNumCols(IList<string> numericColumnNames, IEnumerable<string> allPrefixes) {
			HashSet<string> exclude = new HashSet<string>(allPrefixes);
			List<int> result = new List<int>();
			for (int i = 0; i < numericColumnNames.Count; i++) {
				if (!exclude.Contains(numericColumnNames[i])) {
					result.Add(i);
				}
			}
			return result.ToArray();
		}

		private static int[] FilterExpressionColIndices(IEnumerable<int> normalIndices, IList<string> expColNames,
			IEnumerable<string> allPrefixes) {
			HashSet<string> exclude = new HashSet<string>(allPrefixes);
			List<int> result = new List<int>();
			foreach (int i in normalIndices) {
				if (!exclude.Contains(expColNames[i])) {
					result.Add(i);
				}
			}
			return result.ToArray();
		}

		private static int[,] SortNumberedNames(IList<string> names, IList<string> descriptions, int maxIndex,
			ref string errorString, out string[] allSuffixes, out string[] allPrefixes, out List<string> allDescriptions) {
			HashSet<string> allPrefixes1 = new HashSet<string>();
			List<string> allPrefixesList = new List<string>();
			allDescriptions = new List<string>();
			allSuffixes = new string[maxIndex];
			for (int i = 0; i < maxIndex; i++) {
				allSuffixes[i] = "___" + (i + 1);
			}
			for (int i = 0; i < names.Count; i++) {
				string name = names[i];
				foreach (string t in allSuffixes) {
					if (name.EndsWith(t)) {
						string prefix = name.Substring(0, name.LastIndexOf(t, StringComparison.Ordinal));
						if (!allPrefixes1.Contains(prefix)) {
							allPrefixes1.Add(prefix);
							allPrefixesList.Add(prefix);
							allDescriptions.Add(descriptions[i]);
						}
					}
				}
			}
			allPrefixes = allPrefixesList.ToArray();
			int[,] colInds = new int[allPrefixes.Length, allSuffixes.Length];
			for (int i = 0; i < allPrefixes.Length; i++) {
				for (int j = 0; j < allSuffixes.Length; j++) {
					string q = allPrefixes[i] + allSuffixes[j];
					colInds[i, j] = ArrayUtils.IndexOf(names, q);
				}
			}
			colInds = ExtractCompleteRows(colInds, out int[] valid);
			allPrefixes = allPrefixes.SubArray(valid);
			if (colInds.GetLength(0) == 0) {
				errorString = "There are no suitable columns to expand.";
				return null;
			}
			return colInds;
		}

		private static int[,] ExtractCompleteRows(int[,] colInds, out int[] valid) {
			List<int> valids = new List<int>();
			for (int i = 0; i < colInds.GetLength(0); i++) {
				int c = 0;
				for (int j = 0; j < colInds.GetLength(1); j++) {
					if (colInds[i, j] >= 0) {
						c++;
					}
				}
				if (c == colInds.GetLength(1)) {
					valids.Add(i);
				}
			}
			valid = valids.ToArray();
			return ArrayUtils.ExtractRows(colInds, valid);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString) {
			if (mdata.CategoryRowCount > 0 || mdata.NumericRowCount > 0) {
				errorString = "Categorical and numerical rows are not supported. Please remove them from the table.";
			}
			return new Parameters(new Parameter[] { });
		}
	}
}