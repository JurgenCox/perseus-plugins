using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;

namespace PerseusPluginLib.Join{
	public class ConcatenateProcessing : IMatrixMultiProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Concatenate";
		public bool IsActive => true;
		public float DisplayRank => -5;
		public string HelpOutput => "";
		public string Description => "The matrices are concatenated so that the final matrix contains the rows from all the input matrices.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public int MinNumInput => 2;
		public int MaxNumInput => int.MaxValue;
		public string Heading => "Basic";
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixMultiProcessing:Basic:Concatenate";

		public string GetInputName(int index)
		{
			return $"Matrix {index + 1}";
		}
		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData[] inputData, ref string errString)
		{
			return new Parameters();
		}

		public IMatrixData ProcessData(IMatrixData[] inputData, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo)
		{
			return Concatenate(inputData);
		}

		/// <summary>
		/// Concatenate the provided tables into a single matrix contatining all the rows from the input matrices
		/// </summary>
		public static IMatrixData Concatenate(params IMatrixData[] tables)
		{
			var rowCount = tables.Sum(data => data.RowCount);
			var columns = tables.SelectMany(data => data.ColumnNames).Distinct().ToArray();
			var columnCount = columns.Length;
			var values = new double[rowCount, columnCount];
			var stringColumnNames = tables.SelectMany(data => data.StringColumnNames).Distinct().ToArray();
			var stringColumnCount = stringColumnNames.Length;
			var stringColumns = Enumerable.Range(0, stringColumnCount).Select(_ => new string[rowCount]).ToArray();
			var numericColumnNames = tables.SelectMany(data => data.NumericColumnNames).Distinct().ToArray();
			var numericColumnCount = numericColumnNames.Length;
			var numericColumns = Enumerable.Range(0, numericColumnCount).Select(_ => new double[rowCount]).ToArray();
			var multiNumericColumnNames = tables.SelectMany(data => data.MultiNumericColumnNames).Distinct().ToArray();
			var multiNumericColumnCount = multiNumericColumnNames.Length;
			var multiNumericColumns = Enumerable.Range(0, multiNumericColumnCount).Select(_ => new double[rowCount][]).ToArray();
			var categoryColumnNames = tables.SelectMany(data => data.CategoryColumnNames).Distinct().ToArray();
			var categoryColumnCount = categoryColumnNames.Length;
			var categoryColumns = Enumerable.Range(0, categoryColumnCount).Select(_ => new string[rowCount][]).ToArray();
			var tableIndex = 0;
			var coveredRows = 0;
			var mdata = tables[tableIndex];
			for (int i = 0; i < values.GetLength(0); i++)
			{
				if (i - coveredRows == mdata.RowCount)
				{
					coveredRows += mdata.RowCount;
					tableIndex++;
					mdata = tables[tableIndex];
				}
				var row = i - coveredRows;
				for (int j = 0; j < values.GetLength(1); j++)
				{
					var column = mdata.ColumnNames.FindIndex(col => col.Equals(columns[j]));
					values[i, j] = column >= 0 ? mdata.Values[row, column] : double.NaN;
				}
				for (int j = 0; j < stringColumnCount; j++)
				{
					var column = mdata.StringColumnNames.FindIndex(col => col.Equals(stringColumnNames[j]));
					stringColumns[j][i] = column >= 0 ? mdata.StringColumns[column][row] : string.Empty;
				}
				for (int j = 0; j < numericColumnCount; j++)
				{
					var column = mdata.NumericColumnNames.FindIndex(col => col.Equals(numericColumnNames[j]));
					numericColumns[j][i] = column >= 0 ? mdata.NumericColumns[column][row] : double.NaN;
				}
				for (int j = 0; j < multiNumericColumnCount; j++)
				{
					var column = mdata.MultiNumericColumnNames.FindIndex(col => col.Equals(multiNumericColumnNames[j]));
					multiNumericColumns[j][i] = column >= 0 ? mdata.MultiNumericColumns[column][row] : new double[0];
				}
				for (int j = 0; j < categoryColumnCount; j++)
				{
					var column = mdata.CategoryColumnNames.FindIndex(col => col.Equals(categoryColumnNames[j]));
					categoryColumns[j][i] = column >= 0 ? mdata.GetCategoryColumnEntryAt(column, row) : new string[0];
				}
			}
			var result = PerseusFactory.CreateMatrixData(values, columns.ToList());
			for (int i = 0; i < stringColumnCount; i++)
			{
				result.AddStringColumn(stringColumnNames[i], "", stringColumns[i]);
			}
			for (int i = 0; i < numericColumnCount; i++)
			{
				result.AddNumericColumn(numericColumnNames[i], "", numericColumns[i]);
			}
			for (int i = 0; i < multiNumericColumnCount; i++)
			{
				result.AddMultiNumericColumn(multiNumericColumnNames[i], "", multiNumericColumns[i]);
			}
			for (int i = 0; i < categoryColumnCount; i++)
			{
				result.AddCategoryColumn(categoryColumnNames[i], "", categoryColumns[i]);
			}
			return result;
		}
	}
}