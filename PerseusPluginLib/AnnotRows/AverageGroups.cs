using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.AnnotRows{
	public class AverageGroups : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("average.png");
		public string HelpOutput => "Averaged main matrix containing as many columns as there were groups defined.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Average groups";
		public string Heading => "Annot. rows";
		public bool IsActive => true;
		public float DisplayRank => 3;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotrows:AverageGroups"
			;

		public string Description
			=> "Main columns are averaged over groups. This requires that at least one categorical annotation row is defined.";

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			if (mdata.CategoryRowCount == 0){
				errorString = "No grouping is loaded.";
				return null;
			}
			return new Parameters(new SingleChoiceParam("Grouping"){Values = mdata.CategoryRowNames},
				new SingleChoiceParam("Average type"){
					Values = new[]{"Median", "Mean", "Sum", "Geometric mean"},
					Help = "Select whether median or mean should be used for the averaging."
				},
				new IntParam("Min. valid values per group", 1), new BoolParam("Keep original data", false),
				new SingleChoiceParam("Add variation"){
					Values = new[]{"<None>", "Standard deviation", "Error of mean"},
					Help = "Specify here if a measure of group-wise variation should be added as numerical columns."
				});
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int avType = param.GetParam<int>("Average type").Value;
			if (mdata.CategoryRowCount == 0){
				processInfo.ErrString = "No category rows were loaded.";
				return;
			}
			int groupColInd = param.GetParam<int>("Grouping").Value;
			int validVals = param.GetParam<int>("Min. valid values per group").Value;
			bool keep = param.GetParam<bool>("Keep original data").Value;
			int varInd = param.GetParam<int>("Add variation").Value - 1;
			bool sdev = varInd >= 0;
			Func<IList<double>, double> func;
			switch (avType){
				case 0:
					func = ArrayUtils.Median;
					break;
				case 1:
					func = ArrayUtils.Mean;
					break;
				case 2:
					func = ArrayUtils.Sum;
					break;
				case 3:
					func = ArrayUtils.GeometricMean;
					break;
				default:
					throw new Exception("Never get here.");
			}
			if (sdev){
				AddStandardDeviation(groupColInd, validVals, mdata, varInd);
			}
			if (keep){
				FillMatrixKeep(groupColInd, validVals, mdata, func);
			} else{
				FillMatrixDontKeep(groupColInd, validVals, mdata, func);
			}
		}

		private static void FillMatrixDontKeep(int groupColInd, int validVals, IMatrixData mdata,
			Func<IList<double>, double> func){
			string[][] groupCol = mdata.GetCategoryRowAt(groupColInd);
			string[] groupNames = ArrayUtils.UniqueValuesPreserveOrder(groupCol);
			int[][] colInds = PerseusPluginUtils.GetMainColIndices(groupCol, groupNames);
			double[,] newExCols = new double[mdata.RowCount, groupNames.Length];
			double[,] newQuality = new double[mdata.RowCount, groupNames.Length];
			bool[,] newImputed = new bool[mdata.RowCount, groupNames.Length];
			for (int i = 0; i < newExCols.GetLength(0); i++){
				for (int j = 0; j < newExCols.GetLength(1); j++){
					List<double> vals = new List<double>();
					List<bool> imps = new List<bool>();
					foreach (int ind in colInds[j]){
						double val = mdata.Values.Get(i, ind);
						if (!double.IsNaN(val) && !double.IsInfinity(val)){
							vals.Add(val);
							imps.Add(mdata.IsImputed[i, ind]);
						}
					}
					bool imp = false;
					double xy = double.NaN;
					if (vals.Count >= validVals){
						xy = func(vals);
						imp = ArrayUtils.Or(imps);
					}
					newExCols[i, j] = xy;
					newQuality[i, j] = double.NaN;
					newImputed[i, j] = imp;
				}
			}
			mdata.ColumnNames = new List<string>(groupNames);
			mdata.ColumnDescriptions = GetEmpty(groupNames);
			mdata.Values.Set(newExCols);
			mdata.Quality.Set(newQuality);
			mdata.IsImputed.Set(newImputed);
			mdata.RemoveCategoryRowAt(groupColInd);
			for (int i = 0; i < mdata.CategoryRowCount; i++){
				mdata.SetCategoryRowAt(AverageCategoryRow(mdata.GetCategoryRowAt(i), colInds), i);
			}
			for (int i = 0; i < mdata.NumericRows.Count; i++){
				mdata.NumericRows[i] = AverageNumericRow(mdata.NumericRows[i], colInds);
			}
		}

		public static List<string> GetEmpty(string[] x){
			List<string> result = new List<string>();
			for (int i = 0; i < x.Length; i++){
				result.Add("");
			}
			return result;
		}

		private static void AddStandardDeviation(int groupColInd, int validVals, IMatrixData mdata, int varInd){
			string[][] groupCol = mdata.GetCategoryRowAt(groupColInd);
			string[] groupNames = ArrayUtils.UniqueValuesPreserveOrder(groupCol);
			int[][] colInds = PerseusPluginUtils.GetMainColIndices(groupCol, groupNames);
			double[][] newNumCols = new double[groupNames.Length][];
			for (int i = 0; i < newNumCols.Length; i++){
				newNumCols[i] = new double[mdata.RowCount];
			}
			for (int i = 0; i < mdata.RowCount; i++){
				for (int j = 0; j < groupNames.Length; j++){
					List<double> vals = new List<double>();
					foreach (int ind in colInds[j]){
						double val = mdata.Values.Get(i, ind);
						if (!double.IsNaN(val) && !double.IsInfinity(val)){
							vals.Add(val);
						}
					}
					double xy = double.NaN;
					if (vals.Count >= validVals){
						if (varInd == 0){
							xy = ArrayUtils.StandardDeviation(vals);
						} else{
							xy = ArrayUtils.StandardDeviation(vals)/Math.Sqrt(vals.Count);
						}
					}
					newNumCols[j][i] = xy;
				}
			}
			for (int i = 0; i < groupNames.Length; i++){
				string name = "stddev " + groupNames[i];
				mdata.AddNumericColumn(name, name, newNumCols[i]);
			}
		}

		private static void FillMatrixKeep(int groupColInd, int validVals, IMatrixData mdata, Func<IList<double>, double> func){
			string[][] groupCol = mdata.GetCategoryRowAt(groupColInd);
			string[] groupNames = ArrayUtils.UniqueValuesPreserveOrder(groupCol);
			int[][] colInds = PerseusPluginUtils.GetMainColIndices(groupCol, groupNames);
			double[][] newNumCols = new double[groupNames.Length][];
			for (int i = 0; i < newNumCols.Length; i++){
				newNumCols[i] = new double[mdata.RowCount];
			}
			for (int i = 0; i < mdata.RowCount; i++){
				for (int j = 0; j < groupNames.Length; j++){
					List<double> vals = new List<double>();
					foreach (int ind in colInds[j]){
						double val = mdata.Values.Get(i, ind);
						if (!double.IsNaN(val) && !double.IsInfinity(val)){
							vals.Add(val);
						}
					}
					double xy = double.NaN;
					if (vals.Count >= validVals){
						xy = func(vals);
					}
					newNumCols[j][i] = xy;
				}
			}
			for (int i = 0; i < groupNames.Length; i++){
				mdata.AddNumericColumn(groupNames[i], groupNames[i], newNumCols[i]);
			}
		}

		private static double[] AverageNumericRow(IList<double> numericRow, IList<int[]> colInds){
			double[] result = new double[colInds.Count];
			for (int i = 0; i < result.Length; i++){
				result[i] = ArrayUtils.Mean(ArrayUtils.SubArray(numericRow, colInds[i]));
			}
			return result;
		}

		private static string[][] AverageCategoryRow(IList<string[]> categoryRow, IList<int[]> colInds){
			string[][] result = new string[colInds.Count][];
			for (int i = 0; i < result.Length; i++){
				result[i] = ArrayUtils.UniqueValues(ArrayUtils.Concat(ArrayUtils.SubArray(categoryRow, colInds[i])));
			}
			return result;
		}
	}
}