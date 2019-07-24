using PerseusApi.Matrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseLibS.Num;

namespace PerseusPluginLib.Basic
{
    public class KineticsTimeMeasure
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

        public double[] GetInterval(IMatrixData mdata, int colIndx, int colIndy, int points)
        {

            List<double> getval = new List<double>();
            List<double> timeval = new List<double>();
            double[] xvals = GetColumn(mdata, colIndx);
            double[] yvals = GetColumn(mdata, colIndy);
            GetValidPairs(xvals, yvals, out double[] xvals1, out double[] yvals1);
            for (int i = 0; i < yvals.Length; i++)
            {
                for (int y = 0; y < xvals.Length; y++)
                {
                    if (xvals[y] >= 0 && xvals[y]<= 0.01)
                    {

                    }

                        }
            }


            return xvals;
        }

        public double calculatemedian(double[] range)
        {
            double median = 0;
            int counts = range.Length;
            for (int i = 0; i < range.Length; i++)
            {
                median = (range.Sum()/counts) ;
            }
            return median;
        }
    }
}
