using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseLibS.Calc;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Utils {
	public static class PerseusUtils {
		public static readonly HashSet<string> commentPrefix = new HashSet<string>(new[] {"#", "!"});
		public static readonly HashSet<string> commentPrefixExceptions = new HashSet<string>(new[] {"#N/A", "#n/a"});

		public static void LoadMatrixData(IDictionary<string, string[]> annotationRows, int[] mainColIndices,
			int[] catColIndices, int[] numColIndices, int[] textColIndices, int[] multiNumColIndices, ProcessInfo processInfo,
			IList<string> colNames, IMatrixData mdata, StreamReader reader, StreamReader auxReader, int nrows, string origin,
			char separator, bool shortenExpressionNames, List<Tuple<Relation[], int[], bool>> filters) {
			bool hasAdditionalMatrices = GetHasAddtlMatrices(auxReader, mainColIndices, separator);
			LoadMatrixData(annotationRows, mainColIndices, catColIndices, numColIndices, textColIndices, multiNumColIndices,
				processInfo, colNames, mdata, reader, nrows, origin, separator, shortenExpressionNames, hasAdditionalMatrices,
				filters);
		}

		public static void LoadMatrixData(IDictionary<string, string[]> annotationRows, int[] mainColIndices,
			int[] catColIndices, int[] numColIndices, int[] textColIndices, int[] multiNumColIndices, ProcessInfo processInfo,
			IList<string> colNames, IMatrixData mdata, StreamReader reader, int nrows, string origin, char separator,
			bool shortenExpressionNames, bool hasAdditionalMatrices, List<Tuple<Relation[], int[], bool>> filters) {
			string[] colDescriptions = null;
			if (annotationRows.ContainsKey("Description")) {
				colDescriptions = annotationRows["Description"];
				annotationRows.Remove("Description");
			}
			if (HasBadParameters(mainColIndices, catColIndices, numColIndices, textColIndices, multiNumColIndices, processInfo,
				colNames)) return;
			LoadMatrixData(colNames, colDescriptions, mainColIndices, catColIndices, numColIndices, textColIndices,
				multiNumColIndices, origin, mdata, processInfo.Progress, processInfo.Status, separator, reader, nrows,
				shortenExpressionNames, filters, hasAdditionalMatrices);
			AddAnnotationRows(mainColIndices, mdata, annotationRows);
		}

		/// <summary>
		/// Check for duplicate column selections
		/// </summary>
		private static bool HasBadParameters(int[] mainColIndices, int[] catColIndices, int[] numColIndices,
			int[] textColIndices, int[] multiNumColIndices, ProcessInfo processInfo, IList<string> colNames) {
			int[] allInds = ArrayUtils.Concat(new[]
				{mainColIndices, catColIndices, numColIndices, textColIndices, multiNumColIndices});
			Array.Sort(allInds);
			for (int i = 0; i < allInds.Length - 1; i++) {
				if (allInds[i + 1] == allInds[i]) {
					processInfo.ErrString = "Column '" + colNames[allInds[i]] + "' has been selected multiple times";
					return true;
				}
			}
			string[] allColNames = ArrayUtils.SubArray(colNames, allInds);
			Array.Sort(allColNames);
			for (int i = 0; i < allColNames.Length - 1; i++) {
				if (allColNames[i + 1].Equals(allColNames[i])) {
					processInfo.ErrString = "Column name '" + allColNames[i] + "' occurs multiple times.";
					return true;
				}
			}
			return false;
		}

		private static void LoadMatrixData(IList<string> colNames, IList<string> colDescriptions, IList<int> mainColIndices,
			IList<int> catColIndices, IList<int> numColIndices, IList<int> textColIndices, IList<int> multiNumColIndices,
			string origin, IMatrixData matrixData, Action<int> progress, Action<string> status, char separator,
			TextReader reader, int nrows, bool shortenExpressionNames, List<Tuple<Relation[], int[], bool>> filters,
			bool addtlMatrices) {
			status("Reading data");
			LoadAllData(matrixData, colNames, mainColIndices, catColIndices, numColIndices, textColIndices, multiNumColIndices,
				reader, separator, nrows, filters, progress, addtlMatrices, out double[,] qualityValues,
				out bool[,] isImputedValues, out double[,] mainValues);
			AddColumnDescriptions(colDescriptions, catColIndices, numColIndices, textColIndices, multiNumColIndices, matrixData);
			AddMainColumnDescriptions(colDescriptions, mainColIndices, matrixData);
			matrixData.Name = origin;
			string[] columnNames = ArrayUtils.SubArray(colNames, mainColIndices);
			if (shortenExpressionNames) {
				columnNames = StringUtils.RemoveCommonSubstrings(columnNames, true);
			}
			matrixData.ColumnNames = RemoveQuotes(columnNames);
			matrixData.Values.Set(mainValues);
			if (addtlMatrices) {
				matrixData.Quality.Set(qualityValues);
				matrixData.IsImputed.Set(isImputedValues);
			} else {
				matrixData.Quality.Set(new double[mainValues.GetLength(0), mainValues.GetLength(1)]);
				matrixData.IsImputed.Set(new bool[mainValues.GetLength(0), mainValues.GetLength(1)]);
			}
			matrixData.Origin = origin;
			progress(0);
			status("");
		}

		public static void LoadDataWithAnnotationColumns(IDataWithAnnotationColumns matrixData, IList<string> colNames,
			IList<int> catColIndices, IList<int> numColIndices, IList<int> textColIndices, IList<int> multiNumColIndices,
			Action<int> progress, char separator, TextReader reader, int nrows, List<Tuple<Relation[], int[], bool>> filters) {
			LoadAllData(matrixData, colNames, new int[0], catColIndices, numColIndices, textColIndices, multiNumColIndices,
				reader, separator, nrows, filters, progress, false, out double[,] _, out bool[,] _, out double[,] _);
		}

		private static void LoadAllData(IDataWithAnnotationColumns matrixData, IList<string> colNames,
			IList<int> mainColIndices, IList<int> catColIndices, IList<int> numColIndices, IList<int> textColIndices,
			IList<int> multiNumColIndices, TextReader reader, char separator, int nrows,
			List<Tuple<Relation[], int[], bool>> filters, Action<int> progress, bool addtlMatrices, out double[,] qualityValues,
			out bool[,] isImputedValues, out double[,] mainValues) {
			InitializeAnnotationColumns(catColIndices, numColIndices, textColIndices, multiNumColIndices, nrows,
				out List<string[][]> categoryAnnotation, out List<double[]> numericAnnotation,
				out List<double[][]> multiNumericAnnotation, out List<string[]> stringAnnotation);
			mainValues = InitializeMainValues(mainColIndices, nrows, addtlMatrices, out qualityValues, out isImputedValues);
			reader.ReadLine();
			int count = 0;
			string line;
			while ((line = reader.ReadLine()) != null) {
				if (SkipCommentOrInvalid(separator, filters, addtlMatrices, line, out string[] words)) continue;
				progress(100 * (count + 1) / nrows);
				ReadMainColumns(mainColIndices, addtlMatrices, words, mainValues, count, isImputedValues, qualityValues);
				ReadAnnotationColumns(catColIndices, numColIndices, textColIndices, multiNumColIndices, words, numericAnnotation,
					count, multiNumericAnnotation, categoryAnnotation, stringAnnotation);
				count++;
			}
			reader.Close();
			string[] catColnames = ArrayUtils.SubArray(colNames, catColIndices);
			string[] numColnames = ArrayUtils.SubArray(colNames, numColIndices);
			string[] multiNumColnames = ArrayUtils.SubArray(colNames, multiNumColIndices);
			string[] textColnames = ArrayUtils.SubArray(colNames, textColIndices);
			matrixData.SetAnnotationColumns(RemoveQuotes(textColnames), stringAnnotation, RemoveQuotes(catColnames),
				categoryAnnotation, RemoveQuotes(numColnames), numericAnnotation, RemoveQuotes(multiNumColnames),
				multiNumericAnnotation);
		}

		private static void AddMainColumnDescriptions(IList<string> colDescriptions, IList<int> mainColIndices,
			IMatrixData matrixData) {
			if (colDescriptions != null) {
				string[] columnDesc = ArrayUtils.SubArray(colDescriptions, mainColIndices);
				matrixData.ColumnDescriptions = new List<string>(columnDesc);
			}
		}

		private static void AddColumnDescriptions(IList<string> colDescriptions, IList<int> catColIndices,
			IList<int> numColIndices, IList<int> textColIndices, IList<int> multiNumColIndices,
			IDataWithAnnotationColumns matrixData) {
			if (colDescriptions != null) {
				string[] catColDesc = ArrayUtils.SubArray(colDescriptions, catColIndices);
				string[] numColDesc = ArrayUtils.SubArray(colDescriptions, numColIndices);
				string[] multiNumColDesc = ArrayUtils.SubArray(colDescriptions, multiNumColIndices);
				string[] textColDesc = ArrayUtils.SubArray(colDescriptions, textColIndices);
				matrixData.NumericColumnDescriptions = new List<string>(numColDesc);
				matrixData.CategoryColumnDescriptions = new List<string>(catColDesc);
				matrixData.StringColumnDescriptions = new List<string>(textColDesc);
				matrixData.MultiNumericColumnDescriptions = new List<string>(multiNumColDesc);
			}
		}

		private static void AddAnnotationRows(IList<int> mainColIndices, IDataWithAnnotationRows matrixData,
			IDictionary<string, string[]> annotationRows) {
			SplitAnnotRows(annotationRows, out Dictionary<string, string[]> catAnnotatRows,
				out Dictionary<string, string[]> numAnnotatRows);
			foreach (string key in catAnnotatRows.Keys) {
				string name = key;
				string[] svals = ArrayUtils.SubArray(catAnnotatRows[key], mainColIndices);
				string[][] cat = new string[svals.Length][];
				for (int i = 0; i < cat.Length; i++) {
					string s = svals[i].Trim();
					cat[i] = s.Length > 0 ? s.Split(';') : new string[0];
					List<int> valids = new List<int>();
					for (int j = 0; j < cat[i].Length; j++) {
						cat[i][j] = cat[i][j].Trim();
						if (cat[i][j].Length > 0) {
							valids.Add(j);
						}
					}
					cat[i] = ArrayUtils.SubArray(cat[i], valids);
					Array.Sort(cat[i]);
				}
				matrixData.AddCategoryRow(name, name, cat);
			}
			foreach (string key in numAnnotatRows.Keys) {
				string name = key;
				string[] svals = ArrayUtils.SubArray(numAnnotatRows[key], mainColIndices);
				double[] num = new double[svals.Length];
				for (int i = 0; i < num.Length; i++) {
					string s = svals[i].Trim();
					if (!Parser.TryDouble(s, out num[i])) {
						num[i] = double.NaN;
					}
				}
				matrixData.AddNumericRow(name, name, num);
			}
		}

		private static void ReadAnnotationColumns(IList<int> catColIndices, IList<int> numColIndices,
			IList<int> textColIndices, IList<int> multiNumColIndices, string[] words, List<double[]> numericAnnotation,
			int count, List<double[][]> multiNumericAnnotation, List<string[][]> categoryAnnotation,
			List<string[]> stringAnnotation) {
			for (int i = 0; i < numColIndices.Count; i++) {
				if (numColIndices[i] >= words.Length) {
					numericAnnotation[i][count] = double.NaN;
				} else {
					bool success = Parser.TryDouble(words[numColIndices[i]].Trim(), out double q);
					if (numericAnnotation[i].Length > count) {
						numericAnnotation[i][count] = success ? q : double.NaN;
					}
				}
			}
			for (int i = 0; i < multiNumColIndices.Count; i++) {
				if (multiNumColIndices[i] >= words.Length) {
					multiNumericAnnotation[i][count] = new double[0];
				} else {
					string q = words[multiNumColIndices[i]].Trim();
					if (q.Length >= 2 && q[0] == '\"' && q[q.Length - 1] == '\"') {
						q = q.Substring(1, q.Length - 2);
					}
					if (q.Length >= 2 && q[0] == '\'' && q[q.Length - 1] == '\'') {
						q = q.Substring(1, q.Length - 2);
					}
					string[] ww = q.Length == 0 ? new string[0] : q.Split(';');
					multiNumericAnnotation[i][count] = new double[ww.Length];
					for (int j = 0; j < ww.Length; j++) {
						bool success = Parser.TryDouble(ww[j], out double q1);
						multiNumericAnnotation[i][count][j] = success ? q1 : double.NaN;
					}
				}
			}
			for (int i = 0; i < catColIndices.Count; i++) {
				if (catColIndices[i] >= words.Length) {
					categoryAnnotation[i][count] = new string[0];
				} else {
					string q = words[catColIndices[i]].Trim();
					if (q.Length >= 2 && q[0] == '\"' && q[q.Length - 1] == '\"') {
						q = q.Substring(1, q.Length - 2);
					}
					if (q.Length >= 2 && q[0] == '\'' && q[q.Length - 1] == '\'') {
						q = q.Substring(1, q.Length - 2);
					}
					string[] ww = q.Length == 0 ? new string[0] : q.Split(';');
					List<int> valids = new List<int>();
					for (int j = 0; j < ww.Length; j++) {
						ww[j] = ww[j].Trim();
						if (ww[j].Length > 0) {
							valids.Add(j);
						}
					}
					ww = ArrayUtils.SubArray(ww, valids);
					Array.Sort(ww);
					if (categoryAnnotation[i].Length > count) {
						categoryAnnotation[i][count] = ww;
					}
				}
			}
			for (int i = 0; i < textColIndices.Count; i++) {
				if (textColIndices[i] >= words.Length) {
					stringAnnotation[i][count] = "";
				} else {
					string q = words[textColIndices[i]].Trim();
					if (stringAnnotation[i].Length > count) {
						stringAnnotation[i][count] = RemoveSplitWhitespace(RemoveQuotes(q));
					}
				}
			}
		}

		private static void ReadMainColumns(IList<int> mainColIndices, bool addtlMatrices, string[] words,
			double[,] mainValues, int count, bool[,] isImputedValues, double[,] qualityValues) {
			for (int i = 0; i < mainColIndices.Count; i++) {
				if (mainColIndices[i] >= words.Length) {
					mainValues[count, i] = double.NaN;
				} else {
					string s = StringUtils.RemoveWhitespace(words[mainColIndices[i]]);
					if (addtlMatrices) {
						ParseExp(s, out mainValues[count, i], out isImputedValues[count, i], out qualityValues[count, i]);
					} else {
						if (count < mainValues.GetLength(0)) {
							bool success = Parser.TryDouble(s, out mainValues[count, i]);
							if (!success) {
								mainValues[count, i] = double.NaN;
							}
						}
					}
				}
			}
		}

		private static bool SkipCommentOrInvalid(char separator, List<Tuple<Relation[], int[], bool>> filters,
			bool addtlMatrices, string line, out string[] w) {
			w = new string[0];
			if (TabSep.IsCommentLine(line, commentPrefix, commentPrefixExceptions)) {
				return true;
			}
			if (!IsValidLine(line, separator, filters, out w, addtlMatrices)) {
				return true;
			}
			return false;
		}

		private static double[,] InitializeMainValues(IList<int> mainColIndices, int nrows, bool addtlMatrices,
			out double[,] qualityValues, out bool[,] isImputedValues) {
			double[,] mainValues = new double[nrows, mainColIndices.Count];
			qualityValues = null;
			isImputedValues = null;
			if (addtlMatrices) {
				qualityValues = new double[nrows, mainColIndices.Count];
				isImputedValues = new bool[nrows, mainColIndices.Count];
			}
			return mainValues;
		}

		private static void InitializeAnnotationColumns(IList<int> catColIndices, IList<int> numColIndices,
			IList<int> textColIndices, IList<int> multiNumColIndices, int nrows, out List<string[][]> categoryAnnotation,
			out List<double[]> numericAnnotation, out List<double[][]> multiNumericAnnotation,
			out List<string[]> stringAnnotation) {
			categoryAnnotation = new List<string[][]>();
			for (int i = 0; i < catColIndices.Count; i++) {
				categoryAnnotation.Add(new string[nrows][]);
			}
			numericAnnotation = new List<double[]>();
			for (int i = 0; i < numColIndices.Count; i++) {
				numericAnnotation.Add(new double[nrows]);
			}
			multiNumericAnnotation = new List<double[][]>();
			for (int i = 0; i < multiNumColIndices.Count; i++) {
				multiNumericAnnotation.Add(new double[nrows][]);
			}
			stringAnnotation = new List<string[]>();
			for (int i = 0; i < textColIndices.Count; i++) {
				stringAnnotation.Add(new string[nrows]);
			}
		}

		private static void ParseExp(string s, out double expressionValue, out bool isImputedValue, out double qualityValue) {
			string[] w = s.Split(';');
			expressionValue = double.NaN;
			isImputedValue = false;
			qualityValue = double.NaN;
			if (w.Length > 0) {
				bool success = Parser.TryDouble(w[0], out expressionValue);
				if (!success) {
					expressionValue = double.NaN;
				}
			}
			if (w.Length > 1) {
				bool success = bool.TryParse(w[1], out isImputedValue);
				if (!success) {
					isImputedValue = false;
				}
			}
			if (w.Length > 2) {
				bool success = Parser.TryDouble(w[2], out qualityValue);
				if (!success) {
					qualityValue = double.NaN;
				}
			}
		}

		private static bool GetHasAddtlMatrices(TextReader reader, IList<int> expressionColIndices, char separator) {
			if (expressionColIndices.Count == 0 || reader == null) {
				return false;
			}
			int expressionColIndex = expressionColIndices[0];
			reader.ReadLine();
			string line;
			bool hasAddtl = false;
			while ((line = reader.ReadLine()) != null) {
				if (TabSep.IsCommentLine(line, commentPrefix, commentPrefixExceptions)) {
					continue;
				}
				string[] w = SplitLine(line, separator);
				if (expressionColIndex < w.Length) {
					string s = StringUtils.RemoveWhitespace(w[expressionColIndex]);
					hasAddtl = s.Contains(";");
					break;
				}
			}
			reader.Close();
			return hasAddtl;
		}

		private static string RemoveSplitWhitespace(string s) {
			if (!s.Contains(";")) {
				return s.Trim();
			}
			string[] q = s.Split(';');
			for (int i = 0; i < q.Length; i++) {
				q[i] = q[i].Trim();
			}
			return StringUtils.Concat(";", q);
		}

		private static void SplitAnnotRows(IDictionary<string, string[]> annotRows,
			out Dictionary<string, string[]> catAnnotRows, out Dictionary<string, string[]> numAnnotRows) {
			catAnnotRows = new Dictionary<string, string[]>();
			numAnnotRows = new Dictionary<string, string[]>();
			foreach (string name in annotRows.Keys) {
				if (name.StartsWith("N:")) {
					numAnnotRows.Add(name.Substring(2), annotRows[name]);
				} else if (name.StartsWith("C:")) {
					catAnnotRows.Add(name.Substring(2), annotRows[name]);
				}
			}
		}

		private static string RemoveQuotes(string name) {
			if (name.Length > 2 && name.StartsWith("\"") && name.EndsWith("\"")) {
				return name.Substring(1, name.Length - 2);
			}
			return name;
		}

		private static List<string> RemoveQuotes(IEnumerable<string> names) {
			List<string> result = new List<string>();
			foreach (string name in names) {
				if (name.Length > 2 && name.StartsWith("\"") && name.EndsWith("\"")) {
					result.Add(name.Substring(1, name.Length - 2));
				} else {
					result.Add(name);
				}
			}
			return result;
		}

		private static string[] SplitLine(string line, char separator) {
			line = line.Trim(' ');
			bool inQuote = false;
			List<int> sepInds = new List<int>();
			for (int i = 0; i < line.Length; i++) {
				char c = line[i];
				if (c == '\"') {
					if (inQuote) {
						if (i == line.Length - 1 || line[i + 1] == separator) {
							inQuote = false;
						}
					} else {
						if (i == 0 || line[i - 1] == separator) {
							inQuote = true;
						}
					}
				} else if (c == separator && !inQuote) {
					sepInds.Add(i);
				}
			}
			string[] w = StringUtils.SplitAtIndices(line, sepInds);
			for (int i = 0; i < w.Length; i++) {
				string s = w[i].Trim();
				if (s.Length > 1) {
					if (s[0] == '\"' && s[s.Length - 1] == '\"') {
						s = s.Substring(1, s.Length - 2);
					}
				}
				w[i] = s;
			}
			return w;
		}

		/// <summary>
		/// Search the annotation folder for annotations.
		/// </summary>
		/// <param name="baseNames">The name of the base identifier from which the mapping will be performed. For example Uniprot, ENSG</param>
		/// <param name="files"></param>
		/// <returns>A list of annotations for each file. For example <code>{{"Chromosome", "Orientation"},{"KEGG name", "Pfam"}}</code></returns>
		public static string[][] GetAvailableAnnots(out string[] baseNames, out string[] files) {
			return GetAvailableAnnots(out baseNames, out AnnotType[][] _, out files, out List<string> _);
		}

		/// <summary>
		/// Search the annotation folder for annotations.
		/// </summary>
		/// <param name="baseNames">The name of the base identifier from which the mapping will be performed. For example Uniprot, ENSG</param>
		/// <param name="files"></param>
		/// <param name="badFiles">List of files which could not be processed</param>
		/// <returns>A list of annotations for each file. For example <code>{{"Chromosome", "Orientation"},{"KEGG name", "Pfam"}}</code></returns>
		public static string[][] GetAvailableAnnots(out string[] baseNames, out string[] files, out List<string> badFiles) {
			return GetAvailableAnnots(out baseNames, out AnnotType[][] _, out files, out badFiles);
		}

		/// <summary>
		/// Search the annotation folder for annotations.
		/// </summary>
		/// <param name="baseNames">The name of the base identifier from which the mapping will be performed. For example Uniprot, ENSG</param>
		/// <param name="types"><see cref="AnnotType"/></param>
		/// <param name="files"></param>
		/// <returns>A list of annotations for each file. For example <code>{{"Chromosome", "Orientation"},{"KEGG name", "Pfam"}}</code></returns>
		public static string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files) {
			return GetAvailableAnnots(out baseNames, out types, out files, out List<string> _);
		}

		/// <summary>
		/// Search the annotation folder for annotations.
		/// </summary>
		/// <param name="baseNames">The name of the base identifier from which the mapping will be performed. For example Uniprot, ENSG</param>
		/// <param name="types"><see cref="AnnotType"/></param>
		/// <param name="files"></param>
		/// <param name="badFiles">List of files which could not be processed</param>
		/// <returns>A list of annotations for each file. For example <code>{{"Chromosome", "Orientation"},{"KEGG name", "Pfam"}}</code></returns>
		public static string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files,
			out List<string> badFiles) {
			List<string> filesList = GetAnnotFiles().ToList();
			badFiles = new List<string>();
			List<string> baseNamesList = new List<string>();
			List<AnnotType[]> typesList = new List<AnnotType[]>();
			List<string[]> annotationNames = new List<string[]>();
			foreach (string file in filesList) {
				try {
					string[] name = GetAvailableAnnots(file, out string baseName, out AnnotType[] type);
					annotationNames.Add(name);
					baseNamesList.Add(baseName);
					typesList.Add(type);
				} catch (Exception) {
					badFiles.Add(file);
				}
			}
			foreach (string badFile in badFiles) {
				filesList.Remove(badFile);
			}
			files = filesList.ToArray();
			baseNames = baseNamesList.ToArray();
			types = typesList.ToArray();
			return annotationNames.ToArray();
		}

		private static string[] GetAvailableAnnots(string file, out string baseName, out AnnotType[] types) {
			StreamReader reader = FileUtils.GetReader(file);
			string line = reader.ReadLine();
			string[] header = line.Split('\t');
			line = reader.ReadLine();
			string[] desc = line.Split('\t');
			reader.Close();
			baseName = header[0];
			string[] result = ArrayUtils.SubArray(header, 1, header.Length);
			types = new AnnotType[desc.Length - 1];
			for (int i = 0; i < types.Length; i++) {
				types[i] = FromString1(desc[i + 1]);
			}
			return result;
		}

		private static AnnotType FromString1(string s) {
			switch (s) {
				case "Text": return AnnotType.Text;
				case "Categorical": return AnnotType.Categorical;
				case "Numerical": return AnnotType.Numerical;
				default: return AnnotType.Categorical;
			}
		}

		public static string[] GetAnnotFiles() {
			string folder = Path.Combine(FileUtils.executablePath, "conf", "annotations");
			if (!Directory.Exists(folder)) {
				return new string[0];
			}
			string[] files = Directory.GetFiles(folder);
			List<string> result = new List<string>();
			foreach (string file in files) {
				string fileLow = file.ToLower();
				if (fileLow.EndsWith(".txt.gz") || fileLow.EndsWith(".txt")) {
					result.Add(file);
				}
			}
			return result.ToArray();
		}

        /// <summary>
        /// Create numeric filter parameters for each selection, consisting of a column selection, a relations and a combination parameter.
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
		public static Parameter[] GetNumFilterParams(string[] selection) {
			return new[] {
				GetColumnSelectionParameter(selection), GetRelationsParameter(),
				new SingleChoiceParam("Combine through", 0) {Values = new[] {"intersection", "union"}}
			};
		}

		private static Parameter GetColumnSelectionParameter(string[] selection) {
			const int maxCols = 5;
			string[] values = new string[maxCols];
			Parameters[] subParams = new Parameters[maxCols];
			for (int i = 1; i <= maxCols; i++) {
				values[i - 1] = "" + i;
				Parameter[] px = new Parameter[i];
				for (int j = 0; j < i; j++) {
					px[j] = new SingleChoiceParam(GetVariableName(j), j) {Values = selection};
				}
				Parameters p = new Parameters(px);
				subParams[i - 1] = p;
			}
			return new SingleChoiceWithSubParams("Number of columns", 0) {
				Values = values,
				SubParams = subParams,
				ParamNameWidth = 120,
				TotalWidth = 800
			};
		}

		private static string GetVariableName(int i) {
			const string x = "xyzabc";
			return "" + x[i];
		}

		private static Parameter GetRelationsParameter() {
			const int maxCols = 5;
			string[] values = new string[maxCols];
			Parameters[] subParams = new Parameters[maxCols];
			for (int i = 1; i <= maxCols; i++) {
				values[i - 1] = "" + i;
				Parameter[] px = new Parameter[i];
				for (int j = 0; j < i; j++) {
					px[j] = new StringParam("Relation " + (j + 1));
				}
				Parameters p = new Parameters(px);
				subParams[i - 1] = p;
			}
			return new SingleChoiceWithSubParams("Number of relations", 0) {
				Values = values,
				SubParams = subParams,
				ParamNameWidth = 120,
				TotalWidth = 800
			};
		}

		public static bool IsValidRowNumFilter(double[] row, Relation[] relations, bool and) {
			Dictionary<int, double> vars = new Dictionary<int, double>();
			for (int j = 0; j < row.Length; j++) {
				vars.Add(j, row[j]);
			}
			bool[] results = new bool[relations.Length];
			for (int j = 0; j < relations.Length; j++) {
				results[j] = relations[j].NumEvaluateDouble(vars);
			}
			return and ? ArrayUtils.And(results) : ArrayUtils.Or(results);
		}

		public static Relation[] GetRelationsNumFilter(Parameters param, out string errString, out int[] colInds,
			out bool and) {
			errString = null;
			if (param == null) {
				colInds = new int[0];
				and = false;
				return null;
			}
			and = param.GetParam<int>("Combine through").Value == 0;
			colInds = GetColIndsNumFilter(param, out string[] realVariableNames);
			if (colInds == null || colInds.Length == 0) {
				errString = "Please specify at least one column.";
				return null;
			}
			Relation[] relations = GetRelations(param, realVariableNames);
			foreach (Relation relation in relations) {
				if (relation == null) {
					errString = "Could not parse relations";
					return null;
				}
			}
			return relations;
		}

		private static Relation[] GetRelations(Parameters parameters, string[] realVariableNames) {
			ParameterWithSubParams<int> sp = parameters.GetParamWithSubParams<int>("Number of relations");
			int nrel = sp.Value + 1;
			List<Relation> result = new List<Relation>();
			Parameters param = sp.GetSubParameters();
			for (int j = 0; j < nrel; j++) {
				string rel = param.GetParam<string>("Relation " + (j + 1)).Value;
				if (rel.StartsWith(">") || rel.StartsWith("<") || rel.StartsWith("=")) {
					rel = "x" + rel;
				}
				Relation r = Relation.CreateFromString(rel, realVariableNames, new string[0], out string _);
				result.Add(r);
			}
			return result.ToArray();
		}

		private static int[] GetColIndsNumFilter(Parameters parameters, out string[] realVariableNames) {
			ParameterWithSubParams<int> sp = parameters.GetParamWithSubParams<int>("Number of columns");
			int ncols = sp.Value + 1;
			int[] result = new int[ncols];
			realVariableNames = new string[ncols];
			Parameters param = sp.GetSubParameters();
			for (int j = 0; j < ncols; j++) {
				realVariableNames[j] = GetVariableName(j);
				result[j] = param.GetParam<int>(realVariableNames[j]).Value;
			}
			return result;
		}

		private static bool IsValidLine(string line, char separator, List<Tuple<Relation[], int[], bool>> filters,
			out string[] split, bool hasAddtlMatrices) {
			if (filters == null || filters.Count == 0) {
				split = SplitLine(line, separator);
				return true;
			}
			split = SplitLine(line, separator);
			foreach (Tuple<Relation[], int[], bool> filter in filters) {
				if (!IsValidRowNumFilter(ToDoubles(ArrayUtils.SubArray(split, filter.Item2), hasAddtlMatrices), filter.Item1,
					filter.Item3)) {
					return false;
				}
			}
			return true;
		}

		private static bool IsValidLine(string line, char separator, List<Tuple<Relation[], int[], bool>> filters,
			bool hasAddtlMatrices) {
			if (filters == null || filters.Count == 0) {
				return true;
			}
			string[] w = SplitLine(line, separator);
			foreach (Tuple<Relation[], int[], bool> filter in filters) {
				if (!IsValidRowNumFilter(ToDoubles(ArrayUtils.SubArray(w, filter.Item2), hasAddtlMatrices), filter.Item1,
					filter.Item3)) {
					return false;
				}
			}
			return true;
		}

		private static double[] ToDoubles(string[] s1, bool hasAddtlMatrices) {
			double[] result = new double[s1.Length];
			for (int i = 0; i < s1.Length; i++) {
				string s = StringUtils.RemoveWhitespace(s1[i]);
				if (hasAddtlMatrices) {
					ParseExp(s, out double f, out bool _, out double _);
					result[i] = f;
				} else {
					bool success = Parser.TryDouble(s, out result[i]);
					if (!success) {
						result[i] = double.NaN;
					}
				}
			}
			return result;
		}

		public static int GetRowCount(StreamReader reader, StreamReader auxReader, int[] mainColIndices,
			List<Tuple<Relation[], int[], bool>> filters, char separator) {
			return GetRowCount(reader, filters, separator,
				auxReader != null && GetHasAddtlMatrices(auxReader, mainColIndices, separator));
		}

		public static int GetRowCount(StreamReader reader, List<Tuple<Relation[], int[], bool>> filters, char separator,
			bool addtlMatrices) {
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			reader.ReadLine();
			int count = 0;
			string line;
			while ((line = reader.ReadLine()) != null) {
                if (TabSep.IsCommentLine(line, commentPrefix, commentPrefixExceptions))
                {
                    continue;
				}
				if (IsValidLine(line, separator, filters, addtlMatrices)) {
					count++;
				}
			}
			return count;
		}

		public static void AddFilter(List<Tuple<Relation[], int[], bool>> filters, Parameters p, int[] inds,
			out string errString) {
			Relation[] relations = GetRelationsNumFilter(p, out errString, out int[] colInds, out bool and);
			if (errString != null) {
				return;
			}
			colInds = ArrayUtils.SubArray(inds, colInds);
			if (relations != null) {
				filters.Add(new Tuple<Relation[], int[], bool>(relations, colInds, and));
			}
		}

		/// <summary>
		/// Write data table to file with tab separation.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="filename"></param>
		public static void WriteDataWithAnnotationColumns(IDataWithAnnotationColumns data, string filename) {
			using (StreamWriter writer = new StreamWriter(filename)) {
				WriteDataWithAnnotationColumns(data, writer);
			}
		}

		/// <summary>
		/// Write data table to stream with tab separation.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="writer"></param>
		public static void WriteDataWithAnnotationColumns(IDataWithAnnotationColumns data, StreamWriter writer) {
			IEnumerable<string> columnNames = ColumnNames(data);
			writer.WriteLine(StringUtils.Concat("\t", columnNames));
			if (HasAnyDescription(data)) {
				IEnumerable<string> columnDescriptions = ColumnDescriptions(data);
				writer.WriteLine("#!{Description}" + StringUtils.Concat("\t", columnDescriptions));
			}
			IEnumerable<string> columnTypes = ColumnTypes(data);
			writer.WriteLine("#!{Type}" + StringUtils.Concat("\t", columnTypes));
			IEnumerable<string> dataRows = DataAnnotationRows(data);
			foreach (string row in dataRows) {
				writer.WriteLine(row);
			}
		}

		/// <summary>
		/// Write matrix to stream with tab separation.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="writer"></param>
		/// <param name="addtlMatrices"></param>
		public static void WriteMatrix(IMatrixData data, StreamWriter writer, bool addtlMatrices = false) {
			IEnumerable<string> columnNames = ColumnNames(data);
			writer.WriteLine(StringUtils.Concat("\t", columnNames));
			if (HasAnyDescription(data)) {
				IEnumerable<string> columnDescriptions = ColumnDescriptions(data);
				writer.WriteLine("#!{Description}" + StringUtils.Concat("\t", columnDescriptions));
			}
			IEnumerable<string> columnTypes = ColumnTypes(data);
			writer.WriteLine("#!{Type}" + StringUtils.Concat("\t", columnTypes));
			IEnumerable<string> numAnnotRows = NumericalAnnotationRows(data);
			foreach (string row in numAnnotRows) {
				writer.WriteLine(row);
			}
			IEnumerable<string> catAnnotRows = CategoricalAnnotationRows(data);
			foreach (string row in catAnnotRows) {
				writer.WriteLine(row);
			}
			WriteDataRows(data, addtlMatrices, writer);
		}

		/// <summary>
		/// Write matrix to file with tab separation
		/// </summary>
		/// <param name="data"></param>
		/// <param name="filename"></param>
		/// <param name="addtlMatrices">if true numbers are converted to triples <code>value;imputed;quality</code></param>
		public static void WriteMatrixToFile(IMatrixData data, string filename, bool addtlMatrices = false) {
			using (StreamWriter writer = new StreamWriter(filename)) {
				WriteMatrix(data, writer, addtlMatrices);
			}
		}

		private static void WriteDataRows(IMatrixData data, bool addtlMatrices, TextWriter writer) {
			for (int j = 0; j < data.RowCount; j++) {
				List<string> words = new List<string>();
				for (int i = 0; i < data.ColumnCount; i++) {
					string s1 = Parser.ToString(data.Values.Get(j, i));
					if (addtlMatrices) {
						s1 += ";" + data.IsImputed[j, i] + ";" + Parser.ToString(data.Quality.Get(j, i));
					}
					words.Add(s1);
				}
				IEnumerable<string> row = words.Concat(DataAnnotationRow(data, j));
				writer.WriteLine(StringUtils.Concat("\t", row));
			}
		}

		private static IEnumerable<string> DataAnnotationRows(IDataWithAnnotationColumns data) {
			for (int i = 0; i < data.RowCount; i++) {
				yield return StringUtils.Concat("\t", DataAnnotationRow(data, i));
			}
		}

		private static IEnumerable<string> DataAnnotationRow(IDataWithAnnotationColumns data, int j) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.CategoryColumnCount; i++) {
				string[] q = data.GetCategoryColumnEntryAt(i, j) ?? new string[0];
				words.Add(q.Length > 0 ? StringUtils.Concat(";", q) : "");
			}
			for (int i = 0; i < data.NumericColumnCount; i++) {
				words.Add(data.NumericColumns[i][j].ToString());
			}
			for (int i = 0; i < data.StringColumnCount; i++) {
				words.Add(data.StringColumns[i][j]?? string.Empty);
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++) {
				double[] q = data.MultiNumericColumns[i][j];
				words.Add(q != null && q.Length > 0 ? StringUtils.Concat(";", q) : "");
			}
			return words;
		}

		private static IEnumerable<string> CategoricalAnnotationRows(IDataWithAnnotationRows data) {
			List<string> rows = new List<string>();
			for (int i = 0; i < data.CategoryRowCount; i++) {
				List<string> words = new List<string>();
				for (int j = 0; j < data.ColumnCount; j++) {
					string[] s = data.GetCategoryRowAt(i)[j];
					words.Add(s.Length == 0 ? "" : StringUtils.Concat(";", s));
				}
				IEnumerable<string> row = words.Concat(AnnotationRowPadding((IDataWithAnnotationColumns) data));
				rows.Add("#!{C:" + data.CategoryRowNames[i] + "}" + StringUtils.Concat("\t", row));
			}
			return rows;
		}

		private static IEnumerable<string> NumericalAnnotationRows(IDataWithAnnotationRows data) {
			List<string> rows = new List<string>();
			for (int i = 0; i < data.NumericRowCount; i++) {
				List<string> words = new List<string>();
				for (int j = 0; j < data.ColumnCount; j++) {
					words.Add(Parser.ToString(data.NumericRows[i][j]));
				}
				IEnumerable<string> row = words.Concat(AnnotationRowPadding((IDataWithAnnotationColumns) data));
				rows.Add("#!{N:" + data.NumericRowNames[i] + "}" + StringUtils.Concat("\t", row));
			}
			return rows;
		}

		private static IEnumerable<string> AnnotationRowPadding(IDataWithAnnotationColumns data) {
			List<string> words = new List<string>();
			for (int j = 0; j < data.CategoryColumnCount; j++) {
				words.Add("");
			}
			for (int j = 0; j < data.NumericColumnCount; j++) {
				words.Add("");
			}
			for (int j = 0; j < data.StringColumnCount; j++) {
				words.Add("");
			}
			for (int j = 0; j < data.MultiNumericColumnCount; j++) {
				words.Add("");
			}
			return words;
		}

		private static IEnumerable<string> ColumnTypes(IMatrixData data) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.ColumnCount; i++) {
				words.Add("E");
			}
			return words.Concat(ColumnTypes((IDataWithAnnotationColumns) data));
		}

		private static IEnumerable<string> ColumnTypes(IDataWithAnnotationColumns data) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.CategoryColumnCount; i++) {
				words.Add("C");
			}
			for (int i = 0; i < data.NumericColumnCount; i++) {
				words.Add("N");
			}
			for (int i = 0; i < data.StringColumnCount; i++) {
				words.Add("T");
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++) {
				words.Add("M");
			}
			return words;
		}

		private static IEnumerable<string> ColumnDescriptions(IMatrixData data) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.ColumnCount; i++) {
				words.Add(data.ColumnDescriptions[i] ?? "");
			}
			return words.Concat(ColumnDescriptions((IDataWithAnnotationColumns) data));
		}

		private static IEnumerable<string> ColumnDescriptions(IDataWithAnnotationColumns data) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.CategoryColumnCount; i++) {
				words.Add(data.CategoryColumnDescriptions[i] ?? "");
			}
			for (int i = 0; i < data.NumericColumnCount; i++) {
				words.Add(data.NumericColumnDescriptions[i] ?? "");
			}
			for (int i = 0; i < data.StringColumnCount; i++) {
				words.Add(data.StringColumnDescriptions[i] ?? "");
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++) {
				words.Add(data.MultiNumericColumnDescriptions[i] ?? "");
			}
			return words;
		}

		private static IEnumerable<string> ColumnNames(IMatrixData data) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.ColumnCount; i++) {
				words.Add(data.ColumnNames[i]);
			}
			return words.Concat(ColumnNames((IDataWithAnnotationColumns) data));
		}

		private static IEnumerable<string> ColumnNames(IDataWithAnnotationColumns data) {
			List<string> words = new List<string>();
			for (int i = 0; i < data.CategoryColumnCount; i++) {
				words.Add(data.CategoryColumnNames[i]);
			}
			for (int i = 0; i < data.NumericColumnCount; i++) {
				words.Add(data.NumericColumnNames[i]);
			}
			for (int i = 0; i < data.StringColumnCount; i++) {
				words.Add(data.StringColumnNames[i]);
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++) {
				words.Add(data.MultiNumericColumnNames[i]);
			}
			return words;
		}

		/// <summary>
		/// True if any column description is set.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool HasAnyDescription(IMatrixData data) {
			for (int i = 0; i < data.ColumnCount; i++) {
				if (data.ColumnDescriptions[i] != null && data.ColumnDescriptions[i].Length > 0) {
					return true;
				}
			}
			return HasAnyDescription((IDataWithAnnotationColumns) data);
		}

		private static bool HasAnyDescription(IDataWithAnnotationColumns data) {
			for (int i = 0; i < data.CategoryColumnCount; i++) {
				if (data.CategoryColumnDescriptions[i] != null && data.CategoryColumnDescriptions[i].Length > 0) {
					return true;
				}
			}
			for (int i = 0; i < data.NumericColumnCount; i++) {
				if (data.NumericColumnDescriptions[i] != null && data.NumericColumnDescriptions[i].Length > 0) {
					return true;
				}
			}
			for (int i = 0; i < data.StringColumnCount; i++) {
				if (data.StringColumnDescriptions[i] != null && data.StringColumnDescriptions[i].Length > 0) {
					return true;
				}
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++) {
				if (data.MultiNumericColumnDescriptions[i] != null && data.MultiNumericColumnDescriptions[i].Length > 0) {
					return true;
				}
			}
			return false;
		}

		public static void ReadDataWithAnnotationColumns(IDataWithAnnotationColumns data, ProcessInfo processInfo,
			Func<StreamReader> getReader, string name, char separator) {
			ReadMatrixInformation(getReader, separator, out Dictionary<string, string[]> annotationRows, out string[] colNames,
				out int[] eInds, out int[] nInds, out int[] cInds, out int[] tInds, out int[] mInds,
				out List<Tuple<Relation[], int[], bool>> filters, out int nrows, out bool hasAdditionalMatrices);
			using (StreamReader reader = getReader()) {
				LoadDataWithAnnotationColumns(data, colNames, cInds, nInds, tInds, mInds, processInfo.Progress, separator, reader,
					nrows, filters);
			}
		}

		public static void ReadMatrix(IMatrixData mdata, ProcessInfo processInfo, Func<StreamReader> getReader, string name,
			char separator) {
			ReadMatrixInformation(getReader, separator, out Dictionary<string, string[]> annotationRows, out string[] colNames,
				out int[] eInds, out int[] nInds, out int[] cInds, out int[] tInds, out int[] mInds,
				out List<Tuple<Relation[], int[], bool>> filters, out int nrows, out bool hasAdditionalMatrices);
			using (StreamReader reader = getReader()) {
				LoadMatrixData(annotationRows, eInds, cInds, nInds, tInds, mInds, processInfo, colNames, mdata, reader, nrows, name,
					separator, false, hasAdditionalMatrices, filters);
			}
		}

		private static void ReadMatrixInformation(Func<StreamReader> getReader, char separator,
			out Dictionary<string, string[]> annotationRows, out string[] colNames, out int[] eInds, out int[] nInds,
			out int[] cInds, out int[] tInds, out int[] mInds, out List<Tuple<Relation[], int[], bool>> filters, out int nrows,
			out bool hasAdditionalMatrices) {
			annotationRows = new Dictionary<string, string[]>();
			using (StreamReader reader = getReader()) {
				colNames = TabSep.GetColumnNames(reader, 0, commentPrefix, commentPrefixExceptions, annotationRows, separator);
			}
			string[] typeRow = annotationRows["Type"];
			ColumnIndices(typeRow, out eInds, out nInds, out cInds, out tInds, out mInds);
			using (StreamReader reader = getReader()) {
				hasAdditionalMatrices = GetHasAddtlMatrices(reader, eInds, separator);
			}
			filters = new List<Tuple<Relation[], int[], bool>>();
			using (StreamReader reader = getReader()) {
				nrows = GetRowCount(reader, filters, separator, hasAdditionalMatrices);
			}
		}

		public static void ReadMatrixFromFile(IMatrixData mdata, ProcessInfo processInfo, string filename, char separator) {
			ReadMatrix(mdata, processInfo, () => FileUtils.GetReader(filename), filename, separator);
		}

		public static void ReadDataWithAnnotationColumnsFromFile(IDataWithAnnotationColumns mdata, ProcessInfo processInfo,
			string filename, char separator) {
			ReadDataWithAnnotationColumns(mdata, processInfo, () => FileUtils.GetReader(filename), filename, separator);
		}

		private static void ColumnIndices(string[] typeRow, out int[] eInds, out int[] nInds, out int[] cInds,
			out int[] tInds, out int[] mInds) {
			List<int> _eInds = new List<int>();
			List<int> _nInds = new List<int>();
			List<int> _cInds = new List<int>();
			List<int> _tInds = new List<int>();
			List<int> _mInds = new List<int>();
			for (int i = 0; i < typeRow.Length; i++) {
				switch (typeRow[i]) {
					case "E":
						_eInds.Add(i);
						break;
					case "N":
						_nInds.Add(i);
						break;
					case "C":
						_cInds.Add(i);
						break;
					case "T":
						_tInds.Add(i);
						break;
					case "M":
						_mInds.Add(i);
						break;
				}
			}
			eInds = _eInds.ToArray();
			nInds = _nInds.ToArray();
			cInds = _cInds.ToArray();
			tInds = _tInds.ToArray();
			mInds = _mInds.ToArray();
		}

		public static void ReadMatrixFromFile(IMatrixData mdata, ProcessInfo processInfo, string filename, int[] eInds,
			int[] nInds, int[] cInds, int[] tInds, int[] mInds, Parameters[] mainFilterParameters,
			Parameters[] numericalFilterParameters, bool shortenExpressionColumnNames) {
			if (!File.Exists(filename)) {
				processInfo.ErrString = "File '" + filename + "' does not exist.";
				return;
			}
			string ftl = filename.ToLower();
			bool csv = ftl.EndsWith(".csv") || ftl.EndsWith(".csv.gz");
			char separator = csv ? ',' : '\t';
			string[] colNames;
			Dictionary<string, string[]> annotationRows = new Dictionary<string, string[]>();
			try {
				colNames = TabSep.GetColumnNames(filename, commentPrefix, commentPrefixExceptions, annotationRows, separator);
			} catch (Exception) {
				processInfo.ErrString = "Could not open the file '" + filename + "'. It is probably opened in another program.";
				return;
			}
			string origin = filename;
			List<Tuple<Relation[], int[], bool>> filters = new List<Tuple<Relation[], int[], bool>>();
			string errString;
			foreach (Parameters p in mainFilterParameters) {
				AddFilter(filters, p, eInds, out errString);
				if (errString != null) {
					processInfo.ErrString = errString;
					return;
				}
			}
			foreach (Parameters p in numericalFilterParameters) {
				AddFilter(filters, p, nInds, out errString);
				if (errString != null) {
					processInfo.ErrString = errString;
					return;
				}
			}
			int nrows;
			bool hasAdditionalMatrices;
			using (StreamReader reader = FileUtils.GetReader(filename)) {
				hasAdditionalMatrices = GetHasAddtlMatrices(reader, eInds, separator);
			}
			using (StreamReader reader = FileUtils.GetReader(filename)) {
				nrows = GetRowCount(reader, filters, separator, hasAdditionalMatrices);
			}
			using (StreamReader reader = FileUtils.GetReader(filename)) {
				LoadMatrixData(annotationRows, eInds, cInds, nInds, tInds, mInds, processInfo, colNames, mdata, reader, nrows,
					origin, separator, shortenExpressionColumnNames, hasAdditionalMatrices, filters);
			}
			GC.Collect();
		}
	}
}