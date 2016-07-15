using System.Collections.Generic;
using BaseLib.Graphic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Properties;

namespace PerseusPluginLib.Impute{
	public class ReplaceMissingFromGaussian : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => GraphUtils.ToBitmap2(Resources.histo);
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
			if (separateColumns){
				ReplaceMissingsByGaussianByColumn(width, shift, mdata, cols);
			} else{
				ReplaceMissingsByGaussianWholeMatrix(width, shift, mdata, cols);
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
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
					new MultiChoiceParam("Columns", ArrayUtils.ConsecutiveInts(mdata.ColumnCount)){Values = mdata.ColumnNames}
				});
		}

		public static void ReplaceMissingsByGaussianByColumn(double width, double shift, IMatrixData data, int[] colInds){
			List<int> valid = new List<int>();
			Random2 r = new Random2();
			foreach (int colInd in colInds){
				bool success = ReplaceMissingsByGaussianForOneColumn(width, shift, data, colInd, r);
				if (success){
					valid.Add(colInd);
				}
			}
			if (valid.Count != data.ColumnCount){
				data.ExtractColumns(valid.ToArray());
			}
		}

		private static bool ReplaceMissingsByGaussianForOneColumn(double width, double shift, IMatrixData data, int colInd,
			Random2 r){
			List<float> allValues = new List<float>();
			for (int i = 0; i < data.RowCount; i++){
				float x = data.Values.Get(i, colInd);
				if (!float.IsNaN(x) && !float.IsInfinity(x)){
					allValues.Add(x);
				}
			}
			double stddev;
			double mean = ArrayUtils.MeanAndStddev(allValues.ToArray(), out stddev);
			if (double.IsNaN(mean) || double.IsInfinity(mean) || double.IsNaN(stddev) || double.IsInfinity(stddev)){
				return false;
			}
			double m = mean - shift*stddev;
			double s = stddev*width;
			for (int i = 0; i < data.RowCount; i++){
				if (float.IsNaN(data.Values.Get(i, colInd)) || float.IsInfinity(data.Values.Get(i, colInd))){
					data.Values.Set(i, colInd, (float) r.NextGaussian(m, s));
					data.IsImputed[i, colInd] = true;
				}
			}
			return true;
		}

		public static void ReplaceMissingsByGaussianWholeMatrix(double width, double shift, IMatrixData data, int[] colInds){
			List<float> allValues = new List<float>();
			for (int i = 0; i < data.RowCount; i++){
				foreach (int t in colInds){
					float x = data.Values.Get(i, t);
					if (!float.IsNaN(x) && !float.IsInfinity(x)){
						allValues.Add(x);
					}
				}
			}
			double stddev;
			double mean = ArrayUtils.MeanAndStddev(allValues.ToArray(), out stddev);
			double m = mean - shift*stddev;
			double s = stddev*width;
			Random2 r = new Random2();
			for (int i = 0; i < data.RowCount; i++){
				foreach (int t in colInds){
					if (float.IsNaN(data.Values.Get(i, t)) || float.IsInfinity(data.Values.Get(i, t))){
						data.Values.Set(i, t, (float) r.NextGaussian(m, s));
						data.IsImputed[i, t] = true;
					}
				}
			}
		}
	}
}