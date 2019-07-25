using PerseusApi.Matrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseLibS.Num;
using System;
using System.Linq;

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

        public double[] GetInterval(IMatrixData mdata, int colIndx, int colIndy, int method)
        {

            List<double> getval = new List<double>();
            List<double> timeval = new List<double>();
            double[] xvals = GetColumn(mdata, colIndx);
            double[] yvals = GetColumn(mdata, colIndy);
            GetValidPairs(xvals, yvals, out double[] xvals1, out double[] yvals1);
            double[] result = new double[0];
            for (double z = 0; z < yvals.Length; z += 5)
            {
                for (double i = 0; i < xvals.Length; i += 5)
                {
                    timeval.Add(xvals.Max());
                    if (method == 1) {
                        //calculate median
                        getval.Add(CalculateMedian(yvals));
                    }
                    if (method == 2)
                    {
                        //calculate mean
                        getval.Add(CalculateMedian(yvals));
                    }
                }
            }
            return xvals;
        }


        public double[] GetIntervalPercentile(IMatrixData mdata, int colIndx, int colIndy, int method, int percentile)
        {

            List<double> getval = new List<double>();
            List<double> timeval = new List<double>();
            double[] xvals = GetColumn(mdata, colIndx);
            double[] yvals = GetColumn(mdata, colIndy);
            GetValidPairs(xvals, yvals, out double[] xvals1, out double[] yvals1);
            double[] result = new double[0];
            for (double z = 0; z < yvals.Length; z += 5)
            {
                for (double i = 0; i < xvals.Length; i += 5)
                {
                    timeval.Add(xvals.Max());
                    if (method == 3)
                    {
                        //calculate median
                        getval.Add(calculatepercentile(yvals, percentile));
                    }
                }
            }
            return xvals;
        }


        public double CalculateMedian(double[] range)
        {
            double median = 0;
            int counts = range.Length;
            for (int i = 0; i < range.Length; i++)
            {
                median = (range.Sum()/counts) ;
            }
            return median;
        }

        public double calculatemean(double[] range)
        {
            double median = 0;
            int counts = range.Length;
            for (int i = 0; i < range.Length; i++)
            {
                median = (range.Sum() / counts);
            }
            return median;
        }

        public double calculatepercentile(double[] range, int percentile)
        {
            double median = 0;
            int counts = range.Length;
            for (int i = 0; i < range.Length; i++)
            {
                median = (range.Sum() / counts);
            }
            return median;
        }
    }
}
