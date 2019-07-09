using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Rearrange{
	public class ChangeColumnType : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Convert the type of selected columns to another desired type.";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Change column type";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:ChangeColumnType";
		public int GetMaxThreads(Parameters parameters) { return 1; }

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
				ParameterWithSubParams<int> sp = param.GetParamWithSubParams<int>("Source type");
			Parameters subParams = sp.GetSubParameters();
			int[] colInds = subParams.GetParam<int[]>("Columns").Value;
			int which = subParams.GetParam<int>("Target type").Value;
			switch (sp.Value){
				case 0:
					ExpressionToNumeric(colInds, mdata);
					break;
				case 1:
					switch (which){
						case 0:
							NumericToCategorical(colInds, mdata);
							break;
						case 1:
							NumericToExpression(colInds, mdata);
							break;
						case 2:
							NumericToString(colInds, mdata);
							break;
						default:
							throw new Exception("Never get here");
					}
					break;
				case 2:
					if (which == 0){
						CategoricalToNumeric(colInds, mdata);
					} else{
						CategoricalToString(colInds, mdata);
					}
					break;
				case 3:
					switch (which){
						case 0:
							StringToCategorical(colInds, mdata);
							break;
						case 1:
							StringToExpression(colInds, mdata);
							break;
						case 2:
							StringToNumerical(colInds, mdata);
							break;
						case 3:
							StringToMultiNumerical(colInds, mdata);
							break;
						default:
							throw new Exception("Never get here");
					}
					break;
				case 4:
					switch (which){
						case 0:
							MultiNumericToString(colInds, mdata);
							break;
						default:
							throw new Exception("Never get here");
					}
					break;
				default:
					throw new Exception("Never get here");
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			string[] choice = new[]{"Main", "Numerical", "Categorical", "Text", "Multi-numerical"};
			List<Parameters> subParams = new List<Parameters>{
				GetSubParams(mdata.ColumnNames, GetExpressionSelection()),
				GetSubParams(mdata.NumericColumnNames, GetNumericSelection()),
				GetSubParams(mdata.CategoryColumnNames, GetCategoricalSelection()),
				GetSubParams(mdata.StringColumnNames, GetStringSelection()),
				GetSubParams(mdata.MultiNumericColumnNames, GetMultiNumericSelection())
			};
			return
				new Parameters(new Parameter[]{
					new SingleChoiceWithSubParams("Source type"){
						Values = choice,
						Help = "What is the original type of the column(s) whose type should be changed?",
						SubParams = subParams,
						ParamNameWidth = 136,
						TotalWidth = 731
					}
				});
		}

		private static void StringToCategorical(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.StringColumnCount);
			string[] names = ArrayUtils.SubArray(mdata.StringColumnNames, colInds);
			string[] descriptions = ArrayUtils.SubArray(mdata.StringColumnDescriptions, colInds);
			string[][] str = ArrayUtils.SubArray(mdata.StringColumns, colInds);
			string[][][] newCat = new string[str.Length][][];
			for (int j = 0; j < str.Length; j++){
				newCat[j] = new string[str[j].Length][];
				for (int i = 0; i < newCat[j].Length; i++){
					if (str[j][i] == null || str[j][i].Length == 0){
						newCat[j][i] = new string[0];
					} else{
						string[] x = str[j][i].Split(';');
						Array.Sort(x);
						newCat[j][i] = x;
					}
				}
			}
			for (int i = 0; i < names.Length; i++){
				mdata.AddCategoryColumn(names[i], descriptions[i], newCat[i]);
			}
			mdata.StringColumns = ArrayUtils.SubList(mdata.StringColumns, inds);
			mdata.StringColumnNames = ArrayUtils.SubList(mdata.StringColumnNames, inds);
			mdata.ColumnDescriptions = ArrayUtils.SubList(mdata.StringColumnDescriptions, inds);
		}

		private static void StringToNumerical(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.StringColumnCount);
			string[] name = ArrayUtils.SubArray(mdata.StringColumnNames, colInds);
			string[] description = ArrayUtils.SubArray(mdata.StringColumnDescriptions, colInds);
			string[][] str = ArrayUtils.SubArray(mdata.StringColumns, colInds);
			double[][] newNum = new double[str.Length][];
			for (int j = 0; j < str.Length; j++){
				newNum[j] = new double[str[j].Length];
				for (int i = 0; i < newNum[j].Length; i++){
					if (str[j][i] == null || str[j][i].Length == 0){
						newNum[j][i] = double.NaN;
					} else{
						string x = str[j][i];
						bool success = Parser.TryDouble(x, out double d);
						newNum[j][i] = success ? d : double.NaN;
					}
				}
			}
			mdata.NumericColumnNames.AddRange(name);
			mdata.NumericColumnDescriptions.AddRange(description);
			mdata.NumericColumns.AddRange(newNum);
			mdata.StringColumns = ArrayUtils.SubList(mdata.StringColumns, inds);
			mdata.StringColumnNames = ArrayUtils.SubList(mdata.StringColumnNames, inds);
			mdata.ColumnDescriptions = ArrayUtils.SubList(mdata.StringColumnDescriptions, inds);
		}

		private static void NumericToString(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.NumericColumnCount);
			string[] name = ArrayUtils.SubArray(mdata.NumericColumnNames, colInds);
			string[] description = ArrayUtils.SubArray(mdata.NumericColumnDescriptions, colInds);
			double[][] num = ArrayUtils.SubArray(mdata.NumericColumns, colInds);
			string[][] newString = new string[num.Length][];
			for (int j = 0; j < num.Length; j++){
				newString[j] = new string[num[j].Length];
				for (int i = 0; i < newString[j].Length; i++){
					double x = num[j][i];
					newString[j][i] = "" + x;
				}
			}
			mdata.StringColumnNames.AddRange(name);
			mdata.StringColumnDescriptions.AddRange(description);
			mdata.StringColumns.AddRange(newString);
			mdata.NumericColumns = ArrayUtils.SubList(mdata.NumericColumns, inds);
			mdata.NumericColumnNames = ArrayUtils.SubList(mdata.NumericColumnNames, inds);
			mdata.NumericColumnDescriptions = ArrayUtils.SubList(mdata.NumericColumnDescriptions, inds);
		}

		private static void MultiNumericToString(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.MultiNumericColumnCount);
			string[] name = ArrayUtils.SubArray(mdata.MultiNumericColumnNames, colInds);
			string[] description = ArrayUtils.SubArray(mdata.MultiNumericColumnDescriptions, colInds);
			double[][][] num = ArrayUtils.SubArray(mdata.MultiNumericColumns, colInds);
			string[][] newString = new string[num.Length][];
			for (int j = 0; j < num.Length; j++){
				newString[j] = new string[num[j].Length];
				for (int i = 0; i < newString[j].Length; i++){
					double[] x = num[j][i];
					newString[j][i] = "" + StringUtils.Concat(";", x);
				}
			}
			mdata.StringColumnNames.AddRange(name);
			mdata.ColumnDescriptions.AddRange(description);
			mdata.StringColumns.AddRange(newString);
			mdata.MultiNumericColumns = ArrayUtils.SubList(mdata.MultiNumericColumns, inds);
			mdata.MultiNumericColumnNames = ArrayUtils.SubList(mdata.MultiNumericColumnNames, inds);
			mdata.MultiNumericColumnDescriptions = ArrayUtils.SubList(mdata.MultiNumericColumnDescriptions, inds);
		}

		private static void StringToMultiNumerical(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.StringColumnCount);
			string[] name = ArrayUtils.SubArray(mdata.StringColumnNames, colInds);
			string[] description = ArrayUtils.SubArray(mdata.StringColumnDescriptions, colInds);
			string[][] str = ArrayUtils.SubArray(mdata.StringColumns, colInds);
			double[][][] newMNum = new double[str.Length][][];
			for (int j = 0; j < str.Length; j++){
				newMNum[j] = new double[str[j].Length][];
				for (int i = 0; i < newMNum[j].Length; i++){
					if (str[j][i] == null || str[j][i].Length == 0){
						newMNum[j][i] = new double[0];
					} else{
						string x = str[j][i];
						string[] y = x.Length > 0 ? x.Split(';') : new string[0];
						newMNum[j][i] = new double[y.Length];
						for (int k = 0; k < y.Length; k++){
							bool success = Parser.TryDouble(y[k], out double d);
							newMNum[j][i][k] = success ? d : double.NaN;
						}
					}
				}
			}
			mdata.MultiNumericColumnNames.AddRange(name);
			mdata.MultiNumericColumnDescriptions.AddRange(description);
			mdata.MultiNumericColumns.AddRange(newMNum);
			mdata.StringColumns = ArrayUtils.SubList(mdata.StringColumns, inds);
			mdata.StringColumnNames = ArrayUtils.SubList(mdata.StringColumnNames, inds);
			mdata.ColumnDescriptions = ArrayUtils.SubList(mdata.StringColumnDescriptions, inds);
		}

		private static void CategoricalToNumeric(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.CategoryColumnCount);
			string[] name = ArrayUtils.SubArray(mdata.CategoryColumnNames, colInds);
			string[] description = ArrayUtils.SubArray(mdata.CategoryColumnDescriptions, colInds);
			string[][][] cat = PerseusPluginUtils.GetCategoryColumns(mdata, colInds).ToArray();
			double[][] newNum = new double[cat.Length][];
			for (int j = 0; j < cat.Length; j++){
				newNum[j] = new double[cat[j].Length];
				for (int i = 0; i < newNum[j].Length; i++){
					if (cat[j][i] == null || cat[j][i].Length == 0){
						newNum[j][i] = double.NaN;
					} else{
						bool s = Parser.TryDouble(cat[j][i][0], out double x);
						if (s){
							newNum[j][i] = x;
						} else{
							newNum[j][i] = double.NaN;
						}
					}
				}
			}
			mdata.NumericColumnNames.AddRange(name);
			mdata.NumericColumnDescriptions.AddRange(description);
			mdata.NumericColumns.AddRange(newNum);
			mdata.CategoryColumns = PerseusPluginUtils.GetCategoryColumns(mdata, inds);
			mdata.CategoryColumnNames = ArrayUtils.SubList(mdata.CategoryColumnNames, inds);
			mdata.CategoryColumnDescriptions = ArrayUtils.SubList(mdata.CategoryColumnDescriptions, inds);
		}

		private static void CategoricalToString(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.CategoryColumnCount);
			string[] names = ArrayUtils.SubArray(mdata.CategoryColumnNames, colInds);
			string[] descriptions = ArrayUtils.SubArray(mdata.CategoryColumnDescriptions, colInds);
			string[][][] cat = PerseusPluginUtils.GetCategoryColumns(mdata, colInds).ToArray();
			string[][] newString = new string[cat.Length][];
			for (int j = 0; j < cat.Length; j++){
				newString[j] = new string[cat[j].Length];
				for (int i = 0; i < newString[j].Length; i++){
					if (cat[j][i] == null || cat[j][i].Length == 0){
						newString[j][i] = "";
					} else{
						newString[j][i] = StringUtils.Concat(";", cat[j][i]);
					}
				}
			}
			mdata.StringColumnNames.AddRange(names);
			mdata.StringColumnDescriptions.AddRange(descriptions);
			mdata.StringColumns.AddRange(newString);
			mdata.CategoryColumns = PerseusPluginUtils.GetCategoryColumns(mdata, inds);
			mdata.CategoryColumnNames = ArrayUtils.SubList(mdata.CategoryColumnNames, inds);
			mdata.CategoryColumnDescriptions = ArrayUtils.SubList(mdata.CategoryColumnDescriptions, inds);
		}

		private static void NumericToCategorical(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.NumericColumnCount);
			string[] names = ArrayUtils.SubArray(mdata.NumericColumnNames, colInds);
			string[] descriptions = ArrayUtils.SubArray(mdata.NumericColumnDescriptions, colInds);
			double[][] num = ArrayUtils.SubArray(mdata.NumericColumns, colInds);
			string[][][] newCat = new string[num.Length][][];
			for (int j = 0; j < num.Length; j++){
				newCat[j] = new string[num[j].Length][];
				for (int i = 0; i < newCat[j].Length; i++){
					if (double.IsNaN(num[j][i]) || double.IsInfinity(num[j][i])){
						newCat[j][i] = new string[0];
					} else{
						newCat[j][i] = new[]{"" + num[j][i]};
					}
				}
			}
			for (int i = 0; i < names.Length; i++){
				mdata.AddCategoryColumn(names[i], descriptions[i], newCat[i]);
			}
			mdata.NumericColumns = ArrayUtils.SubList(mdata.NumericColumns, inds);
			mdata.NumericColumnNames = ArrayUtils.SubList(mdata.NumericColumnNames, inds);
			mdata.NumericColumnDescriptions = ArrayUtils.SubList(mdata.NumericColumnDescriptions, inds);
		}

		private static void NumericToExpression(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.NumericColumnCount);
			string[] names = ArrayUtils.SubArray(mdata.NumericColumnNames, colInds);
			string[] descriptions = ArrayUtils.SubArray(mdata.NumericColumnDescriptions, colInds);
			double[][] num = ArrayUtils.SubArray(mdata.NumericColumns, colInds);
			double[][] newEx = new double[num.Length][];
			for (int j = 0; j < num.Length; j++){
				newEx[j] = new double[num[j].Length];
				for (int i = 0; i < newEx[j].Length; i++){
					newEx[j][i] = num[j][i];
				}
			}
			double[,] newExp = new double[mdata.RowCount,mdata.ColumnCount + num.Length];
			double[,] newQual = new double[mdata.RowCount,mdata.ColumnCount + num.Length];
			bool[,] newIsImputed = new bool[mdata.RowCount,mdata.ColumnCount + num.Length];
			for (int i = 0; i < mdata.RowCount; i++){
				for (int j = 0; j < mdata.ColumnCount; j++){
					newExp[i, j] = mdata.Values.Get(i, j);
					newQual[i, j] = mdata.Quality.Get(i, j);
					newIsImputed[i, j] = mdata.IsImputed[i, j];
				}
				for (int j = 0; j < newEx.Length; j++){
					newExp[i, j + mdata.ColumnCount] = newEx[j][i];
					newQual[i, j + mdata.ColumnCount] = double.NaN;
					newIsImputed[i, j + mdata.ColumnCount] = false;
				}
			}
			mdata.Values.Set(newExp);
			mdata.Quality.Set(newQual);
			mdata.IsImputed.Set(newIsImputed);
			mdata.ColumnNames.AddRange(names);
			mdata.ColumnDescriptions.AddRange(descriptions);
			mdata.NumericColumns = ArrayUtils.SubList(mdata.NumericColumns, inds);
			mdata.NumericColumnNames = ArrayUtils.SubList(mdata.NumericColumnNames, inds);
			mdata.NumericColumnDescriptions = ArrayUtils.SubList(mdata.NumericColumnDescriptions, inds);
			for (int i = 0; i < mdata.CategoryRowCount; i++){
				mdata.SetCategoryRowAt(ExtendCategoryRow(mdata.GetCategoryRowAt(i), num.Length), i);
			}
			for (int i = 0; i < mdata.NumericRows.Count; i++){
				mdata.NumericRows[i] = ExtendNumericRow(mdata.NumericRows[i], num.Length);
			}
			for (int i = 0; i < mdata.StringRows.Count; i++)
			{
				mdata.StringRows[i] = ExtendStringRow(mdata.StringRows[i], num.Length);
			}
		}

		private static void StringToExpression(IList<int> colInds, IMatrixData mdata){
			int[] inds = ArrayUtils.Complement(colInds, mdata.StringColumnCount);
			string[] names = ArrayUtils.SubArray(mdata.StringColumnNames, colInds);
			string[] descriptions = ArrayUtils.SubArray(mdata.StringColumnDescriptions, colInds);
			string[][] str = ArrayUtils.SubArray(mdata.StringColumns, colInds);
			double[][] newEx = new double[str.Length][];
			for (int j = 0; j < str.Length; j++){
				newEx[j] = new double[str[j].Length];
				for (int i = 0; i < newEx[j].Length; i++){
					bool success = Parser.TryDouble(str[j][i], out double f);
					newEx[j][i] = success ? f : double.NaN;
				}
			}
			double[,] newExp = new double[mdata.RowCount,mdata.ColumnCount + str.Length];
			double[,] newQual = new double[mdata.RowCount,mdata.ColumnCount + str.Length];
			bool[,] newIsImputed = new bool[mdata.RowCount,mdata.ColumnCount + str.Length];
			for (int i = 0; i < mdata.RowCount; i++){
				for (int j = 0; j < mdata.ColumnCount; j++){
					newExp[i, j] = mdata.Values.Get(i, j);
					newQual[i, j] = mdata.Quality.Get(i, j);
					newIsImputed[i, j] = mdata.IsImputed[i, j];
				}
				for (int j = 0; j < newEx.Length; j++){
					newExp[i, j + mdata.ColumnCount] = newEx[j][i];
					newQual[i, j + mdata.ColumnCount] = double.NaN;
					newIsImputed[i, j + mdata.ColumnCount] = false;
				}
			}
			mdata.Values.Set(newExp);
			mdata.Quality.Set(newQual);
			mdata.IsImputed.Set(newIsImputed);
			mdata.ColumnNames.AddRange(names);
			mdata.ColumnDescriptions.AddRange(descriptions);
			mdata.StringColumns = ArrayUtils.SubList(mdata.StringColumns, inds);
			mdata.StringColumnNames = ArrayUtils.SubList(mdata.StringColumnNames, inds);
			mdata.ColumnDescriptions = ArrayUtils.SubList(mdata.StringColumnDescriptions, inds);
			for (int i = 0; i < mdata.CategoryRowCount; i++){
				mdata.SetCategoryRowAt(ExtendCategoryRow(mdata.GetCategoryRowAt(i), str.Length), i);
			}
			for (int i = 0; i < mdata.NumericRows.Count; i++){
				mdata.NumericRows[i] = ExtendNumericRow(mdata.NumericRows[i], str.Length);
			}
			for (int i = 0; i < mdata.StringRows.Count; i++)
			{
				mdata.StringRows[i] = ExtendStringRow(mdata.StringRows[i], str.Length);
			}
		}

		private static void ExpressionToNumeric(IList<int> colInds, IMatrixData mdata){
			int[] remainingInds = ArrayUtils.Complement(colInds, mdata.ColumnCount);
			foreach (int colInd in colInds){
				double[] d = ArrayUtils.ToDoubles(mdata.Values.GetColumn(colInd));
				mdata.AddNumericColumn(mdata.ColumnNames[colInd], mdata.ColumnDescriptions[colInd], d);
			}
			mdata.ExtractColumns(remainingInds);
		}

		private static double[] ExtendNumericRow(IList<double> numericRow, int add){
			double[] result = new double[numericRow.Count + add];
			for (int i = 0; i < numericRow.Count; i++){
				result[i] = numericRow[i];
			}
			for (int i = numericRow.Count; i < numericRow.Count + add; i++){
				result[i] = double.NaN;
			}
			return result;
		}

		private static string[][] ExtendCategoryRow(IList<string[]> categoryRow, int add){
			string[][] result = new string[categoryRow.Count + add][];
			for (int i = 0; i < categoryRow.Count; i++){
				result[i] = categoryRow[i];
			}
			for (int i = categoryRow.Count; i < categoryRow.Count + add; i++){
				result[i] = new string[0];
			}
			return result;
		}

		private static string[] ExtendStringRow(IList<string> stringRow, int add)
		{
			var result = new string[stringRow.Count + add];
			for (int i = 0; i < stringRow.Count; i++){
				result[i] = stringRow[i];
			}
			for (int i = stringRow.Count; i < stringRow.Count + add; i++)
			{
				result[i] = string.Empty;
			}
			return result;
		}

		private static Parameters GetSubParams(IList<string> values, IList<string> options){
			return
				new Parameters(new MultiChoiceParam("Columns"){Values = values, Help = "Select here the column whose type should be changed."}, new SingleChoiceParam("Target type", 0){
					Values = options,
					Help = "The type that these columns will have in the result table."
				});
		}

		private static string[] GetStringSelection() { return new[]{"Categorical", "Main", "Numerical", "Multi numerical"}; }
		private static string[] GetCategoricalSelection() { return new[]{"Numerical", "Text"}; }
		private static string[] GetExpressionSelection() { return new[]{"Numerical"}; }
		private static string[] GetNumericSelection() { return new[]{"Categorical", "Main", "Text"}; }
		private static string[] GetMultiNumericSelection() { return new[]{"Text"}; }
	}
}