using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Basic
{
    public class AutomaticDensity
    {


        private static double[] GetColumn(IMatrixData matrixData, int ind)
        {
            if (ind < matrixData.ColumnCount)
            {
                return matrixData.Values.GetColumn(ind).ToArray();
            }
            double[] x = matrixData.NumericColumns[ind - matrixData.ColumnCount];
            double[] f = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                f[i] = x[i];
            }
            return f;
        }

        private static void GetValidPairs(IList<double> x, IList<double> y, out double[] x1, out double[] y1)
        {
            List<double> x2 = new List<double>();
            List<double> y2 = new List<double>();
            for (int i = 0; i < x.Count; i++)
            {
                if (!double.IsNaN(x[i]) && !double.IsInfinity(x[i]) && !double.IsNaN(y[i]) && !double.IsInfinity(y[i]))
                {
                    x2.Add(x[i]);
                    y2.Add(y[i]);
                }
            }
            x1 = x2.ToArray();
            y1 = y2.ToArray();
        }

        private static double[] getLog(double[] array)
        {
            double[] newlog = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                newlog[i] = Math.Log(array[i]);
            }
                return newlog;
        }

        public static double[] CalcDensity(IMatrixData mdata, int colIndx, int colIndy,
     int points, bool logarithmic)
        {
            double[] dvals = new double[0];
            double[] xvals = new double[0];
            double[] yvals = new double[0];
            if (logarithmic == true)
            {
                xvals = getLog(GetColumn(mdata, colIndx));
                yvals = getLog(GetColumn(mdata, colIndy));
            } else {
                xvals = GetColumn(mdata, colIndx);
                yvals = GetColumn(mdata, colIndy);
            }
            GetValidPairs(xvals, yvals, out double[] xvals1, out double[] yvals1);
            DensityEstimation.CalcRanges(xvals1, yvals1, out double xmin, out double xmax, out double ymin, out double ymax);
            double[,] values = DensityEstimation.GetValuesOnGrid(xvals1, xmin, (xmax - xmin) / points, points, yvals1, ymin,
                (ymax - ymin) / points, points);

            DensityEstimation.DivideByMaximum(values);
            double[] xmat = new double[points];
            for (int i = 0; i < points; i++)
            {
                xmat[i] = xmin + i * (xmax - xmin) / points;
            }
            double[] ymat = new double[points];
            for (int i = 0; i < points; i++)
            {
                ymat[i] = ymin + i * (ymax - ymin) / points;
            }
            double[,] percvalues = CalcExcludedPercentage(values);
            dvals = new double[xvals.Length];
            double[] pvals = new double[xvals.Length];
            for (int i = 0; i < dvals.Length; i++)
            {
                double xx = xvals[i];
                double yy = yvals[i];
                if (!double.IsNaN(xx) && !double.IsNaN(yy))
                {
                    int xind = ArrayUtils.ClosestIndex(xmat, xx);
                    int yind = ArrayUtils.ClosestIndex(ymat, yy);
                    dvals[i] = values[xind, yind];
                    pvals[i] = percvalues[xind, yind];
                }
                else
                {
                    dvals[i] = double.NaN;
                    pvals[i] = double.NaN;
                }
            }


            return dvals;
        }

        private static double[,] CalcExcludedPercentage(double[,] values)
        {
            int n0 = values.GetLength(0);
            int n1 = values.GetLength(1);
            double[] v = new double[n0 * n1];
            int[] ind0 = new int[n0 * n1];
            int[] ind1 = new int[n0 * n1];
            int count = 0;
            for (int i0 = 0; i0 < n0; i0++)
            {
                for (int i1 = 0; i1 < n1; i1++)
                {
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
            foreach (double t in v)
            {
                total += t;
            }
            double[,] result = new double[n0, n1];
            double sum = 0;
            for (int i = 0; i < v.Length; i++)
            {
                result[ind0[i], ind1[i]] = sum / total;
                sum += v[i];
            }
            return result;
        }



        public static double GetColorBlue(double max, double min)
        {
            //val = ((percent * (max - min) / 100) + min
            double blue = ((20 * (max - min) / 100) + min);
            //  int blue = Convert.ToInt32(((array * 20) / max));
            return blue;
        }

        public static double GetColorCyan(double max, double min)
        {
            double blue = ((40 * (max - min) / 100) + min);
            //  int blue = Convert.ToInt32(((array * 40) / max));
            return blue;
        }

        public static double GetColorGreen(double max, double min)
        {
            double blue = ((60 * (max - min) / 100) + min);
            //   int blue = Convert.ToInt32(((array * 60) / max));
            return blue;
        }

        public static double GetColorYellow(double max, double min)
        {
            double blue = ((80 * (max - min) / 100) + min);
            //   int blue = Convert.ToInt32(((array * 80) / max));
            return blue;
        }
    }
}

