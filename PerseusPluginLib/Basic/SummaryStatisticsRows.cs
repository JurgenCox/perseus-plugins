using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Basic{
	public class SummaryStatisticsRows : IMatrixProcessing{
		//TODO
		//Groeneveld & Meeden’s coefficient
		//Cyhelský's skewness coefficient
		//Distance skewness
		//L-moments
		//Trimmed mean
		//Pearson's skewness coefficients
		//Quantile based skewness measures
		internal static Tuple<string, Func<IList<double>, double>, string>[] procs = {
			new Tuple<string, Func<IList<double>, double>, string>("Sum", ArrayUtils.Sum, "Sum of all values."),
			new Tuple<string, Func<IList<double>, double>, string>("Mean", ArrayUtils.Mean,
				"Sum of all values divided by the number of values."),
			new Tuple<string, Func<IList<double>, double>, string>("Median", ArrayUtils.Median,
				"For an odd number of values the middle value is taken. For an even number of values the average of the two values in " +
				"the middle is calculated."),
			new Tuple<string, Func<IList<double>, double>, string>("Tukey biweight", ArrayUtils.TukeyBiweight, ""),
			new Tuple<string, Func<IList<double>, double>, string>("Standard deviation", ArrayUtils.StandardDeviation, ""),
			new Tuple<string, Func<IList<double>, double>, string>("Coefficient of variation", ArrayUtils.CoefficientOfVariation,
				"The coefficient of variation is defined as the standard deviation divided by the mean."),
			new Tuple<string, Func<IList<double>, double>, string>("Median absolute deviation (MAD)", ArrayUtils.MedianAbsoluteDeviation,
				"This is the median of the absolute values of the difference of each value to the median."),
			new Tuple<string, Func<IList<double>, double>, string>("Full width half maximum (FWHM)", ArrayUtils.FullWidthHalfMaximum,
				"Width of the histogram at half of the maximum value."),
			new Tuple<string, Func<IList<double>, double>, string>("Minimum", ArrayUtils.Min, ""),
			new Tuple<string, Func<IList<double>, double>, string>("Maximum", ArrayUtils.Max, ""),
			new Tuple<string, Func<IList<double>, double>, string>("Range", ArrayUtils.Range, "Maximum minus minimum."),
			new Tuple<string, Func<IList<double>, double>, string>("Valid values", x => x.Count,
				"The number of values are counted in each row that are neither infinite nor 'NaN'."),
			new Tuple<string, Func<IList<double>, double>, string>("Inter-quartile range", ArrayUtils.InterQuartileRange, ""),
			new Tuple<string, Func<IList<double>, double>, string>("1st quartile", ArrayUtils.FirstQuartile, ""),
			new Tuple<string, Func<IList<double>, double>, string>("3rd quartile", ArrayUtils.ThirdQuartile, ""),
			new Tuple<string, Func<IList<double>, double>, string>("Skewness", ArrayUtils.Skewness, ""),
			new Tuple<string, Func<IList<double>, double>, string>("Kurtosis", ArrayUtils.Kurtosis, "")
		};

		internal static string[] procNames;

		static SummaryStatisticsRows(){
			procNames = new string[procs.Length];
			for (int i = 0; i < procNames.Length; i++){
				procNames[i] = procs[i].Item1;
			}
		}

		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Summary statistics (rows)";
		public string Heading => "Basic";
		public bool IsActive => true;
		public float DisplayRank => -5;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:SummaryStatisticsRows";

		public string Description
			=> "A set of simple descriptive quantities are calculated that help summarizing the expression data in each row.";

		public string HelpOutput
			=>
				"For each selected summary statistic, a numerical column is added containing the specific quantitiy for each row of " +
				"expression values. 'NaN' and 'Infinity' values are ignored for all calculations.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			ParameterWithSubParams<int> xp = param.GetParamWithSubParams<int>("Expression column selection");
			bool groups = xp.Value == 2;
			string[] groupNames = null;
			int[][] colIndsGroups = null;
			if (groups){
				int groupRowInd = xp.GetSubParameters().GetParam<int>("Group").Value;
				string[][] groupCol = mdata.GetCategoryRowAt(groupRowInd);
				groupNames = ArrayUtils.UniqueValuesPreserveOrder(groupCol);
				colIndsGroups = PerseusPluginUtils.GetMainColIndices(groupCol, groupNames);
			}
			int[] useCols = xp.Value == 1
				? xp.GetSubParameters().GetParam<int[]>("Columns").Value
				: ArrayUtils.ConsecutiveInts(mdata.ColumnCount);
			HashSet<int> w = ArrayUtils.ToHashSet(param.GetParam<int[]>("Calculate").Value);
			bool[] include = new bool[procs.Length];
			double[][] columns = new double[procs.Length][];
			double[][][] columnsG = null;
			if (groups){
				columnsG = new double[procs.Length][][];
				for (int i = 0; i < columnsG.Length; i++){
					columnsG[i] = new double[groupNames.Length][];
				}
			}
			for (int i = 0; i < include.Length; i++){
				include[i] = w.Contains(i);
				if (include[i]){
					columns[i] = new double[mdata.RowCount];
					if (groups){
						for (int j = 0; j < groupNames.Length; j++){
							columnsG[i][j] = new double[mdata.RowCount];
						}
					}
				}
			}
			for (int i = 0; i < mdata.RowCount; i++){
				List<double> v = new List<double>();
				foreach (int j in useCols){
					double x = mdata.Values.Get(i, j);
					if (!double.IsNaN(x) && !double.IsInfinity(x)){
						v.Add(x);
					}
				}
				for (int j = 0; j < include.Length; j++){
					if (include[j]){
						columns[j][i] = procs[j].Item2(v);
					}
				}
				if (groups){
					List<double>[] vg = new List<double>[groupNames.Length];
					for (int j = 0; j < colIndsGroups.Length; j++){
						vg[j] = new List<double>();
						for (int k = 0; k < colIndsGroups[j].Length; k++){
							double x = mdata.Values.Get(i, colIndsGroups[j][k]);
							if (!double.IsNaN(x) && !double.IsInfinity(x)){
								vg[j].Add(x);
							}
						}
					}
					for (int j = 0; j < include.Length; j++){
						if (include[j]){
							for (int k = 0; k < groupNames.Length; k++){
								columnsG[j][k][i] = procs[j].Item2(vg[k]);
							}
						}
					}
				}
			}
			for (int i = 0; i < include.Length; i++){
				if (include[i]){
					mdata.AddNumericColumn(procs[i].Item1, procs[i].Item3, columns[i]);
					if (groups){
						for (int k = 0; k < groupNames.Length; k++){
							mdata.AddNumericColumn(procs[i].Item1 + " " + groupNames[k], procs[i].Item3, columnsG[i][k]);
						}
					}
				}
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new List<Parameter>{
					new SingleChoiceWithSubParams("Expression column selection"){
						Values = new[]{"Use all expression columns", "Select columns", "Within groups"},
						SubParams =
							new[]{
								new Parameters(),
								new Parameters(new MultiChoiceParam("Columns", ArrayUtils.ConsecutiveInts(mdata.ColumnCount)){
									Values = mdata.ColumnNames,
									Repeats = false
								}),
								new Parameters(new SingleChoiceParam("Group"){Values = mdata.CategoryRowNames})
							},
						ParamNameWidth = 136,
						TotalWidth = 731
					},
					new MultiChoiceParam("Calculate", ArrayUtils.ConsecutiveInts(procNames.Length)){Values = procNames}
				});
		}
	}
}