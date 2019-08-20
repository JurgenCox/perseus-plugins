using System.Collections.Generic;
using BaseLibS.Num;
using BaseLibS.Table;
using BaseLibS.Util;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Manual{
	public class MatrixDataTable : VirtualDataTable{
		private readonly IMatrixData mdata;

		public MatrixDataTable(IMatrixData mdata) : base(mdata.Name, mdata.Description, mdata.RowCount){
			this.mdata = mdata;
			CreateColumns();
			CreateAnnotationRows();
		}

		private void CreateAnnotationRows(){
			CreateTypeRow();
			for (int i = 0; i < mdata.CategoryRowCount; i++){
				DataAnnotationRow row = NewAnnotationRow();
				for (int j = 0; j < mdata.ColumnCount; j++){
					row[j] = StringUtils.Concat(";", mdata.GetCategoryRowAt(i)[j] ?? new string[0]);
				}
				AddAnnotationRow(row, mdata.CategoryRowNames[i], mdata.CategoryRowDescriptions[i]);
			}
			for (int i = 0; i < mdata.NumericRowCount; i++){
				DataAnnotationRow row = NewAnnotationRow();
				for (int j = 0; j < mdata.ColumnCount; j++){
					row[j] = "" + mdata.NumericRows[i][j];
				}
				AddAnnotationRow(row, mdata.NumericRowNames[i], mdata.NumericRowDescriptions[i]);
			}
		}

		private void CreateTypeRow(){
			DataAnnotationRow row = NewAnnotationRow();
			int count = 0;
			for (int i = 0; i < mdata.ColumnCount; i++){
				row[count++] = "Expression";
			}
			for (int i = 0; i < mdata.CategoryColumnCount; i++){
				row[count++] = "Category";
			}
			for (int i = 0; i < mdata.NumericColumnCount; i++){
				row[count++] = "Numeric";
			}
			for (int i = 0; i < mdata.StringColumnCount; i++){
				row[count++] = "Text";
			}
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++){
				row[count++] = "Multi-numeric";
			}
			AddAnnotationRow(row, "Type", "Type of the column.");
		}

		public override object[] GetRowData(long row){
			List<object> rowData = new List<object>();
			for (int i = 0; i < mdata.ColumnCount; i++){
				rowData.Add(NumUtils.RoundSignificantDigits(mdata.Values.Get((int) row, i), 6));
			}
			for (int i = 0; i < mdata.CategoryColumnCount; i++){
				rowData.Add(StringUtils.Concat(";", mdata.GetCategoryColumnEntryAt(i, (int) row) ?? new string[0]));
			}
			for (int i = 0; i < mdata.NumericColumnCount; i++){
				rowData.Add(NumUtils.RoundSignificantDigits(mdata.NumericColumns[i][row], 6));
			}
			for (int i = 0; i < mdata.StringColumnCount; i++){
				rowData.Add(mdata.StringColumns[i][row]);
			}
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++){
				rowData.Add(StringUtils.Concat(";", mdata.MultiNumericColumns[i][row] ?? new double[0]));
			}
			return rowData.ToArray();
		}

		public void CreateColumns(){
			for (int i = 0; i < mdata.ColumnCount; i++){
				string s = mdata.ColumnNames[i];
				AddColumn(s, 60, ColumnType.Numeric, mdata.ColumnDescriptions[i]);
			}
			for (int i = 0; i < mdata.CategoryColumnCount; i++){
				AddColumn(mdata.CategoryColumnNames[i], 60, ColumnType.Categorical, mdata.CategoryColumnDescriptions[i]);
			}
			for (int i = 0; i < mdata.NumericColumnCount; i++){
				AddColumn(mdata.NumericColumnNames[i], 60, ColumnType.Numeric, mdata.NumericColumnDescriptions[i]);
			}

                for (int i = 0; i < mdata.StringColumnCount; i++)
                {
                    AddColumn(mdata.StringColumnNames[i], 60, ColumnType.Text, mdata.StringColumnDescriptions[i]);
                }
            
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++){
				AddColumn(mdata.MultiNumericColumnNames[i], 60, ColumnType.MultiNumeric, mdata.MultiNumericColumnDescriptions[i]);
			}
		}
	}
}