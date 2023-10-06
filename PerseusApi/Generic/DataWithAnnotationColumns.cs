using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BaseLibS.Data;
using BaseLibS.Data.Category;
using BaseLibS.Num;
using BaseLibS.Util;
namespace PerseusApi.Generic {
	[Serializable]
	public class DataWithAnnotationColumns : IDataWithAnnotationColumns {
		public List<string> CategoryColumnNames { get; set; }
		public List<string> NumericColumnNames { get; set; }
	    private List<string> stringColumnNames = new List<string>();
        public List<string> matrixNames = new List<string>();
        public List<string> MultiNumericColumnNames { get; set; }
		public List<string> CategoryColumnDescriptions { get; set; }
		public List<string> NumericColumnDescriptions { get; set; }
		public List<string> StringColumnDescriptions { get; set; }
		public List<string> MultiNumericColumnDescriptions { get; set; }
		private StringVectors stringColumnData = new StringVectors();
		private MultiNumericVectors multiNumericColumnData = new MultiNumericVectors();
		private List<CategoryVector> categoryColumnData = new List<CategoryVector>();
		public List<double[]> NumericColumns { get; set; }
		public DataWithAnnotationColumns() {
			CategoryColumnNames = new List<string>();
			CategoryColumnDescriptions = new List<string>();
			NumericColumnNames = new List<string>();
			NumericColumnDescriptions = new List<string>();
			NumericColumns = new List<double[]>();
			StringColumnDescriptions = new List<string>();
			MultiNumericColumnNames = new List<string>();
			MultiNumericColumnDescriptions = new List<string>();
			MultiNumericColumns = new List<double[][]>();
		}
		public DataWithAnnotationColumns(BinaryReader reader){
			CategoryColumnNames = new List<string>(FileUtils.ReadStringArray(reader));
			NumericColumnNames = new List<string>(FileUtils.ReadStringArray(reader));
			stringColumnNames = new List<string>(FileUtils.ReadStringArray(reader));
			matrixNames = new List<string>(FileUtils.ReadStringArray(reader));
			MultiNumericColumnNames = new List<string>(FileUtils.ReadStringArray(reader));
			CategoryColumnDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			NumericColumnDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			StringColumnDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			MultiNumericColumnDescriptions = new List<string>(FileUtils.ReadStringArray(reader));
			stringColumnData = new StringVectors(reader);
			multiNumericColumnData = new MultiNumericVectors(reader);
			int n = reader.ReadInt32();
			categoryColumnData = new List<CategoryVector>();
			for (int i = 0; i < n; i++){
				categoryColumnData.Add(new CategoryVector(reader));
			}
			n = reader.ReadInt32();
			NumericColumns = new List<double[]>();
			for (int i = 0; i < n; i++) {
				NumericColumns.Add(FileUtils.ReadDoubleArray(reader));
			}
		}
		public virtual void Write(BinaryWriter writer){
			FileUtils.Write(CategoryColumnNames.ToArray(), writer);
			FileUtils.Write(NumericColumnNames.ToArray(), writer);
			FileUtils.Write(stringColumnNames.ToArray(), writer);
			FileUtils.Write(matrixNames.ToArray(), writer);
			FileUtils.Write(MultiNumericColumnNames.ToArray(), writer);
			FileUtils.Write(CategoryColumnDescriptions.ToArray(), writer);
			FileUtils.Write(NumericColumnDescriptions.ToArray(), writer);
			FileUtils.Write(StringColumnDescriptions.ToArray(), writer);
			FileUtils.Write(MultiNumericColumnDescriptions.ToArray(), writer);
			stringColumnData.Write(writer);
			multiNumericColumnData.Write(writer);
			writer.Write(categoryColumnData.Count);
			foreach (CategoryVector vector in categoryColumnData){
				vector.Write(writer);
			}
			writer.Write(NumericColumns.Count);
			foreach (double[] column in NumericColumns){
				FileUtils.Write(column, writer);
			}
		}
		public List<double[][]> MultiNumericColumns {
			get => multiNumericColumnData.MultiNumericVecs;
			set => multiNumericColumnData.MultiNumericVecs = value;
		}
		public List<string[][]> CategoryColumns {
			set {
				categoryColumnData.Clear();
				foreach (string[][] strings in value) {
					categoryColumnData.Add(new CategoryVector(strings));
				}
			}
		}
		public List<string[]> StringColumns {
			get => stringColumnData.StringVecs;
			set => stringColumnData.StringVecs = value;
		}
	    public List<string> StringColumnNames
	    {
	        get => stringColumnNames;
	        set
	        {
	            stringColumnNames = value;
	            if (value != null && (StringColumnDescriptions == null || StringColumnDescriptions.Count == 0))
	            {
	                StringColumnDescriptions = CreateEmpty(stringColumnNames, "");
	            }
	        }
	    }



        public void CopyAnnotationColumnsFrom(IDataWithAnnotationColumns other) {
			NumericColumnNames = CloneX(other.NumericColumnNames);
			NumericColumnDescriptions = CloneX(other.NumericColumnDescriptions);
			MultiNumericColumnNames = CloneX(other.MultiNumericColumnNames);
			MultiNumericColumnDescriptions = CloneX(other.MultiNumericColumnDescriptions);
			StringColumnNames = CloneX(other.StringColumnNames);
			StringColumnDescriptions = CloneX(other.StringColumnDescriptions);
			CategoryColumns = new List<string[][]>();
			CategoryColumnNames = new List<string>();
			CategoryColumnDescriptions = new List<string>();
			categoryColumnData = new List<CategoryVector>();
			for (int i = 0; i < other.CategoryColumnCount; i++) {
				AddCategoryColumn(other.CategoryColumnNames[i], other.CategoryColumnDescriptions[i], other.GetCategoryColumnAt(i));
			}
			NumericColumns = new List<double[]>();
			foreach (double[] s in other.NumericColumns) {
				NumericColumns.Add((double[]) s.Clone());
			}
			MultiNumericColumns = new List<double[][]>();
			foreach (double[][] s in other.MultiNumericColumns) {
				MultiNumericColumns.Add((double[][]) s.Clone());
			}
			StringColumns = new List<string[]>();
			foreach (string[] s in other.StringColumns) {
				StringColumns.Add((string[]) s.Clone());
			}
		}
		public void CopyAnnotationColumnsFromRows(IDataWithAnnotationRows other) {
			NumericColumnNames = CloneX(other.NumericRowNames);
			NumericColumnDescriptions = CloneX(other.NumericRowDescriptions);
			MultiNumericColumnNames = CloneX(other.MultiNumericRowNames);
			MultiNumericColumnDescriptions = CloneX(other.MultiNumericRowDescriptions);
			StringColumnNames = CloneX(other.StringRowNames);
			StringColumnDescriptions = CloneX(other.StringRowDescriptions);
			CategoryColumns = new List<string[][]>();
			CategoryColumnNames = new List<string>();
			CategoryColumnDescriptions = new List<string>();
			categoryColumnData = new List<CategoryVector>();
			for (int i = 0; i < other.CategoryRowCount; i++) {
				AddCategoryColumn(other.CategoryRowNames[i], other.CategoryRowDescriptions[i], other.GetCategoryRowAt(i));
			}
			NumericColumns = new List<double[]>();
			foreach (double[] s in other.NumericRows) {
				NumericColumns.Add((double[]) s.Clone());
			}
			MultiNumericColumns = new List<double[][]>();
			foreach (double[][] s in other.MultiNumericRows) {
				MultiNumericColumns.Add((double[][]) s.Clone());
			}
			StringColumns = new List<string[]>();
			foreach (string[] s in other.StringRows) {
				StringColumns.Add((string[]) s.Clone());
			}
		}
		private int GetCategoryColumnLengthAt(int column) {
			return categoryColumnData[column].Length;
		}
		public string[][] GetCategoryColumnAt(int column) {
			return categoryColumnData[column].GetAllData();
		}
		public string[] GetCategoryColumnEntryAt(int column, int row) {
			return categoryColumnData[column][row];
		}
		public string[][] GetCategoryColumnsEntriesAt(int row) {
			string[][] entries = new string[CategoryColumnCount][];
			for (int col = 0; col < CategoryColumnCount; col++) {
				entries[col] = GetCategoryColumnEntryAt(col, row);
			}
			return entries;
		}
		public string[] GetCategoryColumnValuesAt(int column) {
			return categoryColumnData[column].Values;
		}
		public void SetCategoryColumnAt(string[][] vals, int column) {
			categoryColumnData[column] = new CategoryVector(vals);
		}
		public void RemoveCategoryColumnAt(int index) {
			categoryColumnData.RemoveAt(index);
			CategoryColumnNames.RemoveAt(index);
			CategoryColumnDescriptions.RemoveAt(index);
		}
		public void AddCategoryColumn(string name1, string description, string[][] vals) {
			categoryColumnData.Add(new CategoryVector(vals));
			while (CategoryColumnNames.Contains(name1)) {
				name1 += "_";
			}
			CategoryColumnNames.Add(name1);
			CategoryColumnDescriptions.Add(description);
		}
		public double NumericColumnAt(int column, int row) {
			return NumericColumns[column][row];
		}
		public void ClearCategoryColumns() {
			CategoryColumnNames = new List<string>();
			CategoryColumnDescriptions = new List<string>();
			categoryColumnData.Clear();
		}
		public void AddNumericColumn(string name1, string description, double[] vals) {
			NumericColumns.Add(vals);
			while (NumericColumnNames.Contains(name1)) {
				name1 += "_";
			}
			NumericColumnNames.Add(name1);
			NumericColumnDescriptions.Add(description);
		}
		public void RemoveNumericColumnAt(int index) {
			NumericColumns.RemoveAt(index);
			NumericColumnNames.RemoveAt(index);
			NumericColumnDescriptions.RemoveAt(index);
		}
		public void ClearStringColumns() {
			StringColumnNames = new List<string>();
			StringColumnDescriptions = new List<string>();
			StringColumns = new List<string[]>();
		}
		public void AddStringColumn(string name1, string description, string[] vals) {
            StringColumns.Add(vals.Select(v => v ?? string.Empty).ToArray());
			while (StringColumnNames.Contains(name1)) {
				name1 += "_";
			}
			StringColumnNames.Add(name1);
			StringColumnDescriptions.Add(description);
		}
		public void RemoveStringColumnAt(int index) {
			StringColumns.RemoveAt(index);
			StringColumnNames.RemoveAt(index);
			StringColumnDescriptions.RemoveAt(index);
		}
		public string StringColumnAt(int column, int row) {
			return StringColumns[column][row];
		}
		public void ClearMultiNumericColumns() {
			MultiNumericColumnNames = new List<string>();
			MultiNumericColumnDescriptions = new List<string>();
			MultiNumericColumns = new List<double[][]>();
		}
		public void AddMultiNumericColumn(string name1, string description, double[][] vals) {
			MultiNumericColumns.Add(vals);
			while (MultiNumericColumnNames.Contains(name1)) {
				name1 += "_";
			}
			MultiNumericColumnNames.Add(name1);
			MultiNumericColumnDescriptions.Add(description);
		}
		public void RemoveMultiNumericColumnAt(int index) {
			MultiNumericColumns.RemoveAt(index);
			MultiNumericColumnNames.RemoveAt(index);
			MultiNumericColumnDescriptions.RemoveAt(index);
		}
		public double[] MultiNumericColumnAt(int column, int row) {
			return MultiNumericColumns[column][row];
		}
		public void ClearNumericColumns() {
			NumericColumnNames = new List<string>();
			NumericColumnDescriptions = new List<string>();
			NumericColumns = new List<double[]>();
		}
		public int NumericColumnCount => NumericColumnNames?.Count ?? 0;
		public int CategoryColumnCount => CategoryColumnNames?.Count ?? 0;
		public int MultiNumericColumnCount => MultiNumericColumnNames?.Count ?? 0;
		public int StringColumnCount => StringColumnNames?.Count ?? 0;
		public void Clear() {
			CategoryColumnNames = new List<string>();
			CategoryColumnDescriptions = new List<string>();
			NumericColumnNames = new List<string>();
			NumericColumnDescriptions = new List<string>();
			NumericColumns = new List<double[]>();
			MultiNumericColumnNames = new List<string>();
			MultiNumericColumnDescriptions = new List<string>();
			MultiNumericColumns = new List<double[][]>();
			StringColumnNames = new List<string>();
			StringColumnDescriptions = new List<string>();
			StringColumns = new List<string[]>();
			categoryColumnData = new List<CategoryVector>();
		}
		public virtual void ExtractRows(int[] cols) {
			for (int i = 0; i < CategoryColumnCount; i++) {
				categoryColumnData[i] = categoryColumnData[i].SubArray(cols);
			}
			for (int i = 0; i < NumericColumns.Count; i++) {
				NumericColumns[i] = NumericColumns[i].SubArray(cols);
			}
			for (int i = 0; i < MultiNumericColumns.Count; i++) {
				MultiNumericColumns[i] = MultiNumericColumns[i].SubArray(cols);
			}
			for (int i = 0; i < StringColumns.Count; i++) {
				StringColumns[i] = StringColumns[i].SubArray(cols);
			}
		}
		public void Dispose() {
			CategoryColumnNames = null;
			CategoryColumnDescriptions = null;
			NumericColumnNames = null;
			NumericColumnDescriptions = null;
			MultiNumericColumnNames = null;
			MultiNumericColumnDescriptions = null;
			StringColumnNames = null;
			StringColumnDescriptions = null;
			if (categoryColumnData != null) {
				categoryColumnData.Clear();
				categoryColumnData = null;
			}
			if (NumericColumns != null) {
				NumericColumns.Clear();
				NumericColumns = null;
			}
			if (MultiNumericColumns != null) {
				MultiNumericColumns.Clear();
				MultiNumericColumns = null;
			}
			if (StringColumns != null) {
				StringColumns.Clear();
				StringColumns = null;
			}
		}
		public virtual int RowCount => new[] {
			StringColumns.Select(x => x.Length), NumericColumns.Select(x => x.Length), MultiNumericColumns.Select(x => x.Length),
			Enumerable.Range(0, CategoryColumnCount).Select(GetCategoryColumnLengthAt)
		}.Max(x => x.DefaultIfEmpty(0).Max());
		public void SetAnnotationColumns(List<string> stringColumnNames, List<string> stringColumnDescriptions,
			List<string[]> stringColumns, List<string> categoryColumnNames, List<string> categoryColumnDescriptions,
			List<string[][]> categoryColumns, List<string> numericColumnNames, List<string> numericColumnDescriptions,
			List<double[]> numericColumns, List<string> multiNumericColumnNames, List<string> multiNumericColumnDescriptions,
			List<double[][]> multiNumericColumns) {
			CategoryColumnNames = categoryColumnNames;
			CategoryColumnDescriptions = categoryColumnDescriptions;
			CategoryColumns = categoryColumns;
			NumericColumnNames = numericColumnNames;
			NumericColumnDescriptions = numericColumnDescriptions;
			NumericColumns = numericColumns;
			StringColumnNames = stringColumnNames;
			StringColumnDescriptions = stringColumnDescriptions;
			StringColumns = stringColumns;
			MultiNumericColumnNames = multiNumericColumnNames;
			MultiNumericColumnDescriptions = multiNumericColumnDescriptions;
			MultiNumericColumns = multiNumericColumns;
		}
		public void SetAnnotationColumns(List<string> stringColumnNames, List<string[]> stringColumns,
			List<string> categoryColumnNames, List<string[][]> categoryColumns, List<string> numericColumnNames,
			List<double[]> numericColumns, List<string> multiNumericColumnNames, List<double[][]> multiNumericColumns) {
			CategoryColumnNames = categoryColumnNames;
			CategoryColumnDescriptions = CreateEmpty(categoryColumnNames, "");
			CategoryColumns = categoryColumns;
			NumericColumnNames = numericColumnNames;
			NumericColumnDescriptions = CreateEmpty(numericColumnNames, "");
			NumericColumns = numericColumns;
			StringColumnNames = stringColumnNames;
			StringColumnDescriptions = CreateEmpty(stringColumnNames, "");
			StringColumns = stringColumns;
			MultiNumericColumnNames = multiNumericColumnNames;
			MultiNumericColumnDescriptions = CreateEmpty(multiNumericColumnNames, "");
			MultiNumericColumns = multiNumericColumns;
		}
		public void ClearAnnotationColumns() {
			SetAnnotationColumns(new List<string>(), new List<string[]>(), new List<string>(), new List<string[][]>(),
				new List<string>(), new List<double[]>(), new List<string>(), new List<double[][]>());
		}
		public void Clone(IDataWithAnnotationColumns clone) {
			clone.CategoryColumnNames = CloneX(CategoryColumnNames);
			clone.CategoryColumnDescriptions = CloneX(CategoryColumnDescriptions);
			clone.NumericColumnNames = CloneX(NumericColumnNames);
			clone.NumericColumnDescriptions = CloneX(NumericColumnDescriptions);
			clone.MultiNumericColumnNames = CloneX(MultiNumericColumnNames);
			clone.MultiNumericColumnDescriptions = CloneX(MultiNumericColumnDescriptions);
			clone.StringColumnNames = CloneX(StringColumnNames);
			clone.StringColumnDescriptions = CloneX(StringColumnDescriptions);
			List<string[][]> categoryColumns = new List<string[][]>();
			foreach (CategoryVector s in categoryColumnData) {
				CategoryVector row = s.Copy();
				categoryColumns.Add(row.GetAllData());
			}
			clone.CategoryColumns = categoryColumns;
			clone.NumericColumns = new List<double[]>();
			foreach (double[] s in NumericColumns) {
				clone.NumericColumns.Add((double[]) s.Clone());
			}
			clone.MultiNumericColumns = new List<double[][]>();
			foreach (double[][] s in MultiNumericColumns) {
				clone.MultiNumericColumns.Add((double[][]) s.Clone());
			}
			clone.StringColumns = new List<string[]>();
			foreach (string[] s in StringColumns) {
				clone.StringColumns.Add((string[]) s.Clone());
			}
		}
		protected static List<string> CloneX(IEnumerable<string> x) {
			List<string> result = new List<string>();
			foreach (string s in x) {
				result.Add(s);
			}
			return result;
		}
		public static List<T> CreateEmpty<T>(IList<T> x, T y) {
			List<T> result = new List<T>();
			for (int i = 0; i < x.Count; i++) {
				result.Add(y);
			}
			return result;
		}
        public bool Equals(IDataWithAnnotationColumns other)
        {
            if (other == null)
            {
                return false;
            }
            return StringColumnNames.SequenceEqual(other.StringColumnNames)
                   && StringColumns.Zip(other.StringColumns,
                       (strings, otherStrings) => strings.SequenceEqual(otherStrings)).All(x => x)
                   && NumericColumnNames.SequenceEqual(other.NumericColumnNames)
                   && NumericColumns.Zip(other.NumericColumns,
                       (numerics, otherNumerics) => numerics.SequenceEqual(otherNumerics)).All(x => x)
                   && MultiNumericColumnNames.SequenceEqual(other.MultiNumericColumnNames)
                   && MultiNumericColumns.Zip(other.MultiNumericColumns, TwoDimEquality).All(x => x)
                   && CategoryColumnNames.SequenceEqual(other.CategoryColumnNames)
                   && Enumerable.Range(0, CategoryColumnCount).All(col =>
                   {
                       string[][] multiNumerics = GetCategoryColumnAt(col);
                       string[][] otherMultiNumerics = other.GetCategoryColumnAt(col);
                       return TwoDimEquality(multiNumerics, otherMultiNumerics);
                   });
        }
        private static bool TwoDimEquality<T>(T[][] multiNumerics, T[][] otherMultiNumerics)
        {
            if (multiNumerics.Length != otherMultiNumerics.Length)
            {
                return false;
            }
            for (int i = 0; i < multiNumerics.Length; i++)
            {
                T[] row = multiNumerics[i];
                T[] otherRow = otherMultiNumerics[i];
                if (!row.SequenceEqual(otherRow))
                {
                    return false;
                }
            }
            return true;
        }

	    public bool IsConsistent(out string errString)
	    {
	        StringBuilder errBuilder = new StringBuilder();
	        int length<T>(T[] arr) => arr.Length;
	        int[] rowCounts = StringColumns.Select(length).Concat(NumericColumns.Select(length))
	            .Concat(Enumerable.Range(0, CategoryColumnCount).Select(GetCategoryColumnLengthAt))
	            .Concat(MultiNumericColumns.Select(length)).Distinct().ToArray();
	        if (rowCounts.Length > 1)
	        {
	            errBuilder.AppendLine($"Inconsistent column lengths of {{{string.Join(", ", rowCounts)}}}.");
	        }
	        errString = errBuilder.ToString();
	        return string.IsNullOrEmpty(errString);
	    }
    }
}