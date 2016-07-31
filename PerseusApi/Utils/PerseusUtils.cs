using System;
using System.Collections.Generic;
using System.IO;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using Calc;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Utils{
	public static class PerseusUtils{
		public static readonly HashSet<string> commentPrefix = new HashSet<string>(new[]{"#", "!"});
		public static readonly HashSet<string> commentPrefixExceptions = new HashSet<string>(new[]{"#N/A", "#n/a"});

		public static void LoadMatrixData(IDictionary<string, string[]> annotationRows, int[] eInds, int[] cInds, int[] nInds,
			int[] tInds, int[] mInds, ProcessInfo processInfo, IList<string> colNames, IMatrixData mdata, StreamReader reader,
			StreamReader auxReader, int nrows, string origin, char separator, bool shortenExpressionNames,
			List<Tuple<Relation[], int[], bool>> filters){
			string[] colDescriptions = null;
			if (annotationRows.ContainsKey("Description")){
				colDescriptions = annotationRows["Description"];
				annotationRows.Remove("Description");
			}
			int[] allInds = ArrayUtils.Concat(new[]{eInds, cInds, nInds, tInds, mInds});
			Array.Sort(allInds);
			for (int i = 0; i < allInds.Length - 1; i++){
				if (allInds[i + 1] == allInds[i]){
					processInfo.ErrString = "Column '" + colNames[allInds[i]] + "' has been selected multiple times";
					return;
				}
			}
			string[] allColNames = ArrayUtils.SubArray(colNames, allInds);
			Array.Sort(allColNames);
			for (int i = 0; i < allColNames.Length - 1; i++){
				if (allColNames[i + 1].Equals(allColNames[i])){
					processInfo.ErrString = "Column name '" + allColNames[i] + "' occurs multiple times.";
					return;
				}
			}
			LoadMatrixData(colNames, colDescriptions, eInds, cInds, nInds, tInds, mInds, origin, mdata, annotationRows,
				processInfo.Progress, processInfo.Status, separator, reader, auxReader, nrows, shortenExpressionNames, filters);
		}

		private static void LoadMatrixData(IList<string> colNames, IList<string> colDescriptions, IList<int> mainColIndices,
			IList<int> catColIndices, IList<int> numColIndices, IList<int> textColIndices, IList<int> multiNumColIndices,
			string origin, IMatrixData matrixData, IDictionary<string, string[]> annotationRows, Action<int> progress,
			Action<string> status, char separator, TextReader reader, StreamReader auxReader, int nrows,
			bool shortenExpressionNames, List<Tuple<Relation[], int[], bool>> filters){
			Dictionary<string, string[]> catAnnotatRows;
			Dictionary<string, string[]> numAnnotatRows;
			status("Reading data");
			SplitAnnotRows(annotationRows, out catAnnotatRows, out numAnnotatRows);
			List<string[][]> categoryAnnotation = new List<string[][]>();
			for (int i = 0; i < catColIndices.Count; i++){
				categoryAnnotation.Add(new string[nrows][]);
			}
			List<double[]> numericAnnotation = new List<double[]>();
			for (int i = 0; i < numColIndices.Count; i++){
				numericAnnotation.Add(new double[nrows]);
			}
			List<double[][]> multiNumericAnnotation = new List<double[][]>();
			for (int i = 0; i < multiNumColIndices.Count; i++){
				multiNumericAnnotation.Add(new double[nrows][]);
			}
			List<string[]> stringAnnotation = new List<string[]>();
			for (int i = 0; i < textColIndices.Count; i++){
				stringAnnotation.Add(new string[nrows]);
			}
			float[,] mainValues = new float[nrows, mainColIndices.Count];
			float[,] qualityValues = null;
			bool[,] isImputedValues = null;
			bool hasAddtlMatrices = auxReader != null && GetHasAddtlMatrices(auxReader, mainColIndices, separator);
			if (hasAddtlMatrices){
				qualityValues = new float[nrows, mainColIndices.Count];
				isImputedValues = new bool[nrows, mainColIndices.Count];
			}
			reader.ReadLine();
			int count = 0;
			string line;
			while ((line = reader.ReadLine()) != null){
				progress(100*(count + 1)/nrows);
				if (TabSep.IsCommentLine(line, commentPrefix, commentPrefixExceptions)){
					continue;
				}
				string[] w;
				if (!IsValidLine(line, separator, filters, out w, hasAddtlMatrices)){
					continue;
				}
				for (int i = 0; i < mainColIndices.Count; i++){
					if (mainColIndices[i] >= w.Length){
						mainValues[count, i] = float.NaN;
					} else{
						string s = StringUtils.RemoveWhitespace(w[mainColIndices[i]]);
						if (hasAddtlMatrices){
							ParseExp(s, out mainValues[count, i], out isImputedValues[count, i], out qualityValues[count, i]);
						} else{
							if (count < mainValues.GetLength(0)){
								bool success = float.TryParse(s, out mainValues[count, i]);
								if (!success){
									mainValues[count, i] = float.NaN;
								}
							}
						}
					}
				}
				for (int i = 0; i < numColIndices.Count; i++){
					if (numColIndices[i] >= w.Length){
						numericAnnotation[i][count] = double.NaN;
					} else{
						double q;
						bool success = double.TryParse(w[numColIndices[i]].Trim(), out q);
						if (numericAnnotation[i].Length > count){
							numericAnnotation[i][count] = success ? q : double.NaN;
						}
					}
				}
				for (int i = 0; i < multiNumColIndices.Count; i++){
					if (multiNumColIndices[i] >= w.Length){
						multiNumericAnnotation[i][count] = new double[0];
					} else{
						string q = w[multiNumColIndices[i]].Trim();
						if (q.Length >= 2 && q[0] == '\"' && q[q.Length - 1] == '\"'){
							q = q.Substring(1, q.Length - 2);
						}
						if (q.Length >= 2 && q[0] == '\'' && q[q.Length - 1] == '\''){
							q = q.Substring(1, q.Length - 2);
						}
						string[] ww = q.Length == 0 ? new string[0] : q.Split(';');
						multiNumericAnnotation[i][count] = new double[ww.Length];
						for (int j = 0; j < ww.Length; j++){
							double q1;
							bool success = double.TryParse(ww[j], out q1);
							multiNumericAnnotation[i][count][j] = success ? q1 : double.NaN;
						}
					}
				}
				for (int i = 0; i < catColIndices.Count; i++){
					if (catColIndices[i] >= w.Length){
						categoryAnnotation[i][count] = new string[0];
					} else{
						string q = w[catColIndices[i]].Trim();
						if (q.Length >= 2 && q[0] == '\"' && q[q.Length - 1] == '\"'){
							q = q.Substring(1, q.Length - 2);
						}
						if (q.Length >= 2 && q[0] == '\'' && q[q.Length - 1] == '\''){
							q = q.Substring(1, q.Length - 2);
						}
						string[] ww = q.Length == 0 ? new string[0] : q.Split(';');
						List<int> valids = new List<int>();
						for (int j = 0; j < ww.Length; j++){
							ww[j] = ww[j].Trim();
							if (ww[j].Length > 0){
								valids.Add(j);
							}
						}
						ww = ArrayUtils.SubArray(ww, valids);
						Array.Sort(ww);
						if (categoryAnnotation[i].Length > count){
							categoryAnnotation[i][count] = ww;
						}
					}
				}
				for (int i = 0; i < textColIndices.Count; i++){
					if (textColIndices[i] >= w.Length){
						stringAnnotation[i][count] = "";
					} else{
						string q = w[textColIndices[i]].Trim();
						if (stringAnnotation[i].Length > count){
							stringAnnotation[i][count] = RemoveSplitWhitespace(RemoveQuotes(q));
						}
					}
				}
				count++;
			}
			reader.Close();
			string[] columnNames = ArrayUtils.SubArray(colNames, mainColIndices);
			if (shortenExpressionNames){
				columnNames = StringUtils.RemoveCommonSubstrings(columnNames, true);
			}
			string[] catColnames = ArrayUtils.SubArray(colNames, catColIndices);
			string[] numColnames = ArrayUtils.SubArray(colNames, numColIndices);
			string[] multiNumColnames = ArrayUtils.SubArray(colNames, multiNumColIndices);
			string[] textColnames = ArrayUtils.SubArray(colNames, textColIndices);
			matrixData.Name = origin;
			matrixData.ColumnNames = RemoveQuotes(columnNames);
			matrixData.Values.Set(mainValues);
			if (hasAddtlMatrices){
				matrixData.Quality.Set(qualityValues);
				matrixData.IsImputed.Set(isImputedValues);
			} else{
				matrixData.Quality.Set(new float[mainValues.GetLength(0), mainValues.GetLength(1)]);
				matrixData.IsImputed.Set(new bool[mainValues.GetLength(0), mainValues.GetLength(1)]);
			}
			matrixData.SetAnnotationColumns(RemoveQuotes(textColnames), stringAnnotation, RemoveQuotes(catColnames),
				categoryAnnotation, RemoveQuotes(numColnames), numericAnnotation, RemoveQuotes(multiNumColnames),
				multiNumericAnnotation);
			if (colDescriptions != null){
				string[] columnDesc = ArrayUtils.SubArray(colDescriptions, mainColIndices);
				string[] catColDesc = ArrayUtils.SubArray(colDescriptions, catColIndices);
				string[] numColDesc = ArrayUtils.SubArray(colDescriptions, numColIndices);
				string[] multiNumColDesc = ArrayUtils.SubArray(colDescriptions, multiNumColIndices);
				string[] textColDesc = ArrayUtils.SubArray(colDescriptions, textColIndices);
				matrixData.ColumnDescriptions = new List<string>(columnDesc);
				matrixData.NumericColumnDescriptions = new List<string>(numColDesc);
				matrixData.CategoryColumnDescriptions = new List<string>(catColDesc);
				matrixData.StringColumnDescriptions = new List<string>(textColDesc);
				matrixData.MultiNumericColumnDescriptions = new List<string>(multiNumColDesc);
			}
			foreach (string key in catAnnotatRows.Keys){
				string name = key;
				string[] svals = ArrayUtils.SubArray(catAnnotatRows[key], mainColIndices);
				string[][] cat = new string[svals.Length][];
				for (int i = 0; i < cat.Length; i++){
					string s = svals[i].Trim();
					cat[i] = s.Length > 0 ? s.Split(';') : new string[0];
					List<int> valids = new List<int>();
					for (int j = 0; j < cat[i].Length; j++){
						cat[i][j] = cat[i][j].Trim();
						if (cat[i][j].Length > 0){
							valids.Add(j);
						}
					}
					cat[i] = ArrayUtils.SubArray(cat[i], valids);
					Array.Sort(cat[i]);
				}
				matrixData.AddCategoryRow(name, name, cat);
			}
			foreach (string key in numAnnotatRows.Keys){
				string name = key;
				string[] svals = ArrayUtils.SubArray(numAnnotatRows[key], mainColIndices);
				double[] num = new double[svals.Length];
				for (int i = 0; i < num.Length; i++){
					string s = svals[i].Trim();
					num[i] = double.NaN;
					double.TryParse(s, out num[i]);
				}
				matrixData.AddNumericRow(name, name, num);
			}
			matrixData.Origin = origin;
			progress(0);
			status("");
		}

		private static void ParseExp(string s, out float expressionValue, out bool isImputedValue, out float qualityValue){
			string[] w = s.Split(';');
			expressionValue = float.NaN;
			isImputedValue = false;
			qualityValue = float.NaN;
			if (w.Length > 0){
				bool success = float.TryParse(w[0], out expressionValue);
				if (!success){
					expressionValue = float.NaN;
				}
			}
			if (w.Length > 1){
				bool success = bool.TryParse(w[1], out isImputedValue);
				if (!success){
					isImputedValue = false;
				}
			}
			if (w.Length > 2){
				bool success = float.TryParse(w[2], out qualityValue);
				if (!success){
					qualityValue = float.NaN;
				}
			}
		}

		private static bool GetHasAddtlMatrices(StreamReader reader, IList<int> expressionColIndices, char separator){
			if (expressionColIndices.Count == 0){
				return false;
			}
			int expressionColIndex = expressionColIndices[0];
			reader.ReadLine();
			string line;
			bool hasAddtl = false;
			while ((line = reader.ReadLine()) != null){
				if (TabSep.IsCommentLine(line, commentPrefix, commentPrefixExceptions)){
					continue;
				}
				string[] w = SplitLine(line, separator);
				if (expressionColIndex < w.Length){
					string s = StringUtils.RemoveWhitespace(w[expressionColIndex]);
					hasAddtl = s.Contains(";");
					break;
				}
			}
			reader.Close();
			return hasAddtl;
		}

		private static string RemoveSplitWhitespace(string s){
			if (!s.Contains(";")){
				return s.Trim();
			}
			string[] q = s.Split(';');
			for (int i = 0; i < q.Length; i++){
				q[i] = q[i].Trim();
			}
			return StringUtils.Concat(";", q);
		}

		private static void SplitAnnotRows(IDictionary<string, string[]> annotRows,
			out Dictionary<string, string[]> catAnnotRows, out Dictionary<string, string[]> numAnnotRows){
			catAnnotRows = new Dictionary<string, string[]>();
			numAnnotRows = new Dictionary<string, string[]>();
			foreach (string name in annotRows.Keys){
				if (name.StartsWith("N:")){
					numAnnotRows.Add(name.Substring(2), annotRows[name]);
				} else if (name.StartsWith("C:")){
					catAnnotRows.Add(name.Substring(2), annotRows[name]);
				}
			}
		}

		private static string RemoveQuotes(string name){
			if (name.Length > 2 && name.StartsWith("\"") && name.EndsWith("\"")){
				return name.Substring(1, name.Length - 2);
			}
			return name;
		}

		private static List<string> RemoveQuotes(IEnumerable<string> names){
			List<string> result = new List<string>();
			foreach (string name in names){
				if (name.Length > 2 && name.StartsWith("\"") && name.EndsWith("\"")){
					result.Add(name.Substring(1, name.Length - 2));
				} else{
					result.Add(name);
				}
			}
			return result;
		}

		private static string[] SplitLine(string line, char separator){
			line = line.Trim(' ');
			bool inQuote = false;
			List<int> sepInds = new List<int>();
			for (int i = 0; i < line.Length; i++){
				char c = line[i];
				if (c == '\"'){
					if (inQuote){
						if (i == line.Length - 1 || line[i + 1] == separator){
							inQuote = false;
						}
					} else{
						if (i == 0 || line[i - 1] == separator){
							inQuote = true;
						}
					}
				} else if (c == separator && !inQuote){
					sepInds.Add(i);
				}
			}
			string[] w = StringUtils.SplitAtIndices(line, sepInds);
			for (int i = 0; i < w.Length; i++){
				string s = w[i].Trim();
				if (s.Length > 1){
					if (s[0] == '\"' && s[s.Length - 1] == '\"'){
						s = s.Substring(1, s.Length - 2);
					}
				}
				w[i] = s;
			}
			return w;
		}

		public static string GetNextAvailableName(string s, ICollection<string> taken){
			if (!taken.Contains(s)){
				return s;
			}
			while (true){
				s = GetNext(s);
				if (!taken.Contains(s)){
					return s;
				}
			}
		}

		private static string GetNext(string s){
			if (!HasNumberExtension(s)){
				return s + "_1";
			}
			int x = s.LastIndexOf('_');
			string s1 = s.Substring(x + 1);
			int num = int.Parse(s1);
			return s.Substring(0, x + 1) + (num + 1);
		}

		private static bool HasNumberExtension(string s){
			int x = s.LastIndexOf('_');
			if (x < 0){
				return false;
			}
			string s1 = s.Substring(x + 1);
			int num;
			bool succ = int.TryParse(s1, out num);
			return succ;
		}

		public static string[][] GetAvailableAnnots(out string[] baseNames, out string[] files){
			AnnotType[][] types;
			return GetAvailableAnnots(out baseNames, out types, out files);
		}

		public static string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files){
			files = GetAnnotFiles();
			baseNames = new string[files.Length];
			types = new AnnotType[files.Length][];
			string[][] names = new string[files.Length][];
			for (int i = 0; i < names.Length; i++){
				names[i] = GetAvailableAnnots(files[i], out baseNames[i], out types[i]);
			}
			return names;
		}

		private static string[] GetAvailableAnnots(string file, out string baseName, out AnnotType[] types){
			StreamReader reader = FileUtils.GetReader(file);
			string line = reader.ReadLine();
			string[] header = line.Split('\t');
			line = reader.ReadLine();
			string[] desc = line.Split('\t');
			reader.Close();
			baseName = header[0];
			string[] result = ArrayUtils.SubArray(header, 1, header.Length);
			types = new AnnotType[desc.Length - 1];
			for (int i = 0; i < types.Length; i++){
				types[i] = FromString1(desc[i + 1]);
			}
			return result;
		}

		private static AnnotType FromString1(string s){
			switch (s){
				case "Text":
					return AnnotType.Text;
				case "Categorical":
					return AnnotType.Categorical;
				case "Numerical":
					return AnnotType.Numerical;
				default:
					return AnnotType.Categorical;
			}
		}

		private static string[] GetAnnotFiles(){
			string folder = FileUtils.executablePath + "\\conf\\annotations";
			string[] files = Directory.GetFiles(folder);
			List<string> result = new List<string>();
			foreach (string file in files){
				string fileLow = file.ToLower();
				if (fileLow.EndsWith(".txt.gz") || fileLow.EndsWith(".txt")){
					result.Add(file);
				}
			}
			return result.ToArray();
		}

		public static Parameter[] GetNumFilterParams(string[] selection){
			return new[]{
				GetColumnSelectionParameter(selection), GetRelationsParameter(),
				new SingleChoiceParam("Combine through", 0){Values = new[]{"intersection", "union"}}
			};
		}

		private static Parameter GetColumnSelectionParameter(string[] selection){
			const int maxCols = 5;
			string[] values = new string[maxCols];
			Parameters[] subParams = new Parameters[maxCols];
			for (int i = 1; i <= maxCols; i++){
				values[i - 1] = "" + i;
				Parameter[] px = new Parameter[i];
				for (int j = 0; j < i; j++){
					px[j] = new SingleChoiceParam(GetVariableName(j), j){Values = selection};
				}
				Parameters p = new Parameters(px);
				subParams[i - 1] = p;
			}
			return new SingleChoiceWithSubParams("Number of columns", 0){
				Values = values,
				SubParams = subParams,
				ParamNameWidth = 120,
				TotalWidth = 800
			};
		}

		private static string GetVariableName(int i){
			const string x = "xyzabc";
			return "" + x[i];
		}

		private static Parameter GetRelationsParameter(){
			const int maxCols = 5;
			string[] values = new string[maxCols];
			Parameters[] subParams = new Parameters[maxCols];
			for (int i = 1; i <= maxCols; i++){
				values[i - 1] = "" + i;
				Parameter[] px = new Parameter[i];
				for (int j = 0; j < i; j++){
					px[j] = new StringParam("Relation " + (j + 1));
				}
				Parameters p = new Parameters(px);
				subParams[i - 1] = p;
			}
			return new SingleChoiceWithSubParams("Number of relations", 0){
				Values = values,
				SubParams = subParams,
				ParamNameWidth = 120,
				TotalWidth = 800
			};
		}

		public static bool IsValidRowNumFilter(double[] row, Relation[] relations, bool and){
			Dictionary<int, double> vars = new Dictionary<int, double>();
			for (int j = 0; j < row.Length; j++){
				vars.Add(j, row[j]);
			}
			bool[] results = new bool[relations.Length];
			for (int j = 0; j < relations.Length; j++){
				results[j] = relations[j].NumEvaluateDouble(vars);
			}
			return and ? ArrayUtils.And(results) : ArrayUtils.Or(results);
		}

		public static Relation[] GetRelationsNumFilter(Parameters param, out string errString, out int[] colInds, out bool and){
			errString = null;
			if (param == null){
				colInds = new int[0];
				and = false;
				return null;
			}
			and = param.GetParam<int>("Combine through").Value == 0;
			string[] realVariableNames;
			colInds = GetColIndsNumFilter(param, out realVariableNames);
			if (colInds == null || colInds.Length == 0){
				errString = "Please specify at least one column.";
				return null;
			}
			Relation[] relations = GetRelations(param, realVariableNames);
			foreach (Relation relation in relations){
				if (relation == null){
					errString = "Could not parse relations";
					return null;
				}
			}
			return relations;
		}

		private static Relation[] GetRelations(Parameters parameters, string[] realVariableNames){
			ParameterWithSubParams<int> sp = parameters.GetParamWithSubParams<int>("Number of relations");
			int nrel = sp.Value + 1;
			List<Relation> result = new List<Relation>();
			Parameters param = sp.GetSubParameters();
			for (int j = 0; j < nrel; j++){
				string rel = param.GetParam<string>("Relation " + (j + 1)).Value;
				if (rel.StartsWith(">") || rel.StartsWith("<") || rel.StartsWith("=")){
					rel = "x" + rel;
				}
				string err1;
				Relation r = Relation.CreateFromString(rel, realVariableNames, new string[0], out err1);
				result.Add(r);
			}
			return result.ToArray();
		}

		private static int[] GetColIndsNumFilter(Parameters parameters, out string[] realVariableNames){
			ParameterWithSubParams<int> sp = parameters.GetParamWithSubParams<int>("Number of columns");
			int ncols = sp.Value + 1;
			int[] result = new int[ncols];
			realVariableNames = new string[ncols];
			Parameters param = sp.GetSubParameters();
			for (int j = 0; j < ncols; j++){
				realVariableNames[j] = GetVariableName(j);
				result[j] = param.GetParam<int>(realVariableNames[j]).Value;
			}
			return result;
		}

		public static int GetRowCount(StreamReader reader, StreamReader auxReader, int[] mainColIndices,
			List<Tuple<Relation[], int[], bool>> filters, char separator){
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			reader.ReadLine();
			int count = 0;
			bool hasAddtlMatrices = auxReader != null && GetHasAddtlMatrices(auxReader, mainColIndices, separator);
			string line;
			while ((line = reader.ReadLine()) != null){
				while (TabSep.IsCommentLine(line, commentPrefix, commentPrefixExceptions)){
					line = reader.ReadLine();
				}
				if (IsValidLine(line, separator, filters, hasAddtlMatrices)){
					count++;
				}
			}
			return count;
		}

		private static bool IsValidLine(string line, char separator, List<Tuple<Relation[], int[], bool>> filters,
			out string[] split, bool hasAddtlMatrices){
			if (filters == null || filters.Count == 0){
				split = SplitLine(line, separator);
				return true;
			}
			split = SplitLine(line, separator);
			foreach (Tuple<Relation[], int[], bool> filter in filters){
				if (
					!IsValidRowNumFilter(ToDoubles(ArrayUtils.SubArray(split, filter.Item2), hasAddtlMatrices), filter.Item1,
						filter.Item3)){
					return false;
				}
			}
			return true;
		}

		private static bool IsValidLine(string line, char separator, List<Tuple<Relation[], int[], bool>> filters,
			bool hasAddtlMatrices){
			if (filters == null || filters.Count == 0){
				return true;
			}
			string[] w = SplitLine(line, separator);
			foreach (Tuple<Relation[], int[], bool> filter in filters){
				if (
					!IsValidRowNumFilter(ToDoubles(ArrayUtils.SubArray(w, filter.Item2), hasAddtlMatrices), filter.Item1, filter.Item3)){
					return false;
				}
			}
			return true;
		}

		private static double[] ToDoubles(string[] s1, bool hasAddtlMatrices){
			double[] result = new double[s1.Length];
			for (int i = 0; i < s1.Length; i++){
				string s = StringUtils.RemoveWhitespace(s1[i]);
				if (hasAddtlMatrices){
					bool isImputed;
					float quality;
					float f;
					ParseExp(s, out f, out isImputed, out quality);
					result[i] = f;
				} else{
					bool success = double.TryParse(s, out result[i]);
					if (!success){
						result[i] = double.NaN;
					}
				}
			}
			return result;
		}

		public static void AddFilter(List<Tuple<Relation[], int[], bool>> filters, Parameters p, int[] inds,
			out string errString){
			int[] colInds;
			bool and;
			Relation[] relations = PerseusUtils.GetRelationsNumFilter(p, out errString, out colInds, out and);
			if (errString != null){
				return;
			}
			colInds = ArrayUtils.SubArray(inds, colInds);
			if (relations != null){
				filters.Add(new Tuple<Relation[], int[], bool>(relations, colInds, and));
			}
		}
	}
}