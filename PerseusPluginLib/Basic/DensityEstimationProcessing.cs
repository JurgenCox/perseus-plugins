using System.Collections.Generic;
using BaseLib.Graphic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Properties;

namespace PerseusPluginLib.Basic{
	public class DensityEstimationProcessing : IMatrixProcessing{
		public string Name => "Density estimation";
		public float DisplayRank => -3;
		public bool IsActive => true;
		public bool HasButton => true;
		public Bitmap2 DisplayImage => GraphUtils.ToBitmap2(Resources.density_Image);
		public string Heading => "Basic";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:DensityEstimationProcessing";

		public string Description
			=>
				"The density of data points in two dimensions is calculated. Each data point is smoothed out" +
				" by a suitable Gaussian kernel.";

		public string HelpOutput
			=> "A copy of the input matrix with two numerical columns added containing the density information.";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] colIndx = param.GetParam<int[]>("x").Value;
			int[] colIndy = param.GetParam<int[]>("y").Value;
			if (colIndx.Length == 0){
				processInfo.ErrString = "Please select some columns";
				return;
			}
			if (colIndx.Length != colIndy.Length){
				processInfo.ErrString = "Please select the same number of columns in the boxes for the first and second columns.";
				return;
			}
			int typeInd = param.GetParam<int>("Distribution type").Value;
			int points = param.GetParam<int>("Number of points").Value;
			for (int k = 0; k < colIndx.Length; k++){
				float[] xvals = GetColumn(mdata, colIndx[k]);
				float[] yvals = GetColumn(mdata, colIndy[k]);
				float[] xvals1;
				float[] yvals1;
				GetValidPairs(xvals, yvals, out xvals1, out yvals1);
				double xmin;
				double xmax;
				double ymin;
				double ymax;
				DensityEstimation.CalcRanges(xvals1, yvals1, out xmin, out xmax, out ymin, out ymax);
				float[,] values = DensityEstimation.GetValuesOnGrid(xvals1, xmin, (xmax - xmin)/points, points, yvals1, ymin,
					(ymax - ymin)/points, points);
				if (typeInd == 1){
					MakeConditional1(values);
				}
				if (typeInd == 2){
					MakeConditional2(values);
				}
				if (typeInd == 3){
					MakeConditional3(values);
				}
				DensityEstimation.DivideByMaximum(values);
				double[] xmat = new double[points];
				for (int i = 0; i < points; i++){
					xmat[i] = xmin + i*(xmax - xmin)/points;
				}
				double[] ymat = new double[points];
				for (int i = 0; i < points; i++){
					ymat[i] = ymin + i*(ymax - ymin)/points;
				}
				float[,] percvalues = CalcExcludedPercentage(values);
				double[] dvals = new double[xvals.Length];
				double[] pvals = new double[xvals.Length];
				for (int i = 0; i < dvals.Length; i++){
					double xx = xvals[i];
					double yy = yvals[i];
					if (!double.IsNaN(xx) && !double.IsNaN(yy)){
						int xind = ArrayUtils.ClosestIndex(xmat, xx);
						int yind = ArrayUtils.ClosestIndex(ymat, yy);
						dvals[i] = values[xind, yind];
						pvals[i] = percvalues[xind, yind];
					} else{
						dvals[i] = double.NaN;
						pvals[i] = double.NaN;
					}
				}
				string xname = GetColumnName(mdata, colIndx[k]);
				string yname = GetColumnName(mdata, colIndy[k]);
				mdata.AddNumericColumn("Density_" + xname + "_" + yname,
					"Density of data points in the plane spanned by the columns " + xname + " and " + yname + ".", dvals);
				mdata.AddNumericColumn("Excluded fraction_" + xname + "_" + yname,
					"Percentage of points with a point density smaller than at this point in the plane spanned by the columns " + xname +
					" and " + yname + ".", pvals);
			}
		}

		private static void GetValidPairs(IList<float> x, IList<float> y, out float[] x1, out float[] y1){
			List<float> x2 = new List<float>();
			List<float> y2 = new List<float>();
			for (int i = 0; i < x.Count; i++){
				if (!float.IsNaN(x[i]) && !float.IsInfinity(x[i]) && !float.IsNaN(y[i]) && !float.IsInfinity(y[i])){
					x2.Add(x[i]);
					y2.Add(y[i]);
				}
			}
			x1 = x2.ToArray();
			y1 = y2.ToArray();
		}

		private static void MakeConditional1(float[,] values){
			float[] m = new float[values.GetLength(0)];
			for (int i = 0; i < m.Length; i++){
				for (int j = 0; j < values.GetLength(1); j++){
					m[i] += values[i, j];
				}
			}
			for (int i = 0; i < m.Length; i++){
				for (int j = 0; j < values.GetLength(1); j++){
					values[i, j] /= m[i];
				}
			}
		}

		private static void MakeConditional2(float[,] values){
			float[] m = new float[values.GetLength(1)];
			for (int i = 0; i < m.Length; i++){
				for (int j = 0; j < values.GetLength(0); j++){
					m[i] += values[j, i];
				}
			}
			for (int i = 0; i < m.Length; i++){
				for (int j = 0; j < values.GetLength(0); j++){
					values[j, i] /= m[i];
				}
			}
		}

		private static void MakeConditional3(float[,] values){
			float[] m1 = new float[values.GetLength(0)];
			float[] m2 = new float[values.GetLength(2)];
			for (int i = 0; i < m1.Length; i++){
				for (int j = 0; j < values.GetLength(1); j++){
					m1[i] += values[i, j];
					m2[j] += values[i, j];
				}
			}
			for (int i = 0; i < m1.Length; i++){
				for (int j = 0; j < values.GetLength(1); j++){
					values[i, j] /= m1[i]*m2[j];
				}
			}
		}

		private static float[] GetColumn(IMatrixData matrixData, int ind){
			if (ind < matrixData.ColumnCount){
				return ArrayUtils.ToFloats(matrixData.Values.GetColumn(ind));
			}
			double[] x = matrixData.NumericColumns[ind - matrixData.ColumnCount];
			float[] f = new float[x.Length];
			for (int i = 0; i < x.Length; i++){
				f[i] = (float) x[i];
			}
			return f;
		}

		private static string GetColumnName(IMatrixData matrixData, int ind){
			return ind < matrixData.ColumnCount
				? matrixData.ColumnNames[ind]
				: matrixData.NumericColumnNames[ind - matrixData.ColumnCount];
		}

		private static float[,] CalcExcludedPercentage(float[,] values){
			int n0 = values.GetLength(0);
			int n1 = values.GetLength(1);
			float[] v = new float[n0*n1];
			int[] ind0 = new int[n0*n1];
			int[] ind1 = new int[n0*n1];
			int count = 0;
			for (int i0 = 0; i0 < n0; i0++){
				for (int i1 = 0; i1 < n1; i1++){
					v[count] = values[i0, i1];
					ind0[count] = i0;
					ind1[count] = i1;
					count++;
				}
			}
			int[] o = ArrayUtils.Order(v);
			v = ArrayUtils.SubArray(v, o);
			ind0 = ArrayUtils.SubArray(ind0, o);
			ind1 = ArrayUtils.SubArray(ind1, o);
			double total = 0;
			foreach (float t in v){
				total += t;
			}
			float[,] result = new float[n0, n1];
			double sum = 0;
			for (int i = 0; i < v.Length; i++){
				result[ind0[i], ind1[i]] = (float) (sum/total);
				sum += v[i];
			}
			return result;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			string[] vals = ArrayUtils.Concat(mdata.ColumnNames, mdata.NumericColumnNames);
			int[] sel1 = vals.Length > 0 ? new[]{0} : new int[0];
			int[] sel2 = vals.Length > 1 ? new[]{1} : (vals.Length > 0 ? new[]{0} : new int[0]);
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("x", sel1){
						Values = vals,
						Repeats = true,
						Help =
							"Colums for the first dimension. Multiple choices can be made leading to the creation of multiple density maps."
					},
					new MultiChoiceParam("y", sel2){
						Values = vals,
						Repeats = true,
						Help = "Colums for the second dimension. The number has to be the same as for the 'Column 1' parameter."
					},
					new IntParam("Number of points", 300){
						Help =
							"This parameter defines the resolution of the density map. It specifies the number of pixels per dimension. Large " +
							"values may lead to increased computing times."
					},
					new SingleChoiceParam("Distribution type"){Values = new[]{"P(x,y)", "P(y|x)", "P(x|y)", "P(x,y)/(P(x)*P(y))"}}
				});
		}
	}
}