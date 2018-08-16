using System;
using System.Linq;
using BaseLibS.Num;
using BaseLibS.Num.Matrix;
using BaseLibS.Util;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Utils
{
    public static class DataWithAnnotationColumnsExtensions
    {
        public static void UniqueValues(this IDataWithAnnotationColumns mdata, int[] stringCols)
        {
            foreach (string[] col in stringCols.Select(stringCol => mdata.StringColumns[stringCol]))
            {
                for (int i = 0; i < col.Length; i++)
                {
                    string q = col[i];
                    if (q.Length == 0)
                    {
                        continue;
                    }
                    string[] w = q.Split(';');
                    w = ArrayUtils.UniqueValues(w);
                    col[i] = StringUtils.Concat(";", w);
                }
            }
        }

		/// <summary>
		/// Add a number of empty rows to the table
		/// </summary>
	    public static void AddEmptyRows(this IMatrixData result, int count)
	    {
		    ExtendMainColumns(result, count);
		    AddEmptyRows((IDataWithAnnotationColumns)result, count);
	    }

		/// <summary>
		/// Add a number of empty rows to the table
		/// </summary>
	    public static void AddEmptyRows(this IDataWithAnnotationColumns mdata, int length)
	    {
		    for (int i = 0; i < mdata.StringColumnCount; i++)
		    {
			    mdata.StringColumns[i] = mdata.StringColumns[i].Concat(Enumerable.Repeat(String.Empty, length)).ToArray();
		    }
		    for (int i = 0; i < mdata.NumericColumnCount; i++)
		    {
			    mdata.NumericColumns[i] = mdata.NumericColumns[i].Concat(Enumerable.Repeat(Double.NaN, length)).ToArray();
		    }
		    for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
		    {
			    mdata.MultiNumericColumns[i] = mdata.MultiNumericColumns[i].Concat(Enumerable.Range(0, length).Select(_ => new double[0])).ToArray();
		    }
		    for (int i = 0; i < mdata.CategoryColumnCount; i++)
		    {
			    mdata.SetCategoryColumnAt(mdata.GetCategoryColumnAt(i).Concat(Enumerable.Repeat(new string[0], length)).ToArray(), i);
		    }
	    }

	    /// <summary>
	    /// Add padding with NaNs to main, quality, and imputation columns.
	    /// </summary>
	    private static void ExtendMainColumns(this IMatrixData mdata, int length)
	    {
		    var values = new float[mdata.RowCount + length, mdata.ColumnCount];
		    var hasQuality = mdata.Quality.IsInitialized();
		    var quality = hasQuality ? new float[mdata.Quality.RowCount + length, mdata.Quality.ColumnCount] : new float[0, 0];
		    var hasImputation = mdata.IsImputed.IsInitialized();
		    var isImputed = hasImputation ? new bool[mdata.IsImputed.RowCount + length, mdata.IsImputed.ColumnCount] : new bool[0,0];
		    for (int i = 0; i < mdata.Values.RowCount; i++)
		    {
			    for (int j = 0; j < mdata.Values.ColumnCount; j++)
			    {
				    values[i, j] = (float) mdata.Values[i, j];
				    if (hasQuality)
				    {
					    quality[i, j] = (float)mdata.Quality[i, j];
				    }
				    if (hasImputation)
				    {
					    isImputed[i, j] = mdata.IsImputed[i, j];
				    }
			    }
		    }
		    for (int i = mdata.Values.RowCount; i < values.GetLength(0); i++)
		    {
			    for (int j = 0; j < values.GetLength(1); j++)
			    {
				    values[i, j] = float.NaN;
				    if (hasQuality)
				    {
					    quality[i, j] = float.NaN;
				    }
				    if (hasImputation)
				    {
					    isImputed[i, j] = false;
				    }
			    }	
		    }
		    mdata.Values = new FloatMatrixIndexer(values);
		    if (hasQuality)
		    {
			    mdata.Quality = new FloatMatrixIndexer(quality);
		    }

		    if (hasImputation)
		    {
			    mdata.IsImputed = new BoolMatrixIndexer(isImputed);
		    }
	    }
    }
}