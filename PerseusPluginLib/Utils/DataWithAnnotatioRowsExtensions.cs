using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using BaseLibS.Num;
using PerseusApi.Generic;

namespace PerseusPluginLib.Utils
{
    public static class DataWithAnnotationRowsExtensions
    {
        public static void Concat(this IDataWithAnnotationRows left, IDataWithAnnotationRows right, int[] includeRight)
        {
            var leftRows = left.Annotations();
            var onLeft = Enumerable.Range(0, left.ColumnCount).ToArray();
            var rightRows = right.Annotations();
            var stringRows = ConcatRows(leftRows.@string, onLeft, rightRows.@string, includeRight, "").Unpack();
            var numericRows = ConcatRows(leftRows.numeric, onLeft, rightRows.numeric, includeRight, double.NaN).Unpack();
            var categoryRows = ConcatRows(leftRows.category, onLeft, rightRows.category, includeRight, new string[0]).Unpack();
            var multiNumericRows = ConcatRows(leftRows.multiNumeric, onLeft, rightRows.multiNumeric, includeRight, new double[0]).Unpack();
            left.SetAnnotationRows(stringRows.names, stringRows.descriptions, stringRows.values,
                categoryRows.names, categoryRows.descriptions, categoryRows.values,
                numericRows.names, numericRows.descriptions, numericRows.values,
                multiNumericRows.names, multiNumericRows.descriptions, multiNumericRows.values);
            left.ColumnNames = left.ColumnNames.Concat(ArrayUtils.SubList(right.ColumnNames, includeRight)).ToList();
            left.ColumnDescriptions = left.ColumnDescriptions.Concat(ArrayUtils.SubList(right.ColumnDescriptions, includeRight)).ToList();
        }

        private static (List<string> names, List<string> descriptions, List<T[]> values) Unpack<T>(this ICollection<(string name, string descr, T[] values)> rows)
        {
            return (rows.Select(row => row.name).ToList(), rows.Select(row => row.descr).ToList(), rows.Select(row => row.values).ToList());
        }

        private static (string name, string descr, T[] values)[] ConcatRows<T>((string name, string descr, T[] values)[] left, int[] includeLeft,
            (string name, string descr, T[] values)[] right, int[] includeRight, T @default)
        {
            var leftNames = left.Select(l => l.name).ToArray();
            var names = leftNames.Concat(right.Select(r => r.name).Except(leftNames)).ToArray();
            (string name, string descr, T[] values) Pad(string name, IEnumerable<(string name, string descr, T[] values)> rows, int[] include)
            {
                var row = rows.DefaultIfEmpty((name: name, descr:"", values:null)).Single();
                return (row.name, row.descr, row.values == null ? Enumerable.Repeat(@default, include.Length).ToArray() : ArrayUtils.SubArray(row.values, include));
            }
            var leftPadded = names.GroupJoin(left, name => name, row => row.name,
                (name, rows) => Pad(name, rows, includeLeft));
            var rightPadded = names.GroupJoin(right, name => name, row => row.name,
                (name, rows) => Pad(name, rows, includeRight));
            var newRows = leftPadded.Join(rightPadded, l => l.name, r => r.name,
                (l, r) => (l.name, l.descr, l.values.Concat(r.values).ToArray())).ToArray();
            return newRows;
        }

        public static ((string name, string descr, string[] values)[] @string, (string name, string descr, double[] values)[] numeric,
            (string name, string descr, string[][] values)[] category, (string name, string descr, double[][] values)[] multiNumeric) Annotations(
                this IDataWithAnnotationRows data)
        {
            var stringRows = data.StringRowNames
                .Zip(data.StringRowDescriptions, (name, descr) => (name: name, descr: descr))
                .Zip(data.StringRows, (tuple, values) => (tuple.name, tuple.descr, values))
                .ToArray();
            var numericRows = data.NumericRowNames
                .Zip(data.NumericRowDescriptions, (name, descr) => (name: name, descr: descr))
                .Zip(data.NumericRows, (tuple, values) => (tuple.name, tuple.descr, values))
                .ToArray();
            var categoryRows = data.CategoryRowNames
                .Zip(data.CategoryRowDescriptions, (name, descr) => (name: name, descr: descr))
                .Zip(Enumerable.Range(0, data.CategoryRowCount).Select(data.GetCategoryRowAt), (tuple, values) => (tuple.name, tuple.descr, values))
                .ToArray();
            var multiNumRows = data.MultiNumericRowNames
                .Zip(data.MultiNumericRowDescriptions, (name, descr) => (name: name, descr: descr))
                .Zip(data.MultiNumericRows, (tuple, values) => (tuple.name, tuple.descr, values))
                .ToArray();
            return (stringRows, numericRows, categoryRows, multiNumRows);
        }
    }
}