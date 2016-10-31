using System;
using System.Collections.Generic;
using System.IO;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Export{
	public class TabSeparatedExport : IMatrixExport{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => Bitmap2.GetImage("Save-icon.png");

		public string Description
			=> "Save the matrix to a tab-separated text file. Information on column types will be retained.";

		public string Name => "Generic matrix export";
		public bool IsActive => true;
		public float DisplayRank => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixExport:TabSeparatedExport";

		public void Export(Parameters parameters, IMatrixData data, ProcessInfo processInfo){
			string filename = parameters.GetParam<string>("File name").Value;
			bool addtlMatrices = parameters.GetParam<bool>("Write quality and imputed matrices").Value;
			addtlMatrices = addtlMatrices && data.IsImputed != null && data.Quality != null && data.IsImputed.IsInitialized() &&
							data.Quality.IsInitialized();
			try{
				StreamWriter writer = new StreamWriter(filename);
				List<string> words = new List<string>();
				for (int i = 0; i < data.ColumnCount; i++){
					words.Add(data.ColumnNames[i]);
				}
				for (int i = 0; i < data.CategoryColumnCount; i++){
					words.Add(data.CategoryColumnNames[i]);
				}
				for (int i = 0; i < data.NumericColumnCount; i++){
					words.Add(data.NumericColumnNames[i]);
				}
				for (int i = 0; i < data.StringColumnCount; i++){
					words.Add(data.StringColumnNames[i]);
				}
				for (int i = 0; i < data.MultiNumericColumnCount; i++){
					words.Add(data.MultiNumericColumnNames[i]);
				}
				writer.WriteLine(StringUtils.Concat("\t", words));
				if (HasAnyDescription(data)){
					words = new List<string>();
					for (int i = 0; i < data.ColumnCount; i++){
						words.Add(data.ColumnDescriptions[i] ?? "");
					}
					for (int i = 0; i < data.CategoryColumnCount; i++){
						words.Add(data.CategoryColumnDescriptions[i] ?? "");
					}
					for (int i = 0; i < data.NumericColumnCount; i++){
						words.Add(data.NumericColumnDescriptions[i] ?? "");
					}
					for (int i = 0; i < data.StringColumnCount; i++){
						words.Add(data.StringColumnDescriptions[i] ?? "");
					}
					for (int i = 0; i < data.MultiNumericColumnCount; i++){
						words.Add(data.MultiNumericColumnDescriptions[i] ?? "");
					}
					writer.WriteLine("#!{Description}" + StringUtils.Concat("\t", words));
				}
				words = new List<string>();
				for (int i = 0; i < data.ColumnCount; i++){
					words.Add("E");
				}
				for (int i = 0; i < data.CategoryColumnCount; i++){
					words.Add("C");
				}
				for (int i = 0; i < data.NumericColumnCount; i++){
					words.Add("N");
				}
				for (int i = 0; i < data.StringColumnCount; i++){
					words.Add("T");
				}
				for (int i = 0; i < data.MultiNumericColumnCount; i++){
					words.Add("M");
				}
				writer.WriteLine("#!{Type}" + StringUtils.Concat("\t", words));
				for (int i = 0; i < data.NumericRowCount; i++){
					words = new List<string>();
					for (int j = 0; j < data.ColumnCount; j++){
						words.Add("" + data.NumericRows[i][j]);
					}
					for (int j = 0; j < data.CategoryColumnCount; j++){
						words.Add("");
					}
					for (int j = 0; j < data.NumericColumnCount; j++){
						words.Add("");
					}
					for (int j = 0; j < data.StringColumnCount; j++){
						words.Add("");
					}
					for (int j = 0; j < data.MultiNumericColumnCount; j++){
						words.Add("");
					}
					writer.WriteLine("#!{N:" + data.NumericRowNames[i] + "}" + StringUtils.Concat("\t", words));
				}
				for (int i = 0; i < data.CategoryRowCount; i++){
					words = new List<string>();
					for (int j = 0; j < data.ColumnCount; j++){
						string[] s = data.GetCategoryRowAt(i)[j];
						words.Add(s.Length == 0 ? "" : StringUtils.Concat(";", s));
					}
					for (int j = 0; j < data.CategoryColumnCount; j++){
						words.Add("");
					}
					for (int j = 0; j < data.NumericColumnCount; j++){
						words.Add("");
					}
					for (int j = 0; j < data.StringColumnCount; j++){
						words.Add("");
					}
					for (int j = 0; j < data.MultiNumericColumnCount; j++){
						words.Add("");
					}
					writer.WriteLine("#!{C:" + data.CategoryRowNames[i] + "}" + StringUtils.Concat("\t", words));
				}
				int n = data.RowCount;
				for (int j = 0; j < n; j++){
					words = new List<string>();
					for (int i = 0; i < data.ColumnCount; i++){
						string s1 = "" + data.Values.Get(j, i);
						if (addtlMatrices){
							s1 += ";" + data.IsImputed[j, i] + ";" + data.Quality.Get(j, i);
						}
						words.Add(s1);
					}
					for (int i = 0; i < data.CategoryColumnCount; i++){
						string[] q = data.GetCategoryColumnEntryAt(i, j) ?? new string[0];
						words.Add((q.Length > 0 ? StringUtils.Concat(";", q) : ""));
					}
					for (int i = 0; i < data.NumericColumnCount; i++){
						words.Add("" + data.NumericColumns[i][j]);
					}
					for (int i = 0; i < data.StringColumnCount; i++){
						words.Add(data.StringColumns[i][j]);
					}
					for (int i = 0; i < data.MultiNumericColumnCount; i++){
						double[] q = data.MultiNumericColumns[i][j];
						words.Add((q.Length > 0 ? StringUtils.Concat(";", q) : ""));
					}
					string s = StringUtils.Concat("\t", words);
					writer.WriteLine(s);
					processInfo.Progress(100*(j + 1)/n);
				}
				writer.Close();
			} catch (Exception e){
				processInfo.ErrString = e.Message;
			}
		}

		private static bool HasAnyDescription(IMatrixData data){
			for (int i = 0; i < data.ColumnCount; i++){
				if (data.ColumnDescriptions[i] != null && data.ColumnDescriptions[i].Length > 0){
					return true;
				}
			}
			for (int i = 0; i < data.CategoryColumnCount; i++){
				if (data.CategoryColumnDescriptions[i] != null && data.CategoryColumnDescriptions[i].Length > 0){
					return true;
				}
			}
			for (int i = 0; i < data.NumericColumnCount; i++){
				if (data.NumericColumnDescriptions[i] != null && data.NumericColumnDescriptions[i].Length > 0){
					return true;
				}
			}
			for (int i = 0; i < data.StringColumnCount; i++){
				if (data.StringColumnDescriptions[i] != null && data.StringColumnDescriptions[i].Length > 0){
					return true;
				}
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++){
				if (data.MultiNumericColumnDescriptions[i] != null && data.MultiNumericColumnDescriptions[i].Length > 0){
					return true;
				}
			}
			return false;
		}

		public Parameters GetParameters(IMatrixData matrixData, ref string errorString){
			return new Parameters(new FileParam("File name"){Filter = "Tab separated file (*.txt)|*.txt", Save = true},
				new BoolParam("Write quality and imputed matrices", false));
		}
	}
}