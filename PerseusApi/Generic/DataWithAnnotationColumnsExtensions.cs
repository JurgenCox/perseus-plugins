using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Util;

namespace PerseusApi.Generic{
	public static class DataWithAnnotationColumnsExtensions{
		public static string[][] GetCategoryColumn(this IDataWithAnnotationColumns data, string colname){
			return data.GetCategoryColumnAt(
				data.CategoryColumnNames.FindIndex(col => col.ToLower().Equals(colname.ToLower())));
		}

		public static bool TryGetStringColumn(this IDataWithAnnotationColumns data, string colname,
			out string[] column){
			var index = data.StringColumnNames.FindIndex(col => col.ToLower().Equals(colname.ToLower()));
			if (index >= 0){
				column = data.StringColumns[index];
				return true;
			}
			column = null;
			return false;
		}

		public static string[] GetStringColumn(this IDataWithAnnotationColumns data, string colname){
			return data.StringColumns[data.StringColumnNames.FindIndex(col => col.ToLower().Equals(colname.ToLower()))];
		}

		public static double[] GetNumericColumn(this IDataWithAnnotationColumns data, string colname){
			return data.NumericColumns[
				data.NumericColumnNames.FindIndex(col => col.ToLower().Equals(colname.ToLower()))];
		}

		public static int AnnotationColumnCount(this IDataWithAnnotationColumns data){
			return data.StringColumnCount + data.NumericColumnCount + data.CategoryColumnCount +
			       data.MultiNumericColumnCount;
		}

		/// <summary>
		/// Return all numerical data in the provided columns as rows.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="columns"></param>
		/// <returns></returns>
		public static double[][] NumericalRows(this IDataWithAnnotationColumns table, string[] columns){
			var numericalColumns = columns.Select(column =>
				table.NumericColumns[table.NumericColumnNames.FindIndex(col => col.Equals(column))]).ToArray();
			var numericalRows = new double[table.RowCount][];
			for (int i = 0; i < table.RowCount; i++){
				numericalRows[i] = numericalColumns.Select(col => col[i]).ToArray();
			}
			return numericalRows;
		}

		public static void PadColumnsToLength(this IDataWithAnnotationColumns nodeTable, int numRows){
			for (int i = 0; i < nodeTable.StringColumnCount; i++){
				var oldColumn = nodeTable.StringColumns[i];
				if (oldColumn.Length < numRows){
					var newColumn = Enumerable.Repeat(string.Empty, numRows).ToArray();
					Array.Copy(oldColumn, newColumn, oldColumn.Length);
					nodeTable.StringColumns[i] = newColumn;
				}
			}
			for (int i = 0; i < nodeTable.NumericColumnCount; i++){
				var oldColumn = nodeTable.NumericColumns[i];
				if (oldColumn.Length < numRows){
					var newColumn = Enumerable.Repeat(double.NaN, numRows).ToArray();
					Array.Copy(oldColumn, newColumn, oldColumn.Length);
					nodeTable.NumericColumns[i] = newColumn;
				}
			}
			for (int i = 0; i < nodeTable.CategoryColumnCount; i++){
				var oldColumn = nodeTable.GetCategoryColumnAt(i);
				if (oldColumn.Length < numRows){
					var newColumn = Enumerable.Repeat(Empty<string>(), numRows).ToArray();
					Array.Copy(oldColumn, newColumn, oldColumn.Length);
					nodeTable.SetCategoryColumnAt(newColumn, i);
				}
			}
			for (int i = 0; i < nodeTable.MultiNumericColumnCount; i++){
				var oldColumn = nodeTable.MultiNumericColumns[i];
				if (oldColumn.Length < numRows){
					var newColumn = Enumerable.Repeat(Empty<double>(), numRows).ToArray();
					Array.Copy(oldColumn, newColumn, oldColumn.Length);
					nodeTable.MultiNumericColumns[i] = newColumn;
				}
			}
		}

		public static void RemoveStringColumn(this IDataWithAnnotationColumns table, string colname){
			var index = table.StringColumnNames.FindIndex(col => col.Equals(colname));
			table.RemoveStringColumnAt(index);
		}

		/// <summary>
		/// Add Columns from another table, sorting to rows according to the mapping.
		/// Columns will be renamed to rename unique.
		/// </summary>
		/// <param name="toTable"></param>
		/// <param name="fromTable"></param>
		/// <param name="numRows"></param>
		/// <param name="rowMap"></param>
		public static void MapColumnValuesFrom(this IDataWithAnnotationColumns toTable,
			IDataWithAnnotationColumns fromTable, int numRows, Dictionary<int, int> rowMap){
			for (int i = 0; i < fromTable.StringColumnCount; i++){
				var column = fromTable.StringColumns[i];
				var newColumn = Enumerable.Repeat(string.Empty, numRows).ToArray();
				foreach (var kv in rowMap){
					newColumn[kv.Value] = column[kv.Key];
				}
				var name = StringUtils.GetNextAvailableName(fromTable.StringColumnNames[i], toTable.StringColumnNames);
				toTable.AddStringColumn(name, fromTable.StringColumnDescriptions[i], newColumn);
			}
			for (int i = 0; i < fromTable.NumericColumnCount; i++){
				var column = fromTable.NumericColumns[i];
				var newColumn = Enumerable.Repeat(double.NaN, numRows).ToArray();
				foreach (var kv in rowMap){
					newColumn[kv.Value] = column[kv.Key];
				}
				var name = StringUtils.GetNextAvailableName(fromTable.NumericColumnNames[i],
					toTable.NumericColumnNames);
				toTable.AddNumericColumn(name, fromTable.NumericColumnDescriptions[i], newColumn);
			}
			for (int i = 0; i < fromTable.CategoryColumnCount; i++){
				var column = fromTable.GetCategoryColumnAt(i);
				var newColumn = Enumerable.Repeat(Empty<string>(), numRows).ToArray();
				foreach (var kv in rowMap){
					newColumn[kv.Value] = column[kv.Key];
				}
				var name = StringUtils.GetNextAvailableName(fromTable.CategoryColumnNames[i],
					toTable.CategoryColumnNames);
				toTable.AddCategoryColumn(name, fromTable.CategoryColumnDescriptions[i], newColumn);
			}
			for (int i = 0; i < fromTable.MultiNumericColumnCount; i++){
				var column = fromTable.MultiNumericColumns[i];
				var newColumn = Enumerable.Repeat(Empty<double>(), numRows).ToArray();
				foreach (var kv in rowMap){
					newColumn[kv.Value] = column[kv.Key];
				}
				var name = StringUtils.GetNextAvailableName(fromTable.MultiNumericColumnNames[i],
					toTable.MultiNumericColumnNames);
				toTable.AddMultiNumericColumn(name, fromTable.MultiNumericColumnDescriptions[i], newColumn);
			}
		}

		public static T[] Empty<T>(){
			return EmptyArray<T>.Value;
		}
	}

	// Useful in number of places that return an empty byte array to avoid
	// unnecessary memory allocation.
	internal static class EmptyArray<T>{
		public static readonly T[] Value = new T[0];
	}
}