using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Join{
	public class MatchingRowsByName : IMatrixMultiProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("combineButton.Image.png");
		public string Name => "Matching rows by name";
		public bool IsActive => true;
		public float DisplayRank => -5;
		public string HelpOutput => "";

		public string Description
			=>
				"The base matrix is copied. Rows of the second matrix are associated with rows of the base matrix via matching " +
				"expressions in a textual column from each matrix. Selected columns of the second matrix are attached to the " +
				"first matrix. If exactly one row of the second matrix corresponds to a row of the base matrix, values are " +
				"just copied. If more than one row of the second matrix matches to a row of the first matrix, the corresponding " +
				"values are averaged (actually the median is taken) for numerical and expression columns and concatenated " +
				"for textual and categorical columns.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public int MinNumInput => 2;
		public int MaxNumInput => 2;
		public string Heading => "Basic";

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixMultiProcessing:Basic:MatchingRowsByName";

		public string GetInputName(int index){
			return index == 0 ? "Base matrix" : "Other matrix";
		}

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData[] inputData, ref string errString){
			IMatrixData matrixData1 = inputData[0];
		    if (matrixData1.StringColumnCount < 1)
		    {
		        errString = $"Please add at least one text column to {matrixData1.Name}";
		        return null;
		    }
			IMatrixData matrixData2 = inputData[1];
		    if (matrixData2.StringColumnCount < 1)
		    {
		        errString = $"Please add at least one text column to {matrixData2.Name}";
		        return null;
		    }
			List<string> controlChoice1 = matrixData1.StringColumnNames;
			int index1 = 0;
			for (int i = 0; i < controlChoice1.Count; i++){
				if (controlChoice1[i].ToLower().Contains("uniprot")){
					index1 = i;
					break;
				}
			}
			List<string> controlChoice2 = matrixData2.StringColumnNames;
			int index2 = 0;
			for (int i = 0; i < controlChoice2.Count; i++){
				if (controlChoice2[i].ToLower().Contains("uniprot")){
					index2 = i;
					break;
				}
			}
			List<string> numCol = matrixData2.NumericColumnNames;
			int[] numSel = new int[0];
			List<string> catCol = matrixData2.CategoryColumnNames;
			int[] catSel = new int[0];
			List<string> textCol = matrixData2.StringColumnNames;
			int[] textSel = new int[0];
			List<string> exCol = matrixData2.ColumnNames;
			int[] exSel = new int[0];
			return
				new Parameters(new SingleChoiceParam("Matching column in matrix 1"){
					Values = controlChoice1,
					Value = index1,
					Help = "The column in the first matrix that is used for matching rows."
				}, new SingleChoiceParam("Matching column in matrix 2"){
					Values = controlChoice2,
					Value = index2,
					Help = "The column in the second matrix that is used for matching rows."
				}, new BoolWithSubParams("Use additional column pair"){
					SubParamsTrue =
						new Parameters(new SingleChoiceParam("Additional column in matrix 1"){
							Values = controlChoice1,
							Value = index1,
							Help = "Additional column in the first matrix that is used for matching rows."
						}, new SingleChoiceParam("Additional column in matrix 2"){
							Values = controlChoice2,
							Value = index2,
							Help = "Additional column in the second matrix that is used for matching rows."
						})
				}, new BoolParam("Indicator"){
					Help =
						"If checked, a categorical column will be added in which it is indicated by a '+' if at least one row of the second " +
						"matrix matches."
				}, new MultiChoiceParam("Main columns"){
					Value = exSel,
					Values = exCol,
					Help = "Main columns of the second matrix that should be added to the first matrix."
				}, new SingleChoiceParam("Combine main values"){
					Values = new[]{"Median", "Mean", "Minimum", "Maximum", "Sum"},
					Help =
						"In case multiple rows of the second matrix match to a row of the first matrix, how should multiple " +
						"values be combined?"
				}, new MultiChoiceParam("Categorical columns"){
					Values = catCol,
					Value = catSel,
					Help = "Categorical columns of the second matrix that should be added to the first matrix."
				}, new MultiChoiceParam("Text columns"){
					Values = textCol,
					Value = textSel,
					Help = "Text columns of the second matrix that should be added to the first matrix."
				}, new MultiChoiceParam("Numerical columns"){
					Values = numCol,
					Value = numSel,
					Help = "Numerical columns of the second matrix that should be added to the first matrix."
				}, new SingleChoiceParam("Combine numerical values"){
					Values = new[]{"Median", "Mean", "Minimum", "Maximum", "Sum", "Keep separate"},
					Help =
						"In case multiple rows of the second matrix match to a row of the first matrix, how should multiple " +
						"numerical values be combined?"
				});
		}

		public IMatrixData ProcessData(IMatrixData[] inputData, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			const string separator = "!§$%";
			IMatrixData mdata1 = inputData[0];
			IMatrixData mdata2 = inputData[1];
			int[][] indexMap = GetIndexMap(mdata1, mdata2, parameters, separator);
			IMatrixData result = GetResult(mdata1, mdata2, parameters, indexMap);
			AddMainColumns(mdata1, mdata2, parameters, indexMap, result);
			AddNumericColumns(mdata1, mdata2, parameters, indexMap, result);
			AddCategoricalColumns(mdata1, mdata2, parameters, indexMap, result);
			AddStringColumns(mdata1, mdata2, parameters, indexMap, result);
			return result;
		}

		private static IMatrixData GetResult(IMatrixData mdata1, IMatrixData mdata2,
			Parameters parameters, IList<int[]> indexMap)
		{
			IMatrixData result = (IMatrixData) mdata1.Clone();
			SetAnnotationRows(result, mdata1, mdata2);
			bool indicator = parameters.GetParam<bool>("Indicator").Value;
			if (indicator){
				string[][] indicatorCol = new string[indexMap.Count][];
				for (int i = 0; i < indexMap.Count; i++){
					indicatorCol[i] = indexMap[i].Length > 0 ? new[]{"+"} : new string[0];
				}
				result.AddCategoryColumn(mdata2.Name, "", indicatorCol);
			}
			result.Origin = "Combination";
			return result;
		}

		private static void AddMainColumns(IDataWithAnnotationColumns mdata1, IMatrixData mdata2, Parameters parameters,
			IList<int[]> indexMap, IMatrixData result){
			Func<double[], double> avExpression = GetAveraging(parameters.GetParam<int>("Combine main values").Value);
			int[] exColInds = parameters.GetParam<int[]>("Main columns").Value;
			if (exColInds.Length > 0){
				double[,] newExColumns = new double[mdata1.RowCount, exColInds.Length];
				double[,] newQuality = new double[mdata1.RowCount, exColInds.Length];
				bool[,] newIsImputed = new bool[mdata1.RowCount, exColInds.Length];
				string[] newExColNames = new string[exColInds.Length];
				for (int i = 0; i < exColInds.Length; i++){
					newExColNames[i] = mdata2.ColumnNames[exColInds[i]];
					for (int j = 0; j < mdata1.RowCount; j++){
						int[] inds = indexMap[j];
						List<double> values = new List<double>();
						List<double> qual = new List<double>();
						List<bool> imp = new List<bool>();
						foreach (int ind in inds){
							double v = mdata2.Values.Get(ind, exColInds[i]);
							if (!double.IsNaN(v) && !double.IsInfinity(v)){
								values.Add(v);
								if (mdata2.Quality.IsInitialized()){
									double qx = mdata2.Quality.Get(ind, exColInds[i]);
									if (!double.IsNaN(qx) && !double.IsInfinity(qx)){
										qual.Add(qx);
									}
								}
								if (mdata2.IsImputed != null){
									bool isi = mdata2.IsImputed[ind, exColInds[i]];
									imp.Add(isi);
								}
							}
						}
						newExColumns[j, i] = values.Count == 0 ? double.NaN : avExpression(values.ToArray());
						newQuality[j, i] = qual.Count == 0 ? double.NaN : avExpression(qual.ToArray());
						newIsImputed[j, i] = imp.Count != 0 && AvImp(imp.ToArray());
					}
				}
				MakeNewNames(newExColNames, result.ColumnNames);
				AddMainColumns(result, newExColNames, newExColumns, newQuality, newIsImputed);
			}
		}

		private static void AddNumericColumns(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2,
			Parameters parameters, IList<int[]> indexMap, IDataWithAnnotationColumns result){
			Func<double[], double> avNumerical = GetAveraging(parameters.GetParam<int>("Combine numerical values").Value);
			int[] numCols = parameters.GetParam<int[]>("Numerical columns").Value;
			if (avNumerical != null){
				double[][] newNumericalColumns = new double[numCols.Length][];
				string[] newNumColNames = new string[numCols.Length];
				for (int i = 0; i < numCols.Length; i++){
					double[] oldCol = mdata2.NumericColumns[numCols[i]];
					newNumColNames[i] = mdata2.NumericColumnNames[numCols[i]];
					newNumericalColumns[i] = new double[mdata1.RowCount];
					for (int j = 0; j < mdata1.RowCount; j++){
						int[] inds = indexMap[j];
						List<double> values = new List<double>();
						foreach (int ind in inds){
							double v = oldCol[ind];
							if (!double.IsNaN(v)){
								values.Add(v);
							}
						}
						newNumericalColumns[i][j] = values.Count == 0 ? double.NaN : avNumerical(values.ToArray());
					}
				}
				for (int i = 0; i < numCols.Length; i++){
					result.AddNumericColumn(newNumColNames[i], "", newNumericalColumns[i]);
				}
			} else{
				double[][][] newMultiNumericalColumns = new double[numCols.Length][][];
				string[] newMultiNumColNames = new string[numCols.Length];
				for (int i = 0; i < numCols.Length; i++){
					double[] oldCol = mdata2.NumericColumns[numCols[i]];
					newMultiNumColNames[i] = mdata2.NumericColumnNames[numCols[i]];
					newMultiNumericalColumns[i] = new double[mdata1.RowCount][];
					for (int j = 0; j < mdata1.RowCount; j++){
						int[] inds = indexMap[j];
						List<double> values = new List<double>();
						foreach (int ind in inds){
							double v = oldCol[ind];
							if (!double.IsNaN(v)){
								values.Add(v);
							}
						}
						newMultiNumericalColumns[i][j] = values.ToArray();
					}
				}
				for (int i = 0; i < numCols.Length; i++){
					result.AddMultiNumericColumn(newMultiNumColNames[i], "", newMultiNumericalColumns[i]);
				}
			}
		}

		private static void AddCategoricalColumns(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2,
			Parameters parameters, IList<int[]> indexMap, IDataWithAnnotationColumns result){
			int[] catCols = parameters.GetParam<int[]>("Categorical columns").Value;
			string[][][] newCatColumns = new string[catCols.Length][][];
			string[] newCatColNames = new string[catCols.Length];
			for (int i = 0; i < catCols.Length; i++){
				string[][] oldCol = mdata2.GetCategoryColumnAt(catCols[i]);
				newCatColNames[i] = mdata2.CategoryColumnNames[catCols[i]];
				newCatColumns[i] = new string[mdata1.RowCount][];
				for (int j = 0; j < mdata1.RowCount; j++){
					int[] inds = indexMap[j];
					List<string[]> values = new List<string[]>();
					foreach (int ind in inds){
						string[] v = oldCol[ind];
						if (v.Length > 0){
							values.Add(v);
						}
					}
					newCatColumns[i][j] = values.Count == 0
						? new string[0]
						: ArrayUtils.UniqueValues(ArrayUtils.Concat(values.ToArray()));
				}
			}
			for (int i = 0; i < catCols.Length; i++){
				result.AddCategoryColumn(newCatColNames[i], "", newCatColumns[i]);
			}
		}

		private static void AddStringColumns(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2,
			Parameters parameters, IList<int[]> indexMap, IDataWithAnnotationColumns result){
			int[] stringCols = parameters.GetParam<int[]>("Text columns").Value;
			string[][] newStringColumns = new string[stringCols.Length][];
			string[] newStringColNames = new string[stringCols.Length];
			for (int i = 0; i < stringCols.Length; i++){
				string[] oldCol = mdata2.StringColumns[stringCols[i]];
				newStringColNames[i] = mdata2.StringColumnNames[stringCols[i]];
				newStringColumns[i] = new string[mdata1.RowCount];
				for (int j = 0; j < mdata1.RowCount; j++){
					int[] inds = indexMap[j];
					List<string> values = new List<string>();
					foreach (int ind in inds){
						string v = oldCol[ind];
						if (v.Length > 0){
							values.Add(v);
						}
					}
					newStringColumns[i][j] = values.Count == 0 ? "" : StringUtils.Concat(";", values.ToArray());
				}
			}
			for (int i = 0; i < stringCols.Length; i++){
				result.AddStringColumn(newStringColNames[i], "", newStringColumns[i]);
			}
		}

		private static string[][] GetColumnSplitBySemicolon(IDataWithAnnotationColumns mdata, Parameters parameters,
			string colName){
			string[] matchingColumn2 = mdata.StringColumns[parameters.GetParam<int>(colName).Value];
			string[][] w = new string[matchingColumn2.Length][];
			for (int i = 0; i < matchingColumn2.Length; i++){
				string r = matchingColumn2[i].Trim();
				w[i] = r.Length == 0 ? new string[0] : r.Split(';');
				w[i] = ArrayUtils.UniqueValues(w[i]);
			}
			return w;
		}

		private static Dictionary<string, List<int>> GetIdToColsSingle(IDataWithAnnotationColumns mdata2,
			Parameters parameters){
			string[][] matchCol2 = GetColumnSplitBySemicolon(mdata2, parameters, "Matching column in matrix 2");
			Dictionary<string, List<int>> idToCols2 = new Dictionary<string, List<int>>();
			for (int i = 0; i < matchCol2.Length; i++){
				foreach (string s in matchCol2[i]){
					if (!idToCols2.ContainsKey(s)){
						idToCols2.Add(s, new List<int>());
					}
					idToCols2[s].Add(i);
				}
			}
			return idToCols2;
		}

		private static Dictionary<string, List<int>> GetIdToColsPair(IDataWithAnnotationColumns mdata2, Parameters parameters,
			Parameters subPar, string separator){
			string[][] matchCol = GetColumnSplitBySemicolon(mdata2, parameters, "Matching column in matrix 2");
			string[][] matchColAddtl = GetColumnSplitBySemicolon(mdata2, subPar, "Additional column in matrix 2");
			Dictionary<string, List<int>> idToCols2 = new Dictionary<string, List<int>>();
			for (int i = 0; i < matchCol.Length; i++){
				foreach (string s1 in matchCol[i]){
					foreach (string s2 in matchColAddtl[i]){
						string id = s1 + separator + s2;
						if (!idToCols2.ContainsKey(id)){
							idToCols2.Add(id, new List<int>());
						}
						idToCols2[id].Add(i);
					}
				}
			}
			return idToCols2;
		}

		private static int[][] GetIndexMap(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2,
			Parameters parameters, string separator){
			ParameterWithSubParams<bool> p = parameters.GetParamWithSubParams<bool>("Use additional column pair");
			bool addtlCol = p.Value;
			Dictionary<string, List<int>> idToCols2 = addtlCol
				? GetIdToColsPair(mdata2, parameters, p.GetSubParameters(), separator)
				: GetIdToColsSingle(mdata2, parameters);
			string[][] matchCol1 = addtlCol
				? GetColumnPair(mdata1, parameters, p.GetSubParameters(), separator)
				: GetColumnSplitBySemicolon(mdata1, parameters, "Matching column in matrix 1");
			int[][] indexMap = new int[matchCol1.Length][];
			for (int i = 0; i < matchCol1.Length; i++){
				List<int> q = new List<int>();
				foreach (string s in matchCol1[i]){
					if (idToCols2.ContainsKey(s)){
						q.AddRange(idToCols2[s]);
					}
				}
				indexMap[i] = ArrayUtils.UniqueValues(q.ToArray());
			}
			return indexMap;
		}

		private static string[][] GetColumnPair(IDataWithAnnotationColumns mdata1, Parameters parameters, Parameters subPar,
			string separator){
			string[][] matchCol = GetColumnSplitBySemicolon(mdata1, parameters, "Matching column in matrix 1");
			string[][] matchColAddtl = GetColumnSplitBySemicolon(mdata1, subPar, "Additional column in matrix 1");
			string[][] result = new string[matchCol.Length][];
			for (int i = 0; i < result.Length; i++){
				result[i] = Combine(matchCol[i], matchColAddtl[i], separator);
			}
			return result;
		}

		private static string[] Combine(IEnumerable<string> s1, IEnumerable<string> s2, string separator){
			List<string> result = new List<string>();
			foreach (string t1 in s1){
				foreach (string t2 in s2){
					result.Add(t1 + separator + t2);
				}
			}
			result.Sort();
			return result.ToArray();
		}

		private static bool AvImp(IEnumerable<bool> b){
			foreach (bool b1 in b){
				if (b1){
					return true;
				}
			}
			return false;
		}

		private static void SetAnnotationRows(IDataWithAnnotationRows result, IDataWithAnnotationRows mdata1,
			IDataWithAnnotationRows mdata2){
			result.CategoryRowNames.Clear();
			result.CategoryRowDescriptions.Clear();
			result.ClearCategoryRows();
			result.NumericRowNames.Clear();
			result.NumericRowDescriptions.Clear();
			result.NumericRows.Clear();
			string[] allCatNames = ArrayUtils.Concat(mdata1.CategoryRowNames, mdata2.CategoryRowNames);
			allCatNames = ArrayUtils.UniqueValues(allCatNames);
			result.CategoryRowNames = new List<string>();
			string[] allCatDescriptions = new string[allCatNames.Length];
			for (int i = 0; i < allCatNames.Length; i++){
				allCatDescriptions[i] = GetDescription(allCatNames[i], mdata1.CategoryRowNames, mdata2.CategoryRowNames,
					mdata1.CategoryRowDescriptions, mdata2.CategoryRowDescriptions);
			}
			result.CategoryRowDescriptions = new List<string>();
			for (int index = 0; index < allCatNames.Length; index++){
				string t = allCatNames[index];
				string[][] categoryRow = new string[mdata1.ColumnCount + mdata2.ColumnCount][];
				for (int j = 0; j < categoryRow.Length; j++){
					categoryRow[j] = new string[0];
				}
				int ind1 = mdata1.CategoryRowNames.IndexOf(t);
				if (ind1 >= 0){
					string[][] c1 = mdata1.GetCategoryRowAt(ind1);
					for (int j = 0; j < c1.Length; j++){
						categoryRow[j] = c1[j];
					}
				}
				int ind2 = mdata2.CategoryRowNames.IndexOf(t);
				if (ind2 >= 0){
					string[][] c2 = mdata2.GetCategoryRowAt(ind2);
					for (int j = 0; j < c2.Length; j++){
						categoryRow[mdata1.ColumnCount + j] = c2[j];
					}
				}
				result.AddCategoryRow(allCatNames[index], allCatDescriptions[index], categoryRow);
			}
			string[] allNumNames = ArrayUtils.Concat(mdata1.NumericRowNames, mdata2.NumericRowNames);
			allNumNames = ArrayUtils.UniqueValues(allNumNames);
			result.NumericRowNames = new List<string>(allNumNames);
			string[] allNumDescriptions = new string[allNumNames.Length];
			for (int i = 0; i < allNumNames.Length; i++){
				allNumDescriptions[i] = GetDescription(allNumNames[i], mdata1.NumericRowNames, mdata2.NumericRowNames,
					mdata1.NumericRowDescriptions, mdata2.NumericRowDescriptions);
			}
			result.NumericRowDescriptions = new List<string>(allNumDescriptions);
			foreach (string t in allNumNames){
				double[] numericRow = new double[mdata1.ColumnCount + mdata2.ColumnCount];
				for (int j = 0; j < numericRow.Length; j++){
					numericRow[j] = double.NaN;
				}
				int ind1 = mdata1.NumericRowNames.IndexOf(t);
				if (ind1 >= 0){
					double[] c1 = mdata1.NumericRows[ind1];
					for (int j = 0; j < c1.Length; j++){
						numericRow[j] = c1[j];
					}
				}
				int ind2 = mdata2.NumericRowNames.IndexOf(t);
				if (ind2 >= 0){
					double[] c2 = mdata2.NumericRows[ind2];
					for (int j = 0; j < c2.Length; j++){
						numericRow[mdata1.ColumnCount + j] = c2[j];
					}
				}
				result.NumericRows.Add(numericRow);
			}
		}

		private static string GetDescription(string name, IList<string> names1, IList<string> names2,
			IList<string> descriptions1, IList<string> descriptions2){
			int ind = names1.IndexOf(name);
			if (ind >= 0){
				return descriptions1[ind];
			}
			ind = names2.IndexOf(name);
			return descriptions2[ind];
		}

		private static Func<double[], double> GetAveraging(int ind){
			switch (ind){
				case 0:
					return ArrayUtils.Median;
				case 1:
					return ArrayUtils.Mean;
				case 2:
					return ArrayUtils.Min;
				case 3:
					return ArrayUtils.Max;
				case 4:
					return ArrayUtils.Sum;
				case 5:
					return null;
				default:
					throw new Exception("Never get here.");
			}
		}


		private static void MakeNewNames(IList<string> newExColNames, IEnumerable<string> mainColumnNames){
			HashSet<string> taken = new HashSet<string>(mainColumnNames);
			for (int i = 0; i < newExColNames.Count; i++){
				if (taken.Contains(newExColNames[i])){
					string n1 = StringUtils.GetNextAvailableName(newExColNames[i], taken);
					newExColNames[i] = n1;
					taken.Add(n1);
				} else{
					taken.Add(newExColNames[i]);
				}
			}
		}
	}
}