using System;
using System.Collections.Generic;

namespace PerseusApi.Generic{
    public interface IDataWithAnnotationColumns : IEquatable<IDataWithAnnotationColumns>
    {
        void CopyAnnotationColumnsFrom(IDataWithAnnotationColumns other);
		void CopyAnnotationColumnsFromRows(IDataWithAnnotationRows other);
		int RowCount { get; }
		/// <summary>
		///     For performance reasons, please do not call this inside a loop when iterating over the elements.
		///     Use <code>GetCategoryColumnEntryAt</code> instead.
		/// </summary>
		string[][] GetCategoryColumnAt(int column);

        /// <summary>
        /// Get all the categories associated with the table entry at the specified column and row.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns></returns>
		string[] GetCategoryColumnEntryAt(int column, int row);
        
        /// <summary>
        /// Get all category values used in the specified column.
        /// See <code>PerseusLibS.ICategoryVectorData</code>.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
		string[] GetCategoryColumnValuesAt(int column);

        /// <summary>
        /// Change the values of an existing category column.
        /// </summary>
        /// <param name="vals">new values</param>
        /// <param name="column">existing column</param>
		void SetCategoryColumnAt(string[][] vals, int column);

		void RemoveCategoryColumnAt(int column);
		void ClearCategoryColumns();
		void AddCategoryColumn(string name, string description, string[][] vals);
		int CategoryColumnCount { get; }
		List<string> CategoryColumnNames { get; set; }
        List<string> CategoryColumnDescriptions { get; set; }
		List<string[][]> CategoryColumns { set; }

        /// <summary>
        /// Gets all numeric columns in the table.
        /// Might not be feasible for large tables, use <see cref="NumericColumnAt"/> instead
        /// </summary>
		List<double[]> NumericColumns { get; set; }

        /// <summary>
        /// Gets the numeric column values at the specified row
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns></returns>
	    double NumericColumnAt(int column, int row);

		int NumericColumnCount { get; }
		void ClearNumericColumns();
		void AddNumericColumn(string name, string description, double[] vals);
		void RemoveNumericColumnAt(int index);
		List<string> NumericColumnNames { get; set; }
		List<string> NumericColumnDescriptions { get; set; }
	
		void ClearStringColumns();
		void AddStringColumn(string name, string description, string[] vals);
      //  void AddStringColumnDescriptions(string name, string description, string[] vals);
        void RemoveStringColumnAt(int index);

        /// <summary>
        /// Gets all string columns in the table.
        /// Might not be feasible for large tables, use <see cref="StringColumnAt"/> instead
        /// </summary>
		List<string[]> StringColumns { get; set; }


        /// <summary>
        /// Get string column values at the specified column and row
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns></returns>
	    string StringColumnAt(int column, int row);

		int StringColumnCount { get; }
		List<string> StringColumnNames { get; set; }
        List<string> MatrixNames { get; set; }
        //	List<string> ColumnDescription { get; set; }
        //    List<string> MainColumnNames { get; set; }
        List<string> StringColumnDescriptions { get; set; }
        void ClearMultiNumericColumns();
		void AddMultiNumericColumn(string name, string description, double[][] vals);
		void RemoveMultiNumericColumnAt(int index);

        /// <summary>
        /// Gets all multi-numeric columns in the table.
        /// Might not be feasible for large tables, use <see cref="MultiNumericColumnAt"/> instead
        /// </summary>
		List<double[][]> MultiNumericColumns { get; set; }

        /// <summary>
        /// Gets the multi-numeric values at the specified column and row
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns></returns>
	    double[] MultiNumericColumnAt(int column, int row);

		int MultiNumericColumnCount { get; }
		List<string> MultiNumericColumnNames { get; set; }
		List<string> MultiNumericColumnDescriptions { get; set; }

		void ExtractRows(int[] indices);
		void SetAnnotationColumns(List<string> stringColumnNames, List<string[]> stringColumns,
			List<string> categoryColumnNames, List<string[][]> categoryColumns, List<string> numericColumnNames,
			List<double[]> numericColumns, List<string> multiNumericColumnNames, List<double[][]> multiNumericColumns);
		void SetAnnotationColumns(List<string> stringColumnNames, List<string> stringColumnDescriptions,
			List<string[]> stringColumns, List<string> categoryColumnNames, List<string> categoryColumnDescriptions,
			List<string[][]> categoryColumns, List<string> numericColumnNames, List<string> numericColumnDescriptions,
			List<double[]> numericColumns, List<string> multiNumericColumnNames, List<string> multiNumericColumnDescriptions,
			List<double[][]> multiNumericColumns);
		void ClearAnnotationColumns();
	}
}