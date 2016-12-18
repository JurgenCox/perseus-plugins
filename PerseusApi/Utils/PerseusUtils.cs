using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		private static bool GetHasAddtlMatrices(TextReader reader, IList<int> expressionColIndices, char separator){
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

		public static string[][] GetAvailableAnnots(out string[] baseNames, out string[] files){
			AnnotType[][] types;
		    List<string> badFiles;
			return GetAvailableAnnots(out baseNames, out types, out files, out badFiles);
		}
        /// <summary>
        /// Read-in all available annotations.
        /// </summary>
        /// <param name="baseNames"></param>
        /// <param name="files"></param>
        /// <param name="badFiles">all files which couldn't be read</param>
        /// <returns></returns>
		public static string[][] GetAvailableAnnots(out string[] baseNames, out string[] files, out List<string> badFiles ){
			AnnotType[][] types;
			return GetAvailableAnnots(out baseNames, out types, out files, out badFiles);
		}
		public static string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files) {
		    List<string> badFiles;
			return GetAvailableAnnots(out baseNames, out types, out files, out badFiles);
		}

	    /// <summary>
	    /// Read-in all available annotations.
	    /// </summary>
	    /// <param name="baseNames"></param>
	    /// <param name="types"></param>
	    /// <param name="files"></param>
	    /// <param name="badFiles">all files which couldn't be read</param>
	    /// <returns></returns>
	    public static string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files, out List<string> badFiles ){
			var _files = GetAnnotFiles().ToList();
            badFiles = new List<string>();
		    var _baseNames = new List<string>();
		    var _types = new List<AnnotType[]>();
		    var names = new List<string[]>();
		    foreach (var file in _files)
		    {
		        try
		        {
		            string baseName;
		            AnnotType[] type;
		            var name = GetAvailableAnnots(file, out baseName, out type);
                    names.Add(name);
                    _baseNames.Add(baseName);
                    _types.Add(type);
                }
                catch (Exception exception)
                {
                    badFiles.Add(file);
		        }
	        }
		    foreach (var badFile in badFiles)
		    {
		        _files.Remove(badFile);
		    }
		    files = _files.ToArray();
		    baseNames = _baseNames.ToArray();
		    types = _types.ToArray();
			return names.ToArray();
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

		public static void AddFilter(List<Tuple<Relation[], int[], bool>> filters, Parameters p, int[] inds,
			out string errString){
			int[] colInds;
			bool and;
			Relation[] relations = GetRelationsNumFilter(p, out errString, out colInds, out and);
			if (errString != null){
				return;
			}
			colInds = ArrayUtils.SubArray(inds, colInds);
			if (relations != null){
				filters.Add(new Tuple<Relation[], int[], bool>(relations, colInds, and));
			}
		}

	    /// <summary>
	    /// Write matrix to file with tab separation
	    /// </summary>
	    /// <param name="data"></param>
	    /// <param name="filename"></param>
	    /// <param name="addtlMatrices">if true numbers are converted to triples <code>value;imputed;quality</code></param>
	    public static void WriteMatrixToFile(IMatrixData data, string filename, bool addtlMatrices=false)
	    {
	        using (var writer = new StreamWriter(filename))
	        {
	            List<string> words = new List<string>();
	            for (int i = 0; i < data.ColumnCount; i++)
	            {
	                words.Add(data.ColumnNames[i]);
	            }
	            for (int i = 0; i < data.CategoryColumnCount; i++)
	            {
	                words.Add(data.CategoryColumnNames[i]);
	            }
	            for (int i = 0; i < data.NumericColumnCount; i++)
	            {
	                words.Add(data.NumericColumnNames[i]);
	            }
	            for (int i = 0; i < data.StringColumnCount; i++)
	            {
	                words.Add(data.StringColumnNames[i]);
	            }
	            for (int i = 0; i < data.MultiNumericColumnCount; i++)
	            {
	                words.Add(data.MultiNumericColumnNames[i]);
	            }
	            writer.WriteLine(StringUtils.Concat("\t", words));
	            if (HasAnyDescription(data))
	            {
	                words = new List<string>();
	                for (int i = 0; i < data.ColumnCount; i++)
	                {
	                    words.Add(data.ColumnDescriptions[i] ?? "");
	                }
	                for (int i = 0; i < data.CategoryColumnCount; i++)
	                {
	                    words.Add(data.CategoryColumnDescriptions[i] ?? "");
	                }
	                for (int i = 0; i < data.NumericColumnCount; i++)
	                {
	                    words.Add(data.NumericColumnDescriptions[i] ?? "");
	                }
	                for (int i = 0; i < data.StringColumnCount; i++)
	                {
	                    words.Add(data.StringColumnDescriptions[i] ?? "");
	                }
	                for (int i = 0; i < data.MultiNumericColumnCount; i++)
	                {
	                    words.Add(data.MultiNumericColumnDescriptions[i] ?? "");
	                }
	                writer.WriteLine("#!{Description}" + StringUtils.Concat("\t", words));
	            }
	            words = new List<string>();
	            for (int i = 0; i < data.ColumnCount; i++)
	            {
	                words.Add("E");
	            }
	            for (int i = 0; i < data.CategoryColumnCount; i++)
	            {
	                words.Add("C");
	            }
	            for (int i = 0; i < data.NumericColumnCount; i++)
	            {
	                words.Add("N");
	            }
	            for (int i = 0; i < data.StringColumnCount; i++)
	            {
	                words.Add("T");
	            }
	            for (int i = 0; i < data.MultiNumericColumnCount; i++)
	            {
	                words.Add("M");
	            }
	            writer.WriteLine("#!{Type}" + StringUtils.Concat("\t", words));
	            for (int i = 0; i < data.NumericRowCount; i++)
	            {
	                words = new List<string>();
	                for (int j = 0; j < data.ColumnCount; j++)
	                {
	                    words.Add("" + data.NumericRows[i][j]);
	                }
	                for (int j = 0; j < data.CategoryColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                for (int j = 0; j < data.NumericColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                for (int j = 0; j < data.StringColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                for (int j = 0; j < data.MultiNumericColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                writer.WriteLine("#!{N:" + data.NumericRowNames[i] + "}" + StringUtils.Concat("\t", words));
	            }
	            for (int i = 0; i < data.CategoryRowCount; i++)
	            {
	                words = new List<string>();
	                for (int j = 0; j < data.ColumnCount; j++)
	                {
	                    string[] s = data.GetCategoryRowAt(i)[j];
	                    words.Add(s.Length == 0 ? "" : StringUtils.Concat(";", s));
	                }
	                for (int j = 0; j < data.CategoryColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                for (int j = 0; j < data.NumericColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                for (int j = 0; j < data.StringColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                for (int j = 0; j < data.MultiNumericColumnCount; j++)
	                {
	                    words.Add("");
	                }
	                writer.WriteLine("#!{C:" + data.CategoryRowNames[i] + "}" + StringUtils.Concat("\t", words));
	            }
	            for (int j = 0; j < data.RowCount; j++)
	            {
	                words = new List<string>();
	                for (int i = 0; i < data.ColumnCount; i++)
	                {
	                    string s1 = "" + data.Values.Get(j, i);
	                    if (addtlMatrices)
	                    {
	                        s1 += ";" + data.IsImputed[j, i] + ";" + data.Quality.Get(j, i);
	                    }
	                    words.Add(s1);
	                }
	                for (int i = 0; i < data.CategoryColumnCount; i++)
	                {
	                    string[] q = data.GetCategoryColumnEntryAt(i, j) ?? new string[0];
	                    words.Add((q.Length > 0 ? StringUtils.Concat(";", q) : ""));
	                }
	                for (int i = 0; i < data.NumericColumnCount; i++)
	                {
	                    words.Add("" + data.NumericColumns[i][j]);
	                }
	                for (int i = 0; i < data.StringColumnCount; i++)
	                {
	                    words.Add(data.StringColumns[i][j]);
	                }
	                for (int i = 0; i < data.MultiNumericColumnCount; i++)
	                {
	                    double[] q = data.MultiNumericColumns[i][j];
	                    words.Add((q.Length > 0 ? StringUtils.Concat(";", q) : ""));
	                }
	                string s = StringUtils.Concat("\t", words);
	                writer.WriteLine(s);
	            }
	        }
	    }

	    public static bool HasAnyDescription(IMatrixData data)
	    {
	        for (int i = 0; i < data.ColumnCount; i++)
	        {
	            if (data.ColumnDescriptions[i] != null && data.ColumnDescriptions[i].Length > 0)
	            {
	                return true;
	            }
	        }
	        for (int i = 0; i < data.CategoryColumnCount; i++)
	        {
	            if (data.CategoryColumnDescriptions[i] != null && data.CategoryColumnDescriptions[i].Length > 0)
	            {
	                return true;
	            }
	        }
	        for (int i = 0; i < data.NumericColumnCount; i++)
	        {
	            if (data.NumericColumnDescriptions[i] != null && data.NumericColumnDescriptions[i].Length > 0)
	            {
	                return true;
	            }
	        }
	        for (int i = 0; i < data.StringColumnCount; i++)
	        {
	            if (data.StringColumnDescriptions[i] != null && data.StringColumnDescriptions[i].Length > 0)
	            {
	                return true;
	            }
	        }
	        for (int i = 0; i < data.MultiNumericColumnCount; i++)
	        {
	            if (data.MultiNumericColumnDescriptions[i] != null && data.MultiNumericColumnDescriptions[i].Length > 0)
	            {
	                return true;
	            }
	        }
	        return false;
	    }
        public static void ReadMatrixFromFile(IMatrixData mdata, ProcessInfo processInfo, string filename, char separator)
        {
            var annotationRows = new Dictionary<string, string[]>();
            var colNames = TabSep.GetColumnNames(filename, commentPrefix,
                commentPrefixExceptions, annotationRows, separator);
            var typeRow = annotationRows["Type"];
            int[] eInds, nInds, cInds, tInds, mInds;
            ColumnIndices(typeRow, out eInds, out nInds, out cInds, out tInds, out mInds);
            var filters = new List<Tuple<Relation[], int[], bool>>();
	        int nrows;
	        using(StreamReader reader = FileUtils.GetReader(filename))
	        using (StreamReader auxReader = FileUtils.GetReader(filename))
	        {
	            nrows = GetRowCount(reader, auxReader, eInds, filters, separator);
	        }
	        using (StreamReader reader = FileUtils.GetReader(filename))
	        using (StreamReader auxReader = FileUtils.GetReader(filename))
	        {
	            LoadMatrixData(annotationRows, eInds, cInds, nInds, tInds, mInds, processInfo, colNames, mdata, reader,
	                auxReader, nrows, filename, separator, false, filters);
	        }
        }

	    private static void ColumnIndices(string[] typeRow, out int[] eInds, out int[] nInds, out int[] cInds, out int[] tInds, out int[] mInds)
	    {
	        var _eInds = new List<int>();
	        var _nInds = new List<int>();
	        var _cInds = new List<int>();
	        var _tInds = new List<int>();
	        var _mInds = new List<int>();
	        for (int i = 0; i < typeRow.Length; i++)
	        {
	            switch (typeRow[i])
	            {
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

	    public static void ReadMatrixFromFile(IMatrixData mdata, ProcessInfo processInfo, string filename,
	        int[] eInds, int[] nInds, int[] cInds, int[] tInds, int[] mInds,
	        Parameters[] mainFilterParameters, Parameters[] numericalFilterParameters, bool shortenExpressionColumnNames)
	    {
	        if (!File.Exists(filename))
	        {
	            processInfo.ErrString = "File '" + filename + "' does not exist.";
	            return;
	        }
	        string ftl = filename.ToLower();
	        bool csv = ftl.EndsWith(".csv") || ftl.EndsWith(".csv.gz");
	        char separator = csv ? ',' : '\t';
	        string[] colNames;
	        Dictionary<string, string[]> annotationRows = new Dictionary<string, string[]>();
	        try
	        {
	            colNames = TabSep.GetColumnNames(filename, commentPrefix, commentPrefixExceptions,
	                annotationRows, separator);
	        }
	        catch (Exception)
	        {
	            processInfo.ErrString = "Could not open the file '" + filename + "'. It is probably opened in another program.";
	            return;
	        }
	        string origin = filename;
	        List<Tuple<Relation[], int[], bool>> filters = new List<Tuple<Relation[], int[], bool>>();
	        string errString;
	        foreach (Parameters p in mainFilterParameters)
	        {
	            AddFilter(filters, p, eInds, out errString);
	            if (errString != null)
	            {
	                processInfo.ErrString = errString;
	                return;
	            }
	        }
	        foreach (Parameters p in numericalFilterParameters)
	        {
	            AddFilter(filters, p, nInds, out errString);
	            if (errString != null)
	            {
	                processInfo.ErrString = errString;
	                return;
	            }
	        }
	        int nrows;
	        using(StreamReader reader = FileUtils.GetReader(filename))
	        using (StreamReader auxReader = FileUtils.GetReader(filename))
	        {
	            nrows = GetRowCount(reader, auxReader, eInds, filters, separator);
	        }
	        using (StreamReader reader = FileUtils.GetReader(filename))
	        using (StreamReader auxReader = FileUtils.GetReader(filename))
	        {
	            LoadMatrixData(annotationRows, eInds, cInds, nInds, tInds, mInds, processInfo, colNames, mdata, reader,
	                auxReader, nrows, origin, separator, shortenExpressionColumnNames, filters);
	        }
	        GC.Collect();
	    }
	}
}