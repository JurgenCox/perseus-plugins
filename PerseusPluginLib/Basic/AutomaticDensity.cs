using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Basic{
	public class AutomaticDensity{
		private static double[] GetColumn(IMatrixData matrixData, int ind){
			if (ind < matrixData.ColumnCount){
				return matrixData.Values.GetColumn(ind).ToArray();
			}
			double[] x = matrixData.NumericColumns[ind - matrixData.ColumnCount];
			double[] f = new double[x.Length];
			for (int i = 0; i < x.Length; i++){
				f[i] = x[i];
			}
			return f;
		}

		private static void GetValidPairs(IList<double> x, IList<double> y, out double[] x1, out double[] y1){
			List<double> x2 = new List<double>();
			List<double> y2 = new List<double>();
			for (int i = 0; i < x.Count; i++){
				if (!double.IsNaN(x[i]) && !double.IsInfinity(x[i]) && !double.IsNaN(y[i]) && !double.IsInfinity(y[i])){
					x2.Add(x[i]);
					y2.Add(y[i]);
				}
			}
			x1 = x2.ToArray();
			y1 = y2.ToArray();
		}

		private static double[] GetLog(double[] array){
			double[] newlog = new double[array.Length];
			for (int i = 0; i < array.Length; i++){
				newlog[i] = Math.Log(array[i]);
			}
			return newlog;
		}

		public static double[] CalcDensity(IMatrixData mdata, int colIndX, int colIndY, int points, bool logarithmicX,
			bool logaritmicY, DensityEstimationType type){
			double[] dvals = new double[0];
			double[] xvals = new double[0];
			double[] yvals = new double[0];
			if (logarithmicX && !logaritmicY){
				xvals = GetLog(GetColumn(mdata, colIndX));
				yvals = GetColumn(mdata, colIndY);
			} else if (!logarithmicX && logaritmicY){
				xvals = GetColumn(mdata, colIndX);
				yvals = GetLog(GetColumn(mdata, colIndY));
			} else if (logarithmicX){
				xvals = GetLog(GetColumn(mdata, colIndX));
				yvals = GetLog(GetColumn(mdata, colIndY));
			} else{
				xvals = GetColumn(mdata, colIndX);
				yvals = GetColumn(mdata, colIndY);
			}
			(double[,] values, double[] xmat, double[] ymat) = DensityEstimation.CalcDensityOnGrid(xvals, yvals,
				points, type);
			DensityEstimation.DivideByMaximum(values);
			double[,] percvalues = CalcExcludedPercentage(values);
			dvals = new double[xvals.Length];
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
			return pvals;
		}

		private static double[,] CalcExcludedPercentage(double[,] values){
			int n0 = values.GetLength(0);
			int n1 = values.GetLength(1);
			double[] v = new double[n0 * n1];
			int[] ind0 = new int[n0 * n1];
			int[] ind1 = new int[n0 * n1];
			int count = 0;
			for (int i0 = 0; i0 < n0; i0++){
				for (int i1 = 0; i1 < n1; i1++){
					v[count] = values[i0, i1];
					ind0[count] = i0;
					ind1[count] = i1;
					count++;
				}
			}
			int[] o = v.Order();
			v = v.SubArray(o);
			ind0 = ind0.SubArray(o);
			ind1 = ind1.SubArray(o);
			double total = 0;
			foreach (double t in v){
				total += t;
			}
			double[,] result = new double[n0, n1];
			double sum = 0;
			for (int i = 0; i < v.Length; i++){
				result[ind0[i], ind1[i]] = sum / total;
				sum += v[i];
			}
			return result;
		}

		public static double GetColorBlue(double max, double min){
			//val = ((percent * (max - min) / 100) + min
			double blue = ((20 * (max - min) / 100) + min);
			//  int blue = Convert.ToInt32(((array * 20) / max));
			return blue;
		}

		public static double GetColorCyan(double max, double min){
			double blue = ((40 * (max - min) / 100) + min);
			//  int blue = Convert.ToInt32(((array * 40) / max));
			return blue;
		}

		public static double GetColorGreen(double max, double min){
			double blue = ((60 * (max - min) / 100) + min);
			//   int blue = Convert.ToInt32(((array * 60) / max));
			return blue;
		}

		public static double GetColorYellow(double max, double min){
			double blue = ((80 * (max - min) / 100) + min);
			//   int blue = Convert.ToInt32(((array * 80) / max));
			return blue;
		}
	}
}