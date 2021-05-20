using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Perform;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Basic{
	public class PerformanceCurves : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Performance curves";
		public string Heading => "Basic";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;

		public string Description =>
			"Calculation of predictive performance measures like precision-recall or ROC curves.";

		public bool IsActive => true;
		public float DisplayRank => 10;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url =>
			"http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:PerformanceCurves";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData data, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			bool falseAreIndicated = param.GetParam<int>("Indicated are").Value == 0;
			int catCol = param.GetParam<int>("In column").Value;
			string word = param.GetParam<string>("Indicator").Value;
			int[] scoreColumns = param.GetParam<int[]>("Scores").Value;
			if (scoreColumns.Length == 0){
				processInfo.ErrString = "Please specify at least one column with scores.";
				return;
			}
			bool largeIsGood = param.GetParam<bool>("Large values are good").Value;
			int[] showColumns = param.GetParam<int[]>("Display quantity").Value;
			if (showColumns.Length == 0){
				processInfo.ErrString = "Please select at least one quantity to display";
				return;
			}
			bool[] indCol = GetIndicatorColumn(catCol, word, data);
			List<string> expColNames = new List<string>();
			List<double[]> expCols = new List<double[]>();
			foreach (int scoreColumn in scoreColumns){
				double[] vals = scoreColumn < data.NumericColumnCount
					? data.NumericColumns[scoreColumn]
					: ArrayUtils.ToDoubles(data.Values.GetColumn(scoreColumn - data.NumericColumnCount));
				string name = scoreColumn < data.NumericColumnCount
					? data.NumericColumnNames[scoreColumn]
					: data.ColumnNames[scoreColumn - data.NumericColumnCount];
				CalcCurves(indCol, falseAreIndicated, vals, largeIsGood,
					PerformanceColumnType.allTypes.SubArray(showColumns), name, expCols, expColNames, true);
			}
			double[,] expData = ToMatrix(expCols);
			data.ColumnNames = expColNames;
			data.Values.Set(expData);
			data.SetAnnotationColumns(new List<string>(), new List<string[]>(), new List<string>(),
				new List<string[][]>(), new List<string>(), new List<double[]>(), new List<string>(),
				new List<double[][]>());
		}

		private static void CalcCurves(bool[] indicatorCol, bool falseAreIndicated, double[] vals, bool largeIsGood,
			PerformanceColumnType[] types, string name, List<double[]> expCols, List<string> expColNames,
			bool includeScore){
			(double[][] columns, int[] order) =
				PerformanceColumnType.CalcCurves(indicatorCol, falseAreIndicated, vals, largeIsGood, types);
			string[] columnNames = new string[types.Length];
			for (int i = 0; i < types.Length; i++){
				columnNames[i] = name + " " + types[i].Name;
			}
			expColNames.AddRange(columnNames);
			expCols.AddRange(columns);
			if (includeScore){
				expColNames.Add("Score");
				expCols.Add(vals.SubArray(order));
			}
		}

		private static double[,] ToMatrix(IList<double[]> x){
			double[,] result = new double[x[0].Length, x.Count];
			for (int i = 0; i < result.GetLength(0); i++){
				for (int j = 0; j < result.GetLength(1); j++){
					result[i, j] = x[j][i];
				}
			}
			return result;
		}

		public static bool[] GetIndicatorColumn(int catColInd, string word, IMatrixData data){
			string[][] catCol = data.GetCategoryColumnAt(catColInd);
			bool[] result = new bool[data.RowCount];
			for (int i = 0; i < result.Length; i++){
				string[] cats = catCol[i];
				Array.Sort(cats);
				bool contains = Array.BinarySearch(cats, word) >= 0;
				result[i] = contains;
			}
			return result;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			string[] numChoice = ArrayUtils.Concat(mdata.NumericColumnNames, mdata.ColumnNames);
			return new Parameters(
				new SingleChoiceParam("Indicated are"){
					Values = new[]{"False", "True"},
					Help = "Specify whether rows containing the 'Indicator' are true or false."
				},
				new SingleChoiceParam("In column"){
					Values = mdata.CategoryColumnNames, Help = "The categorical column containing the 'Indicator'."
				},
				new StringParam("Indicator"){
					Value = "+",
					Help =
						"The string that will be searched in the above specified categorical column to define which rows are right or wrong predicted."
				},
				new MultiChoiceParam("Scores"){
					Value = new[]{0},
					Values = numChoice,
					Help =
						"The expression columns that contain the classification scores by which the rows will be ranked."
				},
				new BoolParam("Large values are good"){
					Value = true,
					Help =
						"If checked, large score values are considered good, otherwise the lower the score value the better."
				},
				new MultiChoiceParam("Display quantity"){
					Values = PerformanceColumnType.AllTypeNames, Help = "The quantities that should be calculated."
				});
		}
	}
}