using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Utils
{
    public static class MatrixDataExtensions
    {
        public static void UniqueRows(this IMatrixData mdata, string[] ids, Func<double[], double> combineNumeric, Func<string[], string> combineString, Func<string[][], string[]> combineCategory,
            Func<double[][], double[]> combineMultiNumeric)
        {
            var order = ArrayUtils.Order(ids);
            var uniqueIdx = new List<int>();
            var lastId = "";
            var idxsWithSameId = new List<int>();
            foreach (int j in order)
            {
                var id = ids[j];
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
            var resultRow = rowIdxs[0];
            for (int i = 0; i < mdata.Values.ColumnCount; i++)
            {
                var column = mdata.Values.GetColumn(i);
                var values = column.SubArray(rowIdxs);
                mdata.Values[resultRow, i] = (float) combineNumeric(ArrayUtils.ToDoubles(values));
            }
            for (int i = 0; i < mdata.NumericColumnCount; i++)
            {
                var column = mdata.NumericColumns[i];
                var values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineNumeric(values);
            }
            for (int i = 0; i < mdata.StringColumnCount; i++)
            {
                var column = mdata.StringColumns[i];
                var values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineString(values);
            }
            for (int i = 0; i < mdata.CategoryColumnCount; i++)
            {
                var column = mdata.GetCategoryColumnAt(i);
                var values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineCategory(values);
                mdata.SetCategoryColumnAt(column, i);
            }
            for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
            {
                var column = mdata.MultiNumericColumns[i];
                var values = ArrayUtils.SubArray(column, rowIdxs);
                column[resultRow] = combineMultiNumeric(values);
            }
        }
    }
}