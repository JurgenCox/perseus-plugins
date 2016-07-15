using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Basic{
	public class SummaryStatisticsColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Summary statistics (columns)";
		public string Heading => "Basic";
		public bool IsActive => true;
		public float DisplayRank => -6;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Description
			=>
				"A set of simple descriptive quantities are calculated that help summarizing the data in the selected expression " +
				"or numerical columns.";

		public string HelpOutput
			=>
				"A new matrix is created where each row corresponds to one of the selected summary statistic " +
				"types. 'NaN' and 'Infinity' values are ignored for all calculations.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:SummaryStatisticsColumns"
			;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new List<Parameter>{
					new MultiChoiceParam("Columns"){
						Value = ArrayUtils.ConsecutiveInts(mdata.ColumnCount),
						Values =
							ArrayUtils.Concat(ArrayUtils.Concat(mdata.ColumnNames, mdata.NumericColumnNames), mdata.MultiNumericColumnNames),
						Help = "Specify here the columns for which the summary statistics quantities should be calculated."
					},
					new MultiChoiceParam("Calculate", ArrayUtils.ConsecutiveInts(SummaryStatisticsRows.procNames.Length)){
						Values = SummaryStatisticsRows.procNames,
						Help = "Select here which quantities should be calculated."
					}
				});
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] cols = param.GetParam<int[]>("Columns").Value;
			HashSet<int> w = ArrayUtils.ToHashSet(param.GetParam<int[]>("Calculate").Value);
			bool[] include = new bool[SummaryStatisticsRows.procs.Length];
			double[][] rowws = new double[SummaryStatisticsRows.procs.Length][];
			for (int i = 0; i < include.Length; i++){
				include[i] = w.Contains(i);
				if (include[i]){
					rowws[i] = new double[cols.Length];
				}
			}
			for (int i = 0; i < cols.Length; i++){
				double[] vals = GetColumn(cols[i], mdata);
				for (int j = 0; j < include.Length; j++){
					if (include[j]){
						rowws[j][i] = SummaryStatisticsRows.procs[j].Item2(vals);
					}
				}
			}
			List<double[]> ex = new List<double[]>();
			List<string> names = new List<string>();
			for (int i = 0; i < include.Length; i++){
				if (include[i]){
					ex.Add(rowws[i]);
					names.Add(SummaryStatisticsRows.procs[i].Item1);
				}
			}
			float[,] exVals = GetExVals(ex);
			string[] colNames = GetColNames(mdata, cols);
			mdata.Name = "Summary";
			mdata.ColumnNames = new List<string>(names.ToArray());
			mdata.ColumnDescriptions = new List<string>(names.ToArray());
			mdata.Values.Set(exVals);
			mdata.SetAnnotationColumns(new List<string>(new[]{"Columns"}), new List<string[]>(new[]{colNames}),
				mdata.CategoryRowNames, TransformCategories(mdata, cols, mdata.ColumnCount), mdata.NumericRowNames,
				TransformNumeric(mdata.NumericRows, cols, mdata.ColumnCount), new List<string>(), new List<double[][]>());
			mdata.ClearAnnotationRows();
		}

		private static List<double[]> TransformNumeric(IEnumerable<double[]> numericRows, IList<int> cols, int n){
			List<double[]> result = new List<double[]>();
			foreach (double[] numericRow in numericRows){
				result.Add(TransformNumeric(numericRow, cols, n));
			}
			return result;
		}

		private static double[] TransformNumeric(IList<double> numericRows, IList<int> cols, int n){
			double[] result = new double[cols.Count];
			for (int i = 0; i < cols.Count; i++){
				if (cols[i] < n){
					result[i] = numericRows[cols[i]];
				} else{
					result[i] = double.NaN;
				}
			}
			return result;
		}

		private static List<string[][]> TransformCategories(IMatrixData mdata, IList<int> cols, int n){
			List<string[][]> result = new List<string[][]>();
			for (int index = 0; index < mdata.CategoryRowCount; index++){
				string[][] categoryRow = mdata.GetCategoryRowAt(index);
				result.Add(TransformCategories(categoryRow, cols, n));
			}
			return result;
		}

		private static string[][] TransformCategories(IList<string[]> categoryRow, IList<int> cols, int n){
			string[][] result = new string[cols.Count][];
			for (int i = 0; i < cols.Count; i++){
				if (cols[i] < n){
					result[i] = categoryRow[cols[i]];
				} else{
					result[i] = new string[0];
				}
			}
			return result;
		}

		private static string[] GetColNames(IMatrixData mdata, IEnumerable<int> cols){
			List<string> result = new List<string>();
			foreach (int col in cols){
				result.Add(GetColName(mdata, col));
			}
			return result.ToArray();
		}

		private static string GetColName(IMatrixData mdata, int col){
			if (col < mdata.ColumnCount){
				return mdata.ColumnNames[col];
			}
			col -= mdata.ColumnCount;
			if (col < mdata.NumericColumnCount){
				return mdata.NumericColumnNames[col];
			}
			col -= mdata.NumericColumnCount;
			return mdata.MultiNumericColumnNames[col];
		}

		private static double[] GetColumn(int col, IMatrixData mdata){
			if (col < mdata.ColumnCount){
				List<double> v = new List<double>();
				for (int j = 0; j < mdata.RowCount; j++){
					double x = mdata.Values.Get(j, col);
					if (!double.IsNaN(x) && !double.IsInfinity(x)){
						v.Add(x);
					}
				}
				return v.ToArray();
			}
			col -= mdata.ColumnCount;
			if (col < mdata.NumericColumnCount){
				double[] w = mdata.NumericColumns[col];
				List<double> v = new List<double>();
				foreach (double x in w){
					if (!double.IsNaN(x) && !double.IsInfinity(x)){
						v.Add(x);
					}
				}
				return v.ToArray();
			}
			col -= mdata.NumericColumnCount;
			{
				double[][] w = mdata.MultiNumericColumns[col];
				List<double> v = new List<double>();
				foreach (double[] y in w){
					foreach (double x in y){
						if (!double.IsNaN(x) && !double.IsInfinity(x)){
							v.Add(x);
						}
					}
				}
				return v.ToArray();
			}
		}

		private static float[,] GetExVals(IList<double[]> rows){
			float[,] result = new float[rows[0].Length, rows.Count];
			for (int i = 0; i < result.GetLength(1); i++){
				for (int j = 0; j < result.GetLength(0); j++){
					result[j, i] = (float) rows[i][j];
				}
			}
			return result;
		}
	}
}