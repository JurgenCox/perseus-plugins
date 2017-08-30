using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Impute{
	public class ReplaceMissingFromGaussian : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("histo.png");
		public string HelpOutput => "";
		public int NumSupplTables => 0;
		public string[] HelpSupplTables => new string[0];
		public string Name => "Replace missing values from normal distribution";
		public string Heading => "Imputation";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Imputation:ReplaceMissingFromGaussian";

		public string Description
			=>
				"Missing values will be replaced by random numbers that are drawn from a normal distribution. The parameters of this" +
				" distribution can be optimized to simulate a typical abundance region that the missing values would have if they " +
				"had been measured. In the absence of any a priori knowledge, the distribution of random numbers should be " +
				"similar to the valid values. Often, missing values represent low abundance measurements. The default " +
				"values are chosen to mimic this case.";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			double width = param.GetParam<double>("Width").Value;
			double shift = param.GetParam<double>("Down shift").Value;
			bool separateColumns = param.GetParam<int>("Mode").Value == 1;
			int[] cols = param.GetParam<int[]>("Columns").Value;
			if (cols.Length == 0){
				return;
			}
			if (separateColumns){
				ReplaceMissingsByGaussianByColumn(width, shift, mdata, cols);
			} else{
				string err = ReplaceMissingsByGaussianWholeMatrix(width, shift, mdata, cols);
				if (err != null){
					processInfo.ErrString = err;
				}
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(
					new DoubleParam("Width", 0.3){
						Help =
							"The width of the gaussian distibution relative to the standard deviation of measured values. A value of 0.5 " +
							"would mean that the width of the distribution used for drawing random numbers is half of the standard " +
							"deviation of the data."
					},
					new DoubleParam("Down shift", 1.8){
						Help =
							"The amount by which the distribution used for the random numbers is shifted downward. This is in units of the" +
							" standard deviation of the valid data."
					},
					new SingleChoiceParam("Mode", 1){Values = new[]{"Total matrix", "Separately for each column"}},
					new MultiChoiceParam("Columns", ArrayUtils.ConsecutiveInts(mdata.ColumnCount)){
						Values = ArrayUtils.Concat(mdata.ColumnNames, mdata.NumericColumnNames)
					});
		}

		public static void ReplaceMissingsByGaussianByColumn(double width, double shift, IMatrixData data, int[] colInds){
			List<int> invalidMain = new List<int>();
			Random2 r = new Random2(7);
			foreach (int colInd in colInds){
				bool success = ReplaceMissingsByGaussianForOneColumn(width, shift, data, colInd, r);
				if (!success){
					if (colInd < data.ColumnCount){
						invalidMain.Add(colInd);
					}
				}
			}
			if (invalidMain.Count > 0){
				data.ExtractColumns(ArrayUtils.Complement(invalidMain, data.ColumnCount));
			}
		}

		private static bool ReplaceMissingsByGaussianForOneColumn(double width, double shift, IMatrixData data, int colInd,
			Random2 r){
			List<double> allValues = new List<double>();
			for (int i = 0; i < data.RowCount; i++){
				double x = GetValue(data, i, colInd);
				if (!double.IsNaN(x) && !double.IsInfinity(x)){
					allValues.Add(x);
				}
			}
			double mean = ArrayUtils.MeanAndStddev(allValues.ToArray(), out double stddev);
			if (double.IsNaN(mean) || double.IsInfinity(mean) || double.IsNaN(stddev) || double.IsInfinity(stddev)){
				return false;
			}
			double m = mean - shift*stddev;
			double s = stddev*width;
			for (int i = 0; i < data.RowCount; i++){
				double x = GetValue(data, i, colInd);
				if (double.IsNaN(x) || double.IsInfinity(x)){
					if (colInd < data.ColumnCount){
						data.Values.Set(i, colInd, r.NextGaussian(m, s));
						data.IsImputed[i, colInd] = true;
					} else{
						data.NumericColumns[colInd - data.ColumnCount][i] = r.NextGaussian(m, s);
					}
				}
			}
			return true;
		}

		private static double GetValue(IMatrixData data, int i, int colInd){
			if (colInd < data.ColumnCount){
				return data.Values.Get(i, colInd);
			}
			colInd -= data.ColumnCount;
			return data.NumericColumns[colInd][i];
		}

		public static string ReplaceMissingsByGaussianWholeMatrix(double width, double shift, IMatrixData data, int[] colInds){
			List<double> allValues = new List<double>();
			for (int i = 0; i < data.RowCount; i++){
				foreach (int t in colInds){
					double x = GetValue(data, i, t);
					if (!double.IsNaN(x) && !double.IsInfinity(x)){
						allValues.Add(x);
					}
				}
			}
			double mean = ArrayUtils.MeanAndStddev(allValues.ToArray(), out double stddev);
			if (double.IsNaN(mean) || double.IsInfinity(mean) || double.IsNaN(stddev) || double.IsInfinity(stddev)){
				return "Imputation failed since mean and standard deviation could not be calculated.";
			}
			double m = mean - shift*stddev;
			double s = stddev*width;
			Random2 r = new Random2(7);
			for (int i = 0; i < data.RowCount; i++){
				foreach (int colInd in colInds){
					double x = GetValue(data, i, colInd);
					if (double.IsNaN(x) || double.IsInfinity(x)){
						if (colInd < data.ColumnCount){
							data.Values.Set(i, colInd, r.NextGaussian(m, s));
							data.IsImputed[i, colInd] = true;
						} else{
							data.NumericColumns[colInd - data.ColumnCount][i] = r.NextGaussian(m, s);
						}
					}
				}
			}
			return null;
		}
	}
}