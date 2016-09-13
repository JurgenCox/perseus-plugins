using System.Collections.Generic;

namespace PerseusApi.Generic{
	public interface IDataWithAnnotationRows {
		void CopyAnnotationRowsFrom(IDataWithAnnotationRows other);
		void CopyAnnotationRowsFromColumns(IDataWithAnnotationColumns other);
		int ColumnCount { get; }

        /// <summary>
        /// Returns the entire category row at the given row index.
        /// 
		/// For performance reasons, please do not call this inside a loop when iterating over the elements. 
		/// Use <see cref="GetCategoryRowEntryAt"/>> instead.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
		string[][] GetCategoryRowAt(int row);

        /// <summary>
        /// Returns all the categories at the given row and column.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
		string[] GetCategoryRowEntryAt(int row, int column);

        /// <summary>
        /// Returns the unique categories used in the given row.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
		string[] GetCategoryRowValuesAt(int row);

		void SetCategoryRowAt(string[][] vals, int index);
		void RemoveCategoryRowAt(int index);
		void ClearCategoryRows();
		void AddCategoryRow(string name, string description, string[][] vals);
		int CategoryRowCount { get; }
		List<string> CategoryRowNames { get; set; }
		List<string> CategoryRowDescriptions { get; set; }
		List<string[][]> CategoryRows { set; }
		List<double[]> NumericRows { get; set; }
		int NumericRowCount { get; }
		void ClearNumericRows();
		void AddNumericRow(string name, string description, double[] vals);
		void RemoveNumericRowAt(int index);
		List<string> NumericRowNames { get; set; }
		List<string> NumericRowDescriptions { get; set; }
		void ClearStringRows();
		void AddStringRow(string name, string description, string[] vals);
		void RemoveStringRowAt(int index);
		List<string[]> StringRows { get; set; }
		int StringRowCount { get; }
		List<string> StringRowNames { get; set; }
		List<string> StringRowDescriptions { get; set; }
		void ClearMultiNumericRows();
		void AddMultiNumericRow(string name, string description, double[][] vals);
		void RemoveMultiNumericRowAt(int index);
		List<double[][]> MultiNumericRows { get; set; }
		int MultiNumericRowCount { get; }
		List<string> MultiNumericRowNames { get; set; }
		List<string> MultiNumericRowDescriptions { get; set; }

        /// <summary>
        /// extract the specified columns in-place
        /// </summary>
        /// <param name="indices">columns to keep</param>
		void ExtractColumns(int[] indices);

		void SetAnnotationRows(List<string> stringRowNames, List<string> stringRowDescriptions, List<string[]> stringRows,
			List<string> categoryRowNames, List<string> categoryRowDescriptions, List<string[][]> categoryRows,
			List<string> numericRowNames, List<string> numericRowDescriptions, List<double[]> numericRows,
			List<string> multiNumericRowNames, List<string> multiNumericRowDescriptions, List<double[][]> multiNumericRows);

		void SetAnnotationRows(List<string> stringRowNames, List<string[]> stringRows, List<string> categoryRowNames,
			List<string[][]> categoryRows, List<string> numericRowNames, List<double[]> numericRows,
			List<string> multiNumericRowNames, List<double[][]> multiNumericRows);

		void ClearAnnotationRows();
		List<string> ColumnNames { get; set; }
		List<string> ColumnDescriptions { get; set; }
	}
}