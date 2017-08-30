using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num;
using BaseLibS.Num.Vector;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Utils
{
    public static class MatrixDataExtensions
    {
        public static void UniqueRows(this IMatrixData mdata, string[] ids, Func<double[], double> combineNumeric, Func<string[], string> combineString, Func<string[][], string[]> combineCategory,
            Func<double[][], double[]> combineMultiNumeric)
        {
            int[] order = ArrayUtils.Order(ids);
            List<int> uniqueIdx = new List<int>();
            string lastId = "";
            List<int> idxsWithSameId = new List<int>();
            foreach (int j in order)
            {
                string id = ids[j];
                if (id == lastId)
                {
                    idxsWithSameId.Add(j);
                }
                else
                {
                    CombineRows(mdata, idxsWithSameId, combineNumeric, combineString, combineCategory, combineMultiNumeric);
                    uniqueIdx.Add(j);
                    idxsWithSameId.Clear();
                    idxsWithSameId.Add(j);
                }
                lastId = id;
            }
            CombineRows(mdata, idxsWithSameId, combineNumeric, combineString, combineCategory, combineMultiNumeric);
            mdata.ExtractRows(uniqueIdx.ToArray());
        }

        public static void CombineRows(this IMatrixData mdata, List<int> rowIdxs, Func<double[], double> combineNumeric, Func<string[], string> combineString, Func<string[][], string[]> combineCategory, Func<double[][], double[]> combineMultiNumeric)
        {
            if (!rowIdxs.Any())
            {
                return;
            }
            int resultRow = rowIdxs[0];
            for (int i = 0; i < mdata.Values.ColumnCount; i++)
            {
                BaseVector column = mdata.Values.GetColumn(i);
                BaseVector values = column.SubArray(rowIdxs);
                mdata.Values[resultRow, i] = (float) combineNumeric(ArrayUtils.ToDoubles(values));
            }
            for (int i = 0; i < mdata.NumericColumnCount; i++)
            {
                double[] column = mdata.NumericColumns[i];
                double[] values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineNumeric(values);
            }
            for (int i = 0; i < mdata.StringColumnCount; i++)
            {
                string[] column = mdata.StringColumns[i];
                string[] values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineString(values);
            }
            for (int i = 0; i < mdata.CategoryColumnCount; i++)
            {
                string[][] column = mdata.GetCategoryColumnAt(i);
                string[][] values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineCategory(values);
                mdata.SetCategoryColumnAt(column, i);
            }
            for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
            {
                double[][] column = mdata.MultiNumericColumns[i];
                double[][] values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineMultiNumeric(values);
            }
        }
    }
}