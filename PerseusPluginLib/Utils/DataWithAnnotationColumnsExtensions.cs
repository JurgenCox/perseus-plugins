using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using BaseLib.Forms.Table;
using BaseLibS.Num;
using BaseLibS.Table;
using BaseLibS.Util;
using PerseusApi.Generic;
using PerseusPluginLib.Join;

namespace PerseusPluginLib.Utils
{
    public static class DataWithAnnotationColumnsExtensions
    {
        public static void Concat(this IDataWithAnnotationColumns left, IDataWithAnnotationColumns right, int[] onLeft,
            int[] onRight)
        {
            left.Concat(right, onLeft, onRight, strings => string.Join(";", strings), ArrayUtils.Mean, categories => categories.SelectMany(cat => cat).Distinct().ToArray(), numbers => numbers.SelectMany(num => num).ToArray());
        }

        public static IEnumerable<int[]> MatchedIndices(string[] fromIds, string[] toIds)
        {
            var noMatch = -1;
            var toIdRows = toIds.Select((id, row) => (id: id, row: row));
            var outerLeftJoin = fromIds
                .GroupJoin(toIdRows, fromId => fromId, toIdRow => toIdRow.id,
                    (fromId, matchedToIds) =>
                    {
                        if (string.IsNullOrWhiteSpace(fromId))
                        {
                            return matchedToIds.Select(_ => noMatch).ToArray();
                        }
                        var matchedToIdRows = matchedToIds.Select(toId => toId.row).ToArray();
                        return matchedToIdRows;
                    });
            return outerLeftJoin;
        }

        public static void Concat(this IDataWithAnnotationColumns left, IDataWithAnnotationColumns right, int[] onLeft, int[] onRight, Func<IList<string>,
            string> aggregateStrings, Func<IList<double>, double> aggregateDouble, Func<IList<string[]>, string[]> aggregateCategories, Func<IList<double[]>, double[]> aggregateMultiNumeric)
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
            var noMatch = (Enumerable.Repeat(string.Empty, right.StringColumnCount).ToArray(), Enumerable.Repeat(double.NaN, right.NumericColumnCount).ToArray(),
                Enumerable.Repeat(new string[0], right.CategoryColumnCount).ToArray(), Enumerable.Repeat(new double[0], right.MultiNumericColumnCount).ToArray());
            var combinedRows = mapping.Select(rows =>
            {
                var rowAnnotations = rows.Where(row => row != -1)
                    .Select(row => annotations[row])
                    .DefaultIfEmpty(noMatch);
                var (@string, numeric, category, multiNumeric) = Stack(rowAnnotations);
                return (@string: @string.Select(aggregateStrings).ToArray(),
                numeric: numeric.Select(aggregateDouble).ToArray(),
                category: category.Select(aggregateCategories).ToArray(),
                multiNumeric: multiNumeric.Select(aggregateMultiNumeric).ToArray());
            });
            var (stringCols, numCols, catCols, multiNumCols) = Stack(combinedRows);
            var stringColumns = right.StringColumnNames.Zip(right.StringColumnDescriptions, stringCols, Tuple)
                .Select((col, i) => (col: col, i: i)).Where(col => !onRight.Contains(col.i)).Select(col => col.col);
            foreach (var (name, descr, values) in stringColumns)
            {
               left.AddStringColumn(name, descr, values.ToArray());
            }
            foreach (var (name, descr, values) in right.NumericColumnNames.Zip(right.NumericColumnDescriptions, numCols, Tuple))
            {
               left.AddNumericColumn(name, descr, values.ToArray());
            }
            foreach (var (name, descr, values) in right.CategoryColumnNames.Zip(right.CategoryColumnDescriptions, catCols, Tuple))
            {
               left.AddCategoryColumn(name, descr, values.ToArray());
            }
            foreach (var (name, descr, values) in right.MultiNumericColumnNames.Zip(right.MultiNumericColumnDescriptions, multiNumCols, Tuple))
            {
               left.AddMultiNumericColumn(name, descr, values.ToArray());
            }
            var found = new[] {"+"};
            var notFound = new string[0];
            //left.AddCategoryColumn("Found", "", idCols.Select(ids => ids.Count > 0 ? found : notFound).ToArray());
        }

        public static (T1, T2, T3) Tuple<T1, T2, T3>(T1 first, T2 second, T3 third)
        {
            return (first, second, third);
        }

        public static IEnumerable<TResult> Zip<T1, T2, T3, TResult>(this IEnumerable<T1> first, IEnumerable<T2> second,
            IEnumerable<T3> third, Func<T1, T2, T3, TResult> f)
        {
            return first.Zip(second, (arg1, arg2) => (arg1, arg2))
                .Zip(third, (tuple, arg3) => f(tuple.Item1, tuple.Item2, arg3));
        }

        public static (List<T1>[], List<T2>[], List<T3>[], List<T4>[]) Stack<T1, T2, T3, T4>(this
            IEnumerable<(T1[] values1, T2[] values2, T3[] values3, T4[] values4)> rows)
        {
            var rowEnumerator = rows.GetEnumerator();
            rowEnumerator.MoveNext();
            var first = rowEnumerator.Current;
            var values1 = first.values1.Select(value => new List<T1> {value}).ToArray();
            var values2 = first.values2.Select(value => new List<T2> {value}).ToArray();
            var values3 = first.values3.Select(value => new List<T3> {value}).ToArray();
            var values4 = first.values4.Select(value => new List<T4> {value}).ToArray();
            while (rowEnumerator.MoveNext())
            {
                var row = rowEnumerator.Current;
                Aggregate(row.values1, values1);
                Aggregate(row.values2, values2);
                Aggregate(row.values3, values3);
                Aggregate(row.values4, values4);
            }

            void Aggregate<T>(IEnumerable<T> row, List<T>[] aggregator)
            {
                foreach (var (value, values) in row.Zip(aggregator, (value, values) => (value, values)))
                {
                    values.Add(value);
                }
            }

            return (values1, values2, values3, values4);
        }

        public static (List<T1>[], List<T2>[], List<T3>[], List<T4>[], List<T5>[]) Stack<T1, T2, T3, T4, T5>(this
            IEnumerable<(T1[] values1, T2[] values2, T3[] values3, T4[] values4, T5[] values5)> rows)
        {
            // Getting first row
            // ReSharper disable once PossibleMultipleEnumeration
            var first = rows.First();
            var values1 = first.values1.Select(value => new List<T1> {value}).ToArray();
            var values2 = first.values2.Select(value => new List<T2> {value}).ToArray();
            var values3 = first.values3.Select(value => new List<T3> {value}).ToArray();
            var values4 = first.values4.Select(value => new List<T4> {value}).ToArray();
            var values5 = first.values5.Select(value => new List<T5> {value}).ToArray();
            // Rest of the rows
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var row in rows)
            {
                Aggregate(row.values1, values1);
                Aggregate(row.values2, values2);
                Aggregate(row.values3, values3);
                Aggregate(row.values4, values4);
                Aggregate(row.values5, values5);
            }
            void Aggregate<T>(IEnumerable<T> row, List<T>[] aggregator)
            {
                foreach (var (value, values) in row.Zip(aggregator, (value, values) => (value, values)))
                {
                    values.Add(value);
                }
            }

            return (values1, values2, values3, values4, values5);
        }

        public static (List<T1>[], List<T2>[], List<T3>[], List<T4>[], List<T5>[], List<T6>[], List<T7>[]) Stack<T1, T2, T3, T4, T5, T6, T7>(this
            IEnumerable<(T1[] values1, T2[] values2, T3[] values3, T4[] values4, T5[] values5, T6[] values6, T7[] values7)> rows)
        {
            // Getting first row
            // ReSharper disable once PossibleMultipleEnumeration
            var first = rows.First();
            var values1 = first.values1.Select(value => new List<T1> {value}).ToArray();
            var values2 = first.values2.Select(value => new List<T2> {value}).ToArray();
            var values3 = first.values3.Select(value => new List<T3> {value}).ToArray();
            var values4 = first.values4.Select(value => new List<T4> {value}).ToArray();
            var values5 = first.values5.Select(value => new List<T5> {value}).ToArray();
            var values6 = first.values6.Select(value => new List<T6> {value}).ToArray();
            var values7 = first.values7.Select(value => new List<T7> {value}).ToArray();
            // Rest of the rows
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var row in rows)
            {
                Aggregate(row.values1, values1);
                Aggregate(row.values2, values2);
                Aggregate(row.values3, values3);
                Aggregate(row.values4, values4);
                Aggregate(row.values5, values5);
                Aggregate(row.values6, values6);
                Aggregate(row.values7, values7);
            }
            void Aggregate<T>(IEnumerable<T> row, List<T>[] aggregator)
            {
                foreach (var (value, values) in row.Zip(aggregator, (value, values) => (value, values)))
                {
                    values.Add(value);
                }
            }

            return (values1, values2, values3, values4, values5, values6, values7);
        }

        public static IEnumerable<(int row, string id)> Ids(this IDataWithAnnotationColumns left, int[] onLeft)
        {
            var n = left.RowCount;
            if (onLeft.Length == 1)
            {
                var idCol = left.StringColumns[onLeft.Single()];
                for (int i = 0; i < n; i++)
                {
                    foreach (var id in idCol[i].Split(';'))
                    {
                        yield return (i, id);
                    }
                }
                yield break;
            }
            if (onLeft.Length == 2)
            {
                var id1col = left.StringColumns[onLeft[0]];
                var id2col = left.StringColumns[onLeft[1]];
                for (int i = 0; i < n; i++)
                {
                    var ids1 = id1col[i].Split(';');
                    var ids2 = id2col[i].Split(';');
                    foreach (var id1 in ids1)
                    {
                        foreach (var id2 in ids2)
                        {
                            yield return (i, $"{id1}@#*&!#!@#{id2}");
                        }
                    }
                }
                yield break;
            }
            throw new NotImplementedException($"Not implemented for more than 2 columns");
        }

        public static IEnumerable<(string[] @string, double[] numeric, string[][] category, double[][] multiNumeric)>
            Annotations(this IDataWithAnnotationColumns left)
        {
            var n = left.RowCount;
            var categoryColumns = Enumerable.Range(0, left.CategoryColumnCount).Select(left.GetCategoryColumnAt).ToArray();
            for (int i = 0; i < n; i++)
            {
                var @string = left.StringColumns.Select(col => col[i]).ToArray();
                var numeric = left.NumericColumns.Select(col => col[i]).ToArray();
                var category = categoryColumns.Select(col => col[i]).ToArray();
                var multiNumeric = left.MultiNumericColumns.Select(col => col[i]).ToArray();
                yield return (@string, numeric, category, multiNumeric);
            }
        }

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
    }
}