using System;
using System.Collections.Generic;
using System.Linq;
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
			IMatrixData matrixData2 = inputData[1];
		    if (matrixData1.StringColumnCount + matrixData1.NumericColumnCount == 0)
		    {
		        errString = $"Please add at least one text or numeric column to {matrixData1.Name}.";
		        return null;
		    }
		    if (matrixData2.StringColumnCount + matrixData2.NumericColumnCount == 0)
		    {
		        errString = $"Please add at least one text or numeric column to {matrixData2.Name}.";
		        return null;
		    }

		    var matchColumnNames = matrixData1.StringColumnNames.Concat(matrixData1.NumericColumnNames).ToList();
		    var matchColumnNames2 = matrixData2.StringColumnNames.Concat(matrixData2.NumericColumnNames).ToList();
			var matchType = new Parameter[]
			{
				new BoolParam("Union", false)
				{
					Help = "Add unmatched columns from the second matrix."
				},
				new BoolParam("Add indicator")
				{
					Help = "If checked, a categorical column will be added in" +
					       " which it is indicated by a '+' if at least one row of the second " +
					       "matrix matches."
				}
			};
		    return
				new Parameters(CreateMatchParameters(matchColumnNames, matchColumnNames2)
				              .Concat(matchType)
				              .Concat(CreateCopyParameters(matrixData2))
				              .ToArray());
		}

		public static Parameter[] CreateCopyParameters(IMatrixData matrixData2)
	    {
	        return new Parameter[]{
            new MultiChoiceParam("Copy main columns"){
                Values = matrixData2.ColumnNames,
                Value = new int[0],
                Help = "Main columns of the second matrix that should be added to the first matrix."
            }, new SingleChoiceParam("Combine copied main values"){
                Values = new[]{"Median", "Mean", "Minimum", "Maximum", "Sum", "Keep separate"},
                Help = "In case multiple rows of the second matrix match to a row of the first matrix, how should multiple " +
                        "values be combined?"
            }, new MultiChoiceParam("Copy categorical columns"){
                Values = matrixData2.CategoryColumnNames,
                Value = new int[0],
                Help = "Categorical columns of the second matrix that should be added to the first matrix."
            }, new MultiChoiceParam("Copy text columns"){
                Values = matrixData2.StringColumnNames,
                Value = new int[0],
                Help = "Text columns of the second matrix that should be added to the first matrix."
            }, new MultiChoiceParam("Copy numerical columns"){
                Values = matrixData2.NumericColumnNames,
                Value = new int[0],
                Help = "Numerical columns of the second matrix that should be added to the first matrix."
            }, new SingleChoiceParam("Combine copied numerical values"){
                Values = new[]{"Median", "Mean", "Minimum", "Maximum", "Sum", "Keep separate"},
                Help =
                    "In case multiple rows of the second matrix match to a row of the first matrix, how should multiple " +
                    "numerical values be combined?"
            }};
        }

        public static Parameter[] CreateMatchParameters(List<string> matchColumnNames, List<string> matchColumnNames2, string tryFind = "uniprot")
	    {
	        int FindUniprot(List<string> names) => Math.Max(0, names.FindIndex(col => col.ToLower().Contains("uniprot")));
	        return new Parameter[] {new SingleChoiceParam("Matching column in table 1"){
	            Values = matchColumnNames,
	            Value = FindUniprot(matchColumnNames),
	            Help = "The column in the first matrix that is used for matching rows."
	        }, new SingleChoiceParam("Matching column in table 2"){
	            Values = matchColumnNames2,
	            Value = FindUniprot(matchColumnNames2),
	            Help = "The column in the second matrix that is used for matching rows."
	        }, new BoolWithSubParams("Use additional column pair"){
	            SubParamsTrue =
	                new Parameters(new SingleChoiceParam("Additional column in table 1"){
	                    Values = matchColumnNames,
	                    Value = FindUniprot(matchColumnNames),
	                    Help = "Additional column in the first matrix that is used for matching rows."
	                }, new SingleChoiceParam("Additional column in table 2"){
	                    Values = matchColumnNames2,
	                    Value = FindUniprot(matchColumnNames2),
	                    Help = "Additional column in the second matrix that is used for matching rows."
	                })
	        }};
	    }

	    private const string Separator = "!§$%";
		public IMatrixData ProcessData(IMatrixData[] inputData, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			IMatrixData mdata1 = inputData[0];
			IMatrixData mdata2 = inputData[1];
		    var matching = ParseMatchingColumns(parameters);
			var union = parameters.GetParam<bool>("Union").Value;
		    int[][] indexMap = GetIndexMap(mdata1, mdata2, matching.first, matching.second, union);

		    var result = (IMatrixData) mdata1.Clone();
		    result.Origin = "Combination";
		    var addIndicator = parameters.GetParam<bool>("Add indicator").Value;
	        if (addIndicator)
	        {
                AddIndicator(result, mdata2, indexMap);
            }
		    var (main, text, numeric, category) = ParseCopyParameters(parameters);
			SetAnnotationRows(result, mdata1, mdata2, main.copy);
		    AddMainColumns(result, mdata2, indexMap, main.copy, GetAveraging(main.combine));
		    AddAnnotationColumns(result, mdata2, indexMap, text, numeric, category);
			if (union)
			{
				for (int i = 0; i < result.StringColumnCount; i++)
				{
					var column = result.StringColumns[i];
					result.StringColumns[i] = column.Concat(Enumerable.Repeat(string.Empty, indexMap.Length - column.Length)).ToArray();
				}
				for (int i = 0; i < result.NumericColumnCount; i++)
				{
					var column = result.NumericColumns[i];
					result.NumericColumns[i] = column.Concat(Enumerable.Repeat(double.NaN, indexMap.Length - column.Length)).ToArray();
				}
				for (int i = 0; i < result.CategoryColumnCount; i++)
				{
					var column = result.GetCategoryColumnAt(i);
					result.SetCategoryColumnAt(column.Concat(Enumerable.Repeat(new string[0], indexMap.Length - column.Length)).ToArray(), i);
				}
			}
		    return result;
		}

	    public static ((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category) ParseCopyParameters(
	        Parameters parameters)
	    {
		    var exColInds = parameters.GetParam<int[]>("Copy main columns").Value;
		    var combineMain = parameters.GetParam<int>("Combine copied main values").Value;
		    var copyNumericalColumns = parameters.GetParam<int[]>("Copy numerical columns").Value;
		    var combineNumerical = parameters.GetParam<int>("Combine copied numerical values").Value;
		    var copyCatColumns = parameters.GetParam<int[]>("Copy categorical columns").Value;
		    var copyTextColumns = parameters.GetParam<int[]>("Copy text columns").Value;
	        return ((exColInds, combineMain), copyTextColumns, (copyNumericalColumns, combineNumerical), copyCatColumns);
	    }

	    public static ((int m1, int m2) first, (int m1, int m2)? second) ParseMatchingColumns(Parameters parameters)
	    {
	        int Value(Parameters param, string name) => param.GetParam<int>(name).Value;
	        (int, int) first = (Value(parameters, "Matching column in table 1"), Value(parameters, "Matching column in table 2"));
	        ParameterWithSubParams<bool> p = parameters.GetParamWithSubParams<bool>("Use additional column pair");
	        Parameters subPar = p.GetSubParameters();
	        (int m1, int m2)? second = null;
	        if (p.Value)
	        {
	            second = (Value(subPar, "Additional column in table 1"), Value(subPar, "Additional column in table 2"));
	        }
	        return (first, second);
	    }

	    private static void AddIndicator(IDataWithAnnotationColumns result, IData mdata2, int[][] indexMap)
	    {
	            string[][] indicatorCol = new string[indexMap.Length][];
	            for (int i = 0; i < indexMap.Length; i++)
	            {
	                indicatorCol[i] = indexMap[i].Length > 0 ? new[] {"+"} : new string[0];
	            }
	            result.AddCategoryColumn(mdata2.Name, "", indicatorCol);
	    }

	    public static void AddAnnotationColumns(IDataWithAnnotationColumns result, IDataWithAnnotationColumns mdata2,
	        int[][] indexMap, int[] copyTextColumns, (int[] copy, int combine) numeric, int[] copyCatColumns)
	    {
	        AddNumericColumns(result, mdata2, indexMap, numeric.copy, GetAveraging(numeric.combine));
	        AddCategoricalColumns(result, mdata2, indexMap, copyCatColumns);
	        AddStringColumns(result, mdata2, indexMap, copyTextColumns);
	    }

		private static void AddMainColumns(IMatrixData result, IMatrixData mdata2,
		    IList<int[]> indexMap, int[] exColInds, Func<double[], double> avExpression)
		{
		    if (avExpression != null)
		    {
                var columns = exColInds.Select(i =>
                {
                    var name = mdata2.ColumnNames[i];
                    var values = mdata2.Values.GetColumn(i).Unpack();
                    var quality = mdata2.HasQuality ? mdata2.Quality.GetColumn(i).Unpack() : null;
                    var isImputed = mdata2.IsImputed.IsInitialized() ? mdata2.IsImputed.GetColumn(i) : null;
                    return (name, values, quality, isImputed);
                }).ToArray();
                AddMainColumns(result, indexMap, columns, avExpression);
		    }
		    else
		    {
                var columns = exColInds.Select(i =>
                {
                    var name = mdata2.ColumnNames[i];
                    var values = mdata2.Values.GetColumn(i).Unpack();
                    return (name, values);
                }).ToArray();
                AddMultiNumericColumns(result,  indexMap, columns);
		    }
		}

	    private static void AddMainColumns(IMatrixData result, IList<int[]> indexMap,
	        (string name, double[] values, double[] quality, bool[] isImputed)[] columns,
	        Func<double[], double> avExpression)
	    {
	        var n = indexMap.Count;
	        if (columns.Length > 0)
	        {
	            double[,] newExColumns = new double[n, columns.Length];
	            double[,] newQuality = new double[n, columns.Length];
	            bool[,] newIsImputed = new bool[n, columns.Length];
	            string[] newExColNames = new string[columns.Length];
	            for (int i = 0; i < columns.Length; i++)
	            {
	                var (name, oldValues, quality, isImputed) = columns[i];
	                newExColNames[i] = name;
	                for (int j = 0; j < n; j++)
	                {
	                    int[] inds = indexMap[j];
	                    List<double> values = new List<double>();
	                    List<double> qual = new List<double>();
	                    List<bool> imp = new List<bool>();
	                    foreach (int ind in inds)
	                    {
	                        double v = oldValues[ind];
	                        if (!double.IsNaN(v) && !double.IsInfinity(v))
	                        {
	                            values.Add(v);
	                            if (quality != null)
	                            {
	                                double qx = quality[ind];
	                                if (!double.IsNaN(qx) && !double.IsInfinity(qx))
	                                {
	                                    qual.Add(qx);
	                                }
	                            }
	                            if (isImputed != null)
	                            {
	                                bool isi = isImputed[ind];
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

	    private static void AddNumericColumns(IDataWithAnnotationColumns result, IDataWithAnnotationColumns mdata2, IList<int[]> indexMap, int[] copyNumericalColumns, Func<double[], double> avNumerical){
            var columns = copyNumericalColumns.Select(i => (mdata2.NumericColumnNames[i], mdata2.NumericColumns[i])).ToArray();
		    if (avNumerical != null)
		    {
		        AddNumericColumns(result, indexMap, columns, avNumerical);
		    } else
		    {
                AddMultiNumericColumns(result, indexMap, columns);
		    }
		}

	    private static void AddNumericColumns(IDataWithAnnotationColumns result,
	        IList<int[]> indexMap, (string name, double[] values)[] columns, Func<double[], double> avNumerical)
	    {
	        double[][] newNumericalColumns = new double[columns.Length][];
	        string[] newNumColNames = new string[columns.Length];
	        for (int i = 0; i < columns.Length; i++)
	        {
	            (string name, double[] oldCol) = columns[i];
	            newNumColNames[i] = name;
	            newNumericalColumns[i] = new double[result.RowCount];
	            for (int j = 0; j < result.RowCount; j++)
	            {
	                int[] inds = indexMap[j];
	                List<double> values = new List<double>();
	                foreach (int ind in inds)
	                {
	                    double v = oldCol[ind];
	                    if (!double.IsNaN(v))
	                    {
	                        values.Add(v);
	                    }
	                }
	                newNumericalColumns[i][j] = values.Count == 0 ? double.NaN : avNumerical(values.ToArray());
	            }
	        }
	        for (int i = 0; i < columns.Length; i++)
	        {
	            result.AddNumericColumn(newNumColNames[i], "", newNumericalColumns[i]);
	        }
	    }

	    private static void AddMultiNumericColumns(IDataWithAnnotationColumns result, IList<int[]> indexMap, (string name, double[] values)[] numericalColumns)
	    {
	        double[][][] newMultiNumericalColumns = new double[numericalColumns.Length][][];
	        string[] newMultiNumColNames = new string[numericalColumns.Length];
	        for (int i = 0; i < numericalColumns.Length; i++)
	        {
	            (string name, double[] oldCol) = numericalColumns[i];
	            newMultiNumColNames[i] = name;
	            newMultiNumericalColumns[i] = new double[result.RowCount][];
	            for (int j = 0; j < result.RowCount; j++)
	            {
	                int[] inds = indexMap[j];
	                List<double> values = new List<double>();
	                foreach (int ind in inds)
	                {
	                    double v = oldCol[ind];
	                    if (!double.IsNaN(v))
	                    {
	                        values.Add(v);
	                    }
	                }

	                newMultiNumericalColumns[i][j] = values.ToArray();
	            }
	        }
	        for (int i = 0; i < numericalColumns.Length; i++)
	        {
	            result.AddMultiNumericColumn(newMultiNumColNames[i], "", newMultiNumericalColumns[i]);
	        }
	    }

	    private static void AddCategoricalColumns(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2, IList<int[]> indexMap, int[] copyCatColumns){
		    string[][][] newCatColumns = new string[copyCatColumns.Length][][];
			string[] newCatColNames = new string[copyCatColumns.Length];
			for (int i = 0; i < copyCatColumns.Length; i++){
				string[][] oldCol = mdata2.GetCategoryColumnAt(copyCatColumns[i]);
				newCatColNames[i] = mdata2.CategoryColumnNames[copyCatColumns[i]];
				newCatColumns[i] = new string[mdata1.RowCount][];
				for (int j = 0; j < indexMap.Count; j++){
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
			for (int i = 0; i < copyCatColumns.Length; i++){
				mdata1.AddCategoryColumn(newCatColNames[i], "", newCatColumns[i]);
			}
		}

		private static void AddStringColumns(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2, IList<int[]> indexMap, int[] copyTextColumns){
		    string[][] newStringColumns = new string[copyTextColumns.Length][];
			string[] newStringColNames = new string[copyTextColumns.Length];
			for (int i = 0; i < copyTextColumns.Length; i++){
				string[] oldCol = mdata2.StringColumns[copyTextColumns[i]];
				newStringColNames[i] = mdata2.StringColumnNames[copyTextColumns[i]];
				newStringColumns[i] = new string[indexMap.Count];
				for (int j = 0; j < indexMap.Count; j++){
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
			for (int i = 0; i < copyTextColumns.Length; i++){
				mdata1.AddStringColumn(newStringColNames[i], "", newStringColumns[i]);
			}
		}

		private static string[][] GetColumnSplitBySemicolon(IDataWithAnnotationColumns mdata, int matchingColumn)
		{
		    string[] matchingColumn2;
		    if (matchingColumn < mdata.StringColumnCount)
		    {
                matchingColumn2 = mdata.StringColumns[matchingColumn];
            }
		    else
		    {
		        matchingColumn2 = mdata.NumericColumns[matchingColumn - mdata.StringColumnCount]
		            .Select(Convert.ToString)
		            .ToArray();
		    }
            string[][] w = new string[matchingColumn2.Length][];
			for (int i = 0; i < matchingColumn2.Length; i++){
				string r = matchingColumn2[i].Trim();
				w[i] = r.Length == 0 ? new string[0] : r.Split(';');
				w[i] = ArrayUtils.UniqueValues(w[i]);
			}
			return w;
		}

		private static Dictionary<string, List<int>> GetIdToColsSingle(IDataWithAnnotationColumns mdata2, int matchingColumnInMatrix2)
		{
		    string[][] matchCol2 = GetColumnSplitBySemicolon(mdata2, matchingColumnInMatrix2);
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

		private static Dictionary<string, List<int>> GetIdToColsPair(IDataWithAnnotationColumns mdata2, int matchingColumnInMatrix2, int additionalColumnInMatrix2){
			string[][] matchCol = GetColumnSplitBySemicolon(mdata2, matchingColumnInMatrix2);
			string[][] matchColAddtl = GetColumnSplitBySemicolon(mdata2, additionalColumnInMatrix2);
			Dictionary<string, List<int>> idToCols2 = new Dictionary<string, List<int>>();
			for (int i = 0; i < matchCol.Length; i++){
				foreach (string s1 in matchCol[i]){
					foreach (string s2 in matchColAddtl[i]){
						string id = s1 + Separator + s2;
						if (!idToCols2.ContainsKey(id)){
							idToCols2.Add(id, new List<int>());
						}
						idToCols2[id].Add(i);
					}
				}
			}
			return idToCols2;
		}

	    /// <summary>
	    /// Create index map for mapping between two tables. Indices refer to text + numeric columns.
	    /// </summary>
	    public static int[][] GetIndexMap(IDataWithAnnotationColumns mdata1, IDataWithAnnotationColumns mdata2,
	        (int m1, int m2) first, (int m1, int m2)? second, bool union) {
		    Dictionary<string, List<int>> idToCols2;
		    string[][] matchCol1;
		    if (second.HasValue)
		    {
                idToCols2 = GetIdToColsPair(mdata2, first.m2, second.Value.m2);
                matchCol1 = GetColumnPair(mdata1, first.m1, second.Value.m1);
		    }
		    else
		    {
                idToCols2 = GetIdToColsSingle(mdata2, first.m2);
                matchCol1 = GetColumnSplitBySemicolon(mdata1, first.m1);
		    }
			int[][] indexMap = new int[matchCol1.Length][];
			for (int i = 0; i < matchCol1.Length; i++){
				List<int> q = new List<int>();
				foreach (string s in matchCol1[i]){
					if (idToCols2.TryGetValue(s, out var value))
					{
						q.AddRange(value);
					}
				}
				indexMap[i] = ArrayUtils.UniqueValues(q.ToArray());
			}
		    var unmatchedIndices = Enumerable.Range(0, mdata2.RowCount).Except(indexMap.SelectMany(i => i));
		    if (union)
		    {
			    indexMap = indexMap.Concat(unmatchedIndices.Select(i => new[] {i})).ToArray();
		    }
			return indexMap;
		}

		private static string[][] GetColumnPair(IDataWithAnnotationColumns mdata1,
			int matchingColumnInMatrix1, int additionalColumnInMatrix1){
		    string[][] matchCol = GetColumnSplitBySemicolon(mdata1, matchingColumnInMatrix1);
			string[][] matchColAddtl = GetColumnSplitBySemicolon(mdata1, additionalColumnInMatrix1);
			string[][] result = new string[matchCol.Length][];
			for (int i = 0; i < result.Length; i++){
				result[i] = Combine(matchCol[i], matchColAddtl[i]);
			}
			return result;
		}

		private static string[] Combine(IEnumerable<string> s1, ICollection<string> s2){
			List<string> result = new List<string>();
			foreach (string t1 in s1){
				foreach (string t2 in s2){
					result.Add(t1 + Separator + t2);
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

		private static void SetAnnotationRows(IDataWithAnnotationRows result, IDataWithAnnotationRows mdata1, IDataWithAnnotationRows mdata2, int[] exColInds)
        {
            result.CategoryRowNames.Clear();
			result.CategoryRowDescriptions.Clear();
			result.ClearCategoryRows();
			result.NumericRowNames.Clear();
			result.NumericRowDescriptions.Clear();
			result.NumericRows.Clear();
            var exColCount = exColInds.Length;
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
				string[][] categoryRow = new string[mdata1.ColumnCount + exColCount][];
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
					for (int j = 0; j < exColCount; j++){
						categoryRow[mdata1.ColumnCount + j] = c2[exColInds[j]];
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
				double[] numericRow = new double[mdata1.ColumnCount + exColCount];
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
					for (int j = 0; j < exColCount; j++){
						numericRow[mdata1.ColumnCount + j] = c2[exColInds[j]];
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

		private static void AddMainColumns(IMatrixData data, ICollection<string> names, double[,] vals, double[,] qual, bool[,] imp) {
		    int n = data.RowCount;
		    double[,] newVals = new double[n, data.ColumnCount + vals.GetLength(1)];
			double[,] newQual = new double[n, data.ColumnCount + vals.GetLength(1)];
			bool[,] newImp = new bool[n, data.ColumnCount + vals.GetLength(1)];
			for (int i = 0; i < n; i++){
				for (int j = 0; j < data.ColumnCount; j++){
					newVals[i, j] = data.Values.Get(i, j);
					newQual[i, j] = data.Quality?.Get(i, j) ?? 0;
					newImp[i, j] = data.IsImputed?[i, j] ?? false;
				}
				for (int j = 0; j < vals.GetLength(1); j++){
					newVals[i, data.ColumnCount + j] = vals[i, j];
					newQual[i, data.ColumnCount + j] = qual[i, j];
					newImp[i, data.ColumnCount + j] = imp[i, j];
				}
			}
			data.Values.Set(newVals);
			data.Quality?.Set(newQual);
			data.IsImputed?.Set(newImp);
			data.ColumnNames.AddRange(names);
			data.ColumnDescriptions.AddRange(names);
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