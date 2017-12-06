using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num;
using BaseLibS.Num.Vector;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Utils {

    public static class MatrixDataExtensions
    {
        public static void Concat(this IMatrixData left, IMatrixData right, int[] onLeft, int[] onRight, int[] includeRight)
        {
            left.Concat(right, onLeft, onRight);
            left.Concat(right, includeRight);

        }

        public delegate T Aggregate<T>(IList<T> values);

        public static void Concat(this IMatrixData left, IMatrixData right, int[] onLeft, int[] onRight, Aggregate<double> aggMain,
            Aggregate<double> aggQuality, Aggregate<bool> aggImputed, Aggregate<string> aggText, Aggregate<double> aggNumeric, Aggregate<string[]> aggCategory,
            Aggregate<double[]> multiNumerics)
        {
            var leftIds = left.Ids(onLeft);
            var rightIds = right.Ids(onRight);
            var mapping = leftIds.GroupJoin(rightIds, l => l.id, r => r.id, (l, rs) =>
            {
                var toRows = rs.DefaultIfEmpty((row: -1, id: string.Empty))
                    .Select(r => r.row);
                return (fromRow: l.row, toRows: toRows);
            })
                .GroupBy(map => map.fromRow, (fromRow, maps) =>
                {
                    var toRows = maps.SelectMany(map => map.toRows).Distinct();
                    return (fromRow: fromRow, toRows: toRows);
                })
                .OrderBy(map => map.fromRow)
                .Select(map => map.toRows);
            var annotations = right.Annotations().ToArray();
            var noMatch = (Enumerable.Repeat(double.NaN, right.ColumnCount).ToArray(), Enumerable.Repeat(double.NaN, right.ColumnCount).ToArray(),
                Enumerable.Repeat(false, right.ColumnCount).ToArray(),
                Enumerable.Repeat(string.Empty, right.StringColumnCount).ToArray(), Enumerable.Repeat(double.NaN, right.NumericColumnCount).ToArray(),
                Enumerable.Repeat(new string[0], right.CategoryColumnCount).ToArray(), Enumerable.Repeat(new double[0], right.MultiNumericColumnCount).ToArray());
            var combinedRows = mapping.Select(rows =>
            {
                var rowAnnotations = rows.Where(row => row != -1)
                    .Select(row => annotations[row])
                    .DefaultIfEmpty(noMatch);
                var (main, quality, imputed, @string, numeric, category, multiNumeric) = rowAnnotations.Stack();
                return (
                main: main.Select(values => aggMain(values)).ToArray(),
                quality: quality.Select(values => aggQuality(values)).ToArray(),
                imputed: imputed.Select(values => aggImputed(values)).ToArray(),
                @string: @string.Select(values => aggText(values)).ToArray(),
                numeric: numeric.Select(values => aggNumeric(values)).ToArray(),
                category: category.Select(values => aggCategory(values)).ToArray(),
                multiNumeric: multiNumeric.Select(values => multiNumerics(values)).ToArray());
            });
            var (mainCols, qualityCols, imputedCol, stringCols, numCols, catCols, multiNumCols) = combinedRows.Stack();
            left.AddMainColumns(right.ColumnNames.ToArray(), mainCols.AsTwoDim(), qualityCols.AsTwoDim(), imputedCol.AsTwoDim());
            var stringColumns = right.StringColumnNames.Zip(right.StringColumnDescriptions, stringCols, DataWithAnnotationColumnsExtensions.Tuple)
                .Select((col, i) => (col: col, i: i)).Where(col => !onRight.Contains(col.i)).Select(col => col.col);
            foreach (var (name, descr, values) in stringColumns)
            {
                left.AddStringColumn(name, descr, values.ToArray());
            }
            foreach (var (name, descr, values) in right.NumericColumnNames.Zip(right.NumericColumnDescriptions, numCols, DataWithAnnotationColumnsExtensions.Tuple))
            {
                left.AddNumericColumn(name, descr, values.ToArray());
            }
            foreach (var (name, descr, values) in right.CategoryColumnNames.Zip(right.CategoryColumnDescriptions, catCols, DataWithAnnotationColumnsExtensions.Tuple))
            {
                left.AddCategoryColumn(name, descr, values.ToArray());
            }
            foreach (var (name, descr, values) in right.MultiNumericColumnNames.Zip(right.MultiNumericColumnDescriptions, multiNumCols, DataWithAnnotationColumnsExtensions.Tuple))
            {
                left.AddMultiNumericColumn(name, descr, values.ToArray());
            }
            var found = new[] { "+" };
            var notFound = new string[0];
            //left.AddCategoryColumn("Found", "", idCols.Select(ids => ids.Count > 0 ? found : notFound).ToArray());
        }

        private static T[,] AsTwoDim<T>(this IList<IList<T>> jagged)
        {
            var result = new T[jagged[0].Count, jagged.Count];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    result[i, j] = jagged[j][i];
                }
            }
            return result;
        }

		public static void AddMainColumns(this IMatrixData data, string[] names, double[,] vals, double[,] qual, bool[,] imp){
			double[,] newVals = new double[data.RowCount, data.ColumnCount + vals.GetLength(1)];
			double[,] newQual = new double[data.RowCount, data.ColumnCount + vals.GetLength(1)];
			bool[,] newImp = new bool[data.RowCount, data.ColumnCount + vals.GetLength(1)];
			for (int i = 0; i < data.RowCount; i++){
				for (int j = 0; j < data.ColumnCount; j++){
					newVals[i, j] = data.Values.Get(i, j);
					newQual[i, j] = data.Quality?.Get(i, j) ?? 0;
					newImp[i, j] = data.IsImputed?[i, j] ?? false;
				}
				for (int j = 0; j < vals.GetLength(1); j++){
					newVals[i, data.ColumnCount + j] = vals[i, j];
					newQual[i, data.ColumnCount + j] = qual[i, j];
					newImp[i, data.ColumnCount + j] = imp[i, j];
				}
			}
			data.Values.Set(newVals);
			data.Quality?.Set(newQual);
			data.IsImputed?.Set(newImp);
			data.ColumnNames.AddRange(names);
			data.ColumnDescriptions.AddRange(names);
		}

        public static IEnumerable<(double[] main, double[] quality, bool[] isImputed, string[] @string, double[] numeric, string[][] category, double[][] multiNumeric)>
            Annotations(this IMatrixData left)
        {
            var n = left.RowCount;
            var categoryColumns = Enumerable.Range(0, left.CategoryColumnCount).Select(left.GetCategoryColumnAt).ToArray();
            for (int i = 0; i < n; i++)
            {
                var main = left.Values.GetRow(i).ToArray();
                var quality = left.Quality.GetRow(i).ToArray();
                var isImputed = left.IsImputed.GetRow(i).ToArray();
                var @string = left.StringColumns.Select(col => col[i]).ToArray();
                var numeric = left.NumericColumns.Select(col => col[i]).ToArray();
                var category = categoryColumns.Select(col => col[i]).ToArray();
                var multiNumeric = left.MultiNumericColumns.Select(col => col[i]).ToArray();
                yield return (main, quality, isImputed, @string, numeric, category, multiNumeric);
            }
        }


        public static void UniqueRows(this IMatrixData mdata, string[] ids, Func<double[], double> combineNumeric,
			Func<string[], string> combineString, Func<string[][], string[]> combineCategory,
			Func<double[][], double[]> combineMultiNumeric) {
			int[] order = ArrayUtils.Order(ids);
			List<int> uniqueIdx = new List<int>();
			string lastId = "";
			List<int> idxsWithSameId = new List<int>();
			foreach (int j in order) {
				string id = ids[j];
				if (id == lastId) {
					idxsWithSameId.Add(j);
				} else {
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

		public static void CombineRows(this IMatrixData mdata, List<int> rowIdxs, Func<double[], double> combineNumeric,
			Func<string[], string> combineString, Func<string[][], string[]> combineCategory,
			Func<double[][], double[]> combineMultiNumeric) {
			if (!rowIdxs.Any()) {
				return;
			}
			int resultRow = rowIdxs[0];
			for (int i = 0; i < mdata.Values.ColumnCount; i++) {
				BaseVector column = mdata.Values.GetColumn(i);
				BaseVector values = column.SubArray(rowIdxs);
				mdata.Values[resultRow, i] = combineNumeric(ArrayUtils.ToDoubles(values));
			}
			for (int i = 0; i < mdata.NumericColumnCount; i++) {
				double[] column = mdata.NumericColumns[i];
				double[] values = ArrayUtils.SubArray(column, rowIdxs);
				column[resultRow] = combineNumeric(values);
			}
			for (int i = 0; i < mdata.StringColumnCount; i++) {
				string[] column = mdata.StringColumns[i];
				string[] values = ArrayUtils.SubArray(column, rowIdxs);
				column[resultRow] = combineString(values);
			}
			for (int i = 0; i < mdata.CategoryColumnCount; i++) {
				string[][] column = mdata.GetCategoryColumnAt(i);
				string[][] values = ArrayUtils.SubArray(column, rowIdxs);
				column[resultRow] = combineCategory(values);
				mdata.SetCategoryColumnAt(column, i);
			}
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++) {
				double[][] column = mdata.MultiNumericColumns[i];
				double[][] values = ArrayUtils.SubArray(column, rowIdxs);
				column[resultRow] = combineMultiNumeric(values);
			}
		}
	}
}