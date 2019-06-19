using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Basic {
	public class DensityEstimationProcessing : IMatrixProcessing {
		public string Name => "Density estimation";
		public float DisplayRank => -3;
		public bool IsActive => true;
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("density.Image.png");
		public string Heading => "Basic";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url =>
			"http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:DensityEstimationProcessing";

		public string Description =>
			"The density of data points in two dimensions is calculated. Each data point is smoothed out" +
			" by a suitable Gaussian kernel.";

		public string HelpOutput =>
			"A copy of the input matrix with two numerical columns added containing the density information.";

		public int GetMaxThreads(Parameters parameters) {
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo) {
			int[] colIndx = param.GetParam<int[]>("x").Value;
			int[] colIndy = param.GetParam<int[]>("y").Value;
			if (colIndx.Length == 0) {
				processInfo.ErrString = "Please select some columns";
				return;
			}
			if (colIndx.Length != colIndy.Length) {
				processInfo.ErrString =
					"Please select the same number of columns in the boxes for the first and second columns.";
				return;
			}
			int typeInd = param.GetParam<int>("Distribution type").Value;
			int points = param.GetParam<int>("Number of points").Value;
			for (int k = 0; k < colIndx.Length; k++) {
				double[] xvals = GetColumn(mdata, colIndx[k]);
				double[] yvals = GetColumn(mdata, colIndy[k]);
				DensityEstimationType type = DensityEstimationType.JointDistribution;
				switch (typeInd) {
					case 1:
						type = DensityEstimationType.DivideByX;
						break;
					case 2:
						type = DensityEstimationType.DivideByY;
						break;
					case 3:
						type = DensityEstimationType.DivideByXY;
						break;
				}
				(double[] dvals, double[] pvals) = DensityEstimation.CalcDensitiesAtData(xvals, yvals, points, type);
				string xname = GetColumnName(mdata, colIndx[k]);
				string yname = GetColumnName(mdata, colIndy[k]);
				mdata.AddNumericColumn("Density_" + xname + "_" + yname,
					"Density of data points in the plane spanned by the columns " + xname + " and " + yname + ".",
					dvals);
				mdata.AddNumericColumn("Excluded fraction_" + xname + "_" + yname,
					"Percentage of points with a point density smaller than at this point in the plane spanned by the columns " +
					xname + " and " + yname + ".", pvals);
			}
		}

		private static double[] GetColumn(IMatrixData matrixData, int ind) {
			if (ind < matrixData.ColumnCount) {
				return matrixData.Values.GetColumn(ind).ToArray();
			}
			double[] x = matrixData.NumericColumns[ind - matrixData.ColumnCount];
			double[] f = new double[x.Length];
			for (int i = 0; i < x.Length; i++) {
				f[i] = x[i];
			}
			return f;
		}

		private static string GetColumnName(IMatrixData matrixData, int ind) {
			return ind < matrixData.ColumnCount
				? matrixData.ColumnNames[ind]
				: matrixData.NumericColumnNames[ind - matrixData.ColumnCount];
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString) {
			string[] vals = ArrayUtils.Concat(mdata.ColumnNames, mdata.NumericColumnNames);
			int[] sel1 = vals.Length > 0 ? new[] {0} : new int[0];
			int[] sel2 = vals.Length > 1 ? new[] {1} : (vals.Length > 0 ? new[] {0} : new int[0]);
			return new Parameters(
				new MultiChoiceParam("x", sel1) {
					Values = vals,
					Repeats = true,
					Help =
						"Columns for the first dimension. Multiple choices can be made leading to the creation of multiple density maps."
				},
				new MultiChoiceParam("y", sel2) {
					Values = vals,
					Repeats = true,
					Help =
						"Columns for the second dimension. The number has to be the same as for the 'Column 1' parameter."
				},
				new IntParam("Number of points", 300) {
					Help =
						"This parameter defines the resolution of the density map. It specifies the number of pixels per dimension. Large " +
						"values may lead to increased computing times."
				},
				new SingleChoiceParam("Distribution type") {
					Values = new[] {"P(x,y)", "P(y|x)", "P(x|y)", "P(x,y)/(P(x)*P(y))"}
				});
		}
	}
}