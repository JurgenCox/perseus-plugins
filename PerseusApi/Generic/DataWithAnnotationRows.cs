using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BaseLibS.Data;
using BaseLibS.Data.Category;
using BaseLibS.Num;
using BaseLibS.Util;
namespace PerseusApi.Generic{
	[Serializable]
	public class DataWithAnnotationRows : IDataWithAnnotationRows{
		private List<string> columnNames = new List<string>();
		public List<string> ColumnDescriptions{ get; set; }
		public List<string> CategoryRowNames{ get; set; }
		public List<string> NumericRowNames{ get; set; }
		public List<string> StringRowNames{ get; set; }
		public List<string> MultiNumericRowNames{ get; set; }
		public List<string> CategoryRowDescriptions{ get; set; }
		public List<string> NumericRowDescriptions{ get; set; }
		public List<string> StringRowDescriptions{ get; set; }
		public List<string> MultiNumericRowDescriptions{ get; set; }
		private readonly StringVectors stringRowData = new StringVectors();
		private List<CategoryVector> categoryRowData = new List<CategoryVector>();
		private readonly MultiNumericVectors multiNumericRowData = new MultiNumericVectors();
		public List<double[]> NumericRows{ get; set; }
		public DataWithAnnotationRows(){
			CategoryRowNames = new List<string>();
			CategoryRowDescriptions = new List<string>();
			NumericRowNames = new List<string>();
			NumericRowDescriptions = new List<string>();
			NumericRows = new List<double[]>();
			StringRowNames = new List<string>();
			StringRowDescriptions = new List<string>();
			MultiNumericRowNames = new List<string>();
			MultiNumericRowDescriptions = new List<string>();
			MultiNumericRows = new List<double[][]>();
			ColumnDescriptions = new List<string>();
		}
		public DataWithAnnotationRows(BinaryReader reader){
			columnNames = new List<string>(FileUtils.ReadStringArray(reader));
			ColumnDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			CategoryRowNames = new List<string>(FileUtils.ReadStringArray(reader));
			NumericRowNames = new List<string>(FileUtils.ReadStringArray(reader));
			StringRowNames = new List<string>(FileUtils.ReadStringArray(reader));
			MultiNumericRowNames = new List<string>(FileUtils.ReadStringArray(reader));
			CategoryRowDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			NumericRowDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			StringRowDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			MultiNumericRowDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			stringRowData = new StringVectors(reader);
			int len = reader.ReadInt32();
			categoryRowData = new List<CategoryVector>();
			for (int i = 0; i < len; i++){
				categoryRowData.Add(new CategoryVector(reader));
			}
			multiNumericRowData = new MultiNumericVectors(reader); 
			len = reader.ReadInt32();
			NumericRows = new List<double[]>();
			for (int i = 0; i < len; i++) {
				NumericRows.Add(FileUtils.ReadDoubleArray(reader));
			}
		}
		public void Write(BinaryWriter writer){
			FileUtils.Write(columnNames.ToArray(), writer);
			FileUtils.Write(ColumnDescriptions.ToArray(), writer);
			FileUtils.Write(CategoryRowNames.ToArray(), writer);
			FileUtils.Write(NumericRowNames.ToArray(), writer);
			FileUtils.Write(StringRowNames.ToArray(), writer);
			FileUtils.Write(MultiNumericRowNames.ToArray(), writer);
			FileUtils.Write(CategoryRowDescriptions.ToArray(), writer);
			FileUtils.Write(NumericRowDescriptions.ToArray(), writer);
			FileUtils.Write(StringRowDescriptions.ToArray(), writer);
			FileUtils.Write(MultiNumericRowDescriptions.ToArray(), writer);
			stringRowData.Write(writer);
			writer.Write(categoryRowData.Count);
			foreach (CategoryVector vector in categoryRowData){
				vector.Write(writer);
			}
			multiNumericRowData.Write(writer);
			writer.Write(NumericRows.Count);
			foreach (double[] row in NumericRows){
				FileUtils.Write(row, writer);
			}
		}
		public List<double[][]> MultiNumericRows{
			get => multiNumericRowData.MultiNumericVecs;
			set => multiNumericRowData.MultiNumericVecs = value;
		}
		public List<string> ColumnNames{
			get => columnNames;
			set{
				columnNames = value;
				if (value != null && (ColumnDescriptions == null || ColumnDescriptions.Count == 0)){
					ColumnDescriptions = CreateEmpty(columnNames, "");
				}
			}
		}
		public List<string[]> StringRows{
			get => stringRowData.StringVecs;
			set => stringRowData.StringVecs = value;
		}
		public List<string[][]> CategoryRows{
			set{
				categoryRowData.Clear();
				foreach (string[][] strings in value){
					categoryRowData.Add(new CategoryVector(strings));
				}
			}
		}
		public void CopyAnnotationRowsFrom(IDataWithAnnotationRows other){
			ColumnNames = CloneX(other.ColumnNames);
			ColumnDescriptions = CloneX(other.ColumnDescriptions);
			CategoryRowNames = CloneX(other.CategoryRowNames);
			CategoryRowDescriptions = CloneX(other.CategoryRowDescriptions);
			NumericRowNames = CloneX(other.NumericRowNames);
			NumericRowDescriptions = CloneX(other.NumericRowDescriptions);
			MultiNumericRowNames = CloneX(other.MultiNumericRowNames);
			MultiNumericRowDescriptions = CloneX(other.MultiNumericRowDescriptions);
			StringRowNames = CloneX(other.StringRowNames);
			StringRowDescriptions = CloneX(other.StringRowDescriptions);
			CategoryRows = new List<string[][]>();
			for (int i = 0; i < other.CategoryRowCount; i++){
				AddCategoryRow(other.CategoryRowNames[i], other.CategoryRowDescriptions[i],
					other.GetCategoryRowAt(i));
			}
			NumericRows = new List<double[]>();
			foreach (double[] s in other.NumericRows){
				NumericRows.Add((double[]) s.Clone());
			}
			MultiNumericRows = new List<double[][]>();
			foreach (double[][] s in other.MultiNumericRows){
				MultiNumericRows.Add((double[][]) s.Clone());
			}
			StringRows = new List<string[]>();
			foreach (string[] s in other.StringRows){
				StringRows.Add((string[]) s.Clone());
			}
		}
		public void CopyAnnotationRowsFromColumns(IDataWithAnnotationColumns other){
			NumericRowNames = CloneX(other.NumericColumnNames);
			NumericRowDescriptions = CloneX(other.NumericColumnDescriptions);
			MultiNumericRowNames = CloneX(other.MultiNumericColumnNames);
			MultiNumericRowDescriptions = CloneX(other.MultiNumericColumnDescriptions);
			StringRowNames = CloneX(other.StringColumnNames);
			StringRowDescriptions = CloneX(other.StringColumnDescriptions);
			CategoryRows = new List<string[][]>();
			CategoryRowNames = new List<string>();
			CategoryRowDescriptions = new List<string>();
			categoryRowData = new List<CategoryVector>();
			for (int i = 0; i < other.CategoryColumnCount; i++){
				AddCategoryRow(other.CategoryColumnNames[i], other.CategoryColumnDescriptions[i],
					other.GetCategoryColumnAt(i));
			}
			NumericRows = new List<double[]>();
			foreach (double[] s in other.NumericColumns){
				NumericRows.Add((double[]) s.Clone());
			}
			MultiNumericRows = new List<double[][]>();
			foreach (double[][] s in other.MultiNumericColumns){
				MultiNumericRows.Add((double[][]) s.Clone());
			}
			StringRows = new List<string[]>();
			foreach (string[] s in other.StringColumns){
				StringRows.Add((string[]) s.Clone());
			}
		}
		public string[][] GetCategoryRowAt(int index){
			return categoryRowData[index].GetAllData();
		}
		public string[] GetCategoryRowEntryAt(int index, int column){
			return categoryRowData[index][column];
		}
		public string[] GetCategoryRowValuesAt(int index){
			return categoryRowData[index].Values;
		}
		public void SetCategoryRowAt(string[][] vals, int index){
			categoryRowData[index] = new CategoryVector(vals);
		}
		public void RemoveCategoryRowAt(int index){
			categoryRowData.RemoveAt(index);
			CategoryRowNames.RemoveAt(index);
			CategoryRowDescriptions.RemoveAt(index);
		}
		public void AddCategoryRow(string name1, string description, string[][] vals){
			categoryRowData.Add(new CategoryVector(vals));
			while (CategoryRowNames.Contains(name1)){
				name1 += "_";
			}
			CategoryRowNames.Add(name1);
			CategoryRowDescriptions.Add(description);
		}
		public void ClearCategoryRows(){
			CategoryRowNames = new List<string>();
			CategoryRowDescriptions = new List<string>();
			categoryRowData.Clear();
		}
		public void AddNumericRow(string name1, string description, double[] vals){
			NumericRows.Add(vals);
			while (NumericRowNames.Contains(name1)){
				name1 += "_";
			}
			NumericRowNames.Add(name1);
			NumericRowDescriptions.Add(description);
		}
		public void AddCategoryRows(IList<string> names, IList<string> descriptions, IList<string[][]> vals){
			for (int i = 0; i < names.Count; i++){
				AddCategoryRow(names[i], descriptions[i], vals[i]);
			}
		}
		public void RemoveNumericRowAt(int index){
			NumericRows.RemoveAt(index);
			NumericRowNames.RemoveAt(index);
			NumericRowDescriptions.RemoveAt(index);
		}
		public void ClearStringRows(){
			StringRowNames = new List<string>();
			StringRowDescriptions = new List<string>();
			StringRows = new List<string[]>();
		}
		public void AddStringRow(string name1, string description, string[] vals){
			StringRows.Add(vals);
			while (StringRowNames.Contains(name1)){
				name1 += "_";
			}
			StringRowNames.Add(name1);
			StringRowDescriptions.Add(description);
		}
		public void RemoveStringRowAt(int index){
			StringRows.RemoveAt(index);
			StringRowNames.RemoveAt(index);
			StringRowDescriptions.RemoveAt(index);
		}
		public void ClearMultiNumericRows(){
			MultiNumericRowNames = new List<string>();
			MultiNumericRowDescriptions = new List<string>();
			MultiNumericRows = new List<double[][]>();
		}
		public void AddMultiNumericRow(string name1, string description, double[][] vals){
			MultiNumericRows.Add(vals);
			while (MultiNumericRowNames.Contains(name1)){
				name1 += "_";
			}
			MultiNumericRowNames.Add(name1);
			MultiNumericRowDescriptions.Add(description);
		}
		public void RemoveMultiNumericRowAt(int index){
			MultiNumericRows.RemoveAt(index);
			MultiNumericRowNames.RemoveAt(index);
			MultiNumericRowDescriptions.RemoveAt(index);
		}
		public void ClearNumericRows(){
			NumericRowNames = new List<string>();
			NumericRowDescriptions = new List<string>();
			NumericRows = new List<double[]>();
		}
		public int MainColumnRowCount => ColumnNames?.Count ?? 0;
		public int MainColumnDescriptionRowCount => ColumnNames?.Count ?? 0;
		public int NumericRowCount => NumericRowNames?.Count ?? 0;
		public int CategoryRowCount => CategoryRowNames?.Count ?? 0;
		public int MultiNumericRowCount => MultiNumericRowNames?.Count ?? 0;
		public int StringRowCount => StringRowNames?.Count ?? 0;
		public void Clear(){
			ColumnNames = new List<string>();
			ColumnDescriptions = new List<string>();
			CategoryRowNames = new List<string>();
			CategoryRowDescriptions = new List<string>();
			NumericRowNames = new List<string>();
			NumericRowDescriptions = new List<string>();
			NumericRows = new List<double[]>();
			MultiNumericRowNames = new List<string>();
			MultiNumericRowDescriptions = new List<string>();
			MultiNumericRows = new List<double[][]>();
			StringRowNames = new List<string>();
			StringRowDescriptions = new List<string>();
			StringRows = new List<string[]>();
			categoryRowData = new List<CategoryVector>();
		}
		public virtual void ExtractColumns(int[] cols){
			ColumnNames = ColumnNames.SubList(cols);
			//      ColumnDescriptions = ArrayUtils.SubList(ColumnDescriptions, cols);
			for (int i = 0; i < CategoryRowCount; i++){
				categoryRowData[i] = categoryRowData[i].SubArray(cols);
			}
			for (int i = 0; i < NumericRows.Count; i++){
				NumericRows[i] = NumericRows[i].SubArray(cols);
			}
			for (int i = 0; i < MultiNumericRows.Count; i++){
				MultiNumericRows[i] = MultiNumericRows[i].SubArray(cols);
			}
			for (int i = 0; i < StringRows.Count; i++){
				StringRows[i] = StringRows[i].SubArray(cols);
			}
		}
		public void Dispose(){
			ColumnNames = null;
			ColumnDescriptions = null;
			CategoryRowNames = null;
			CategoryRowDescriptions = null;
			NumericRowNames = null;
			NumericRowDescriptions = null;
			MultiNumericRowNames = null;
			MultiNumericRowDescriptions = null;
			StringRowNames = null;
			StringRowDescriptions = null;
			if (categoryRowData != null){
				categoryRowData.Clear();
				categoryRowData = null;
			}
			if (NumericRows != null){
				NumericRows.Clear();
				NumericRows = null;
			}
			if (MultiNumericRows != null){
				MultiNumericRows.Clear();
				MultiNumericRows = null;
			}
			if (StringRows != null){
				StringRows.Clear();
				StringRows = null;
			}
		}
		public int ColumnCount => ColumnNames?.Count ?? 0;
		public void SetAnnotationRows(List<string> stringRowNames, List<string> stringRowDescriptions,
			List<string[]> stringRows, List<string> categoryRowNames, List<string> categoryRowDescriptions,
			List<string[][]> categoryRows, List<string> numericRowNames, List<string> numericRowDescriptions,
			List<double[]> numericRows, List<string> multiNumericRowNames, List<string> multiNumericRowDescriptions,
			List<double[][]> multiNumericRows){
			CategoryRowNames = categoryRowNames;
			CategoryRowDescriptions = categoryRowDescriptions;
			CategoryRows = categoryRows;
			NumericRowNames = numericRowNames;
			NumericRowDescriptions = numericRowDescriptions;
			NumericRows = numericRows;
			StringRowNames = stringRowNames;
			StringRowDescriptions = stringRowDescriptions;
			StringRows = stringRows;
			MultiNumericRowNames = multiNumericRowNames;
			MultiNumericRowDescriptions = multiNumericRowDescriptions;
			MultiNumericRows = multiNumericRows;
		}
		public void SetAnnotationRows(List<string> stringRowNames, List<string[]> stringRows,
			List<string> categoryRowNames,
			List<string[][]> categoryRows, List<string> numericRowNames, List<double[]> numericRows,
			List<string> multiNumericRowNames, List<double[][]> multiNumericRows){
			CategoryRowNames = categoryRowNames;
			CategoryRowDescriptions = CreateEmpty(categoryRowNames, "");
			CategoryRows = categoryRows;
			NumericRowNames = numericRowNames;
			NumericRowDescriptions = CreateEmpty(numericRowNames, "");
			NumericRows = numericRows;
			StringRowNames = stringRowNames;
			StringRowDescriptions = CreateEmpty(stringRowNames, "");
			StringRows = stringRows;
			MultiNumericRowNames = multiNumericRowNames;
			MultiNumericRowDescriptions = CreateEmpty(multiNumericRowNames, "");
			MultiNumericRows = multiNumericRows;
		}
		public void ClearAnnotationRows(){
			SetAnnotationRows(new List<string>(), new List<string[]>(), new List<string>(),
				new List<string[][]>(), new List<string>(), new List<double[]>(),
				new List<string>(), new List<double[][]>());
		}
		public void Clone(IDataWithAnnotationRows clone){
			clone.ColumnNames = CloneX(ColumnNames);
			clone.ColumnDescriptions = CloneX(ColumnDescriptions);
			clone.CategoryRowNames = CloneX(CategoryRowNames);
			clone.CategoryRowDescriptions = CloneX(CategoryRowDescriptions);
			clone.NumericRowNames = CloneX(NumericRowNames);
			clone.NumericRowDescriptions = CloneX(NumericRowDescriptions);
			clone.MultiNumericRowNames = CloneX(MultiNumericRowNames);
			clone.MultiNumericRowDescriptions = CloneX(MultiNumericRowDescriptions);
			clone.StringRowNames = CloneX(StringRowNames);
			clone.StringRowDescriptions = CloneX(StringRowDescriptions);
			List<string[][]> categoryRows = new List<string[][]>();
			foreach (CategoryVector column in categoryRowData){
				categoryRows.Add(column.GetAllData());
			}
			clone.CategoryRows = categoryRows;
			clone.NumericRows = new List<double[]>();
			foreach (double[] s in NumericRows){
				clone.NumericRows.Add((double[]) s.Clone());
			}
			clone.MultiNumericRows = new List<double[][]>();
			foreach (double[][] s in MultiNumericRows){
				clone.MultiNumericRows.Add((double[][]) s.Clone());
			}
			clone.StringRows = new List<string[]>();
			foreach (string[] s in StringRows){
				clone.StringRows.Add((string[]) s.Clone());
			}
		}
		protected static List<string> CloneX(IEnumerable<string> x){
			List<string> result = new List<string>();
			foreach (string s in x){
				result.Add(s);
			}
			return result;
		}
		public static List<T> CreateEmpty<T>(IList<T> x, T y){
			List<T> result = new List<T>();
			for (int i = 0; i < x.Count; i++){
				result.Add(y);
			}
			return result;
		}
		public bool Equals(IDataWithAnnotationRows other){
			if (other == null){
				return false;
			}
			return ColumnNames.SequenceEqual(other.ColumnNames)
			       && ColumnDescriptions.SequenceEqual(other.ColumnDescriptions)
			       && StringRowNames.SequenceEqual(other.StringRowNames)
			       && StringRows.Zip(other.StringRows,
				       (strings, otherStrings) => strings.SequenceEqual(otherStrings)).All(x => x)
			       && NumericRowNames.SequenceEqual(other.NumericRowNames)
			       && NumericRows.Zip(other.NumericRows,
				       (numerics, otherNumerics) => numerics.SequenceEqual(otherNumerics)).All(x => x)
			       && MultiNumericRowNames.SequenceEqual(other.MultiNumericRowNames)
			       && MultiNumericRows.Zip(other.MultiNumericRows, TwoDimEquality).All(x => x)
			       && CategoryRowNames.SequenceEqual(other.CategoryRowNames)
			       && Enumerable.Range(0, CategoryRowCount).All(col => {
				       string[][] multiNumerics = GetCategoryRowAt(col);
				       string[][] otherMultiNumerics = other.GetCategoryRowAt(col);
				       return TwoDimEquality(multiNumerics, otherMultiNumerics);
			       });
		}
		private static bool TwoDimEquality<T>(T[][] multiNumerics, T[][] otherMultiNumerics){
			if (multiNumerics.Length != otherMultiNumerics.Length){
				return false;
			}
			for (int i = 0; i < multiNumerics.Length; i++){
				T[] row = multiNumerics[i];
				T[] otherRow = otherMultiNumerics[i];
				if (!row.SequenceEqual(otherRow)){
					return false;
				}
			}
			return true;
		}
		public bool IsConsistent(out string errString){
			bool isConsistent = true;
			StringBuilder errBuilder = new StringBuilder();
			if (ColumnCount != ColumnNames.Count){
				errBuilder.AppendLine(
					$"Mismatch between column count {ColumnCount} and number of column names {ColumnNames}");
				isConsistent = false;
			}
			for (int i = 0; i < StringRowCount; i++){
				if (ColumnCount != StringRows[i].Length){
					errBuilder.AppendLine(
						$"Mismatch between column count {ColumnCount} and size {StringRows[i].Length} of annotation row '{StringRowNames[i]}'");
					isConsistent = false;
				}
			}
			for (int i = 0; i < NumericRowCount; i++){
				if (ColumnCount != NumericRows[i].Length){
					errBuilder.AppendLine(
						$"Mismatch between column count {ColumnCount} and size {NumericRows[i].Length} of annotation row '{NumericRowNames[i]}'");
					isConsistent = false;
				}
			}
			for (int i = 0; i < MultiNumericRowCount; i++){
				if (ColumnCount != MultiNumericRows[i].Length){
					errBuilder.AppendLine(
						$"Mismatch between column count {ColumnCount} and size {MultiNumericRows[i].Length} of annotation row '{MultiNumericRowNames[i]}'");
					isConsistent = false;
				}
			}
			for (int i = 0; i < CategoryRowCount; i++){
				string[][] categoryRows = GetCategoryRowAt(i);
				if (ColumnCount != categoryRows.Length){
					errBuilder.AppendLine(
						$"Mismatch between column count {ColumnCount} and size {categoryRows.Length} of annotation row '{CategoryRowNames[i]}'");
					isConsistent = false;
				}
			}
			errString = errBuilder.ToString();
			return isConsistent;
		}
	}
}