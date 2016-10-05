using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Annot{
	internal enum SummaryType{
		Median,
		Mean,
		Sum,
		StandardDeviation
	}

	public class AverageCategories : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"Rows that have a common term in one of the selected categorical columns are avereged. " +
				"Several different summarization methods can be selected.";

		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Average categories";
		public string Heading => "Annot. columns";
		public bool IsActive => true;
		public float DisplayRank => 2;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotcolumns:AverageCategories";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] catIndices = param.GetParam<int[]>("Categories").Value;
			int minSize = param.GetParam<int>("Min. size").Value;
			int summaryInd = param.GetParam<int>("Average type").Value;
			SummaryType type;
			switch (summaryInd){
				case 0:
					type = SummaryType.Median;
					break;
				case 1:
					type = SummaryType.Mean;
					break;
				case 2:
					type = SummaryType.Sum;
					break;
				case 3:
					type = SummaryType.StandardDeviation;
					break;
				default:
					throw new Exception("Never get here.");
			}
			List<float[]> exVals = new List<float[]>();
			List<string[]>[] stringAnnot = new List<string[]>[mdata.StringColumnCount];
			for (int i = 0; i < stringAnnot.Length; i++){
				stringAnnot[i] = new List<string[]>();
			}
			List<string> catNames = new List<string>();
			foreach (string[][] cat in catIndices.Select(mdata.GetCategoryColumnAt)){
				foreach (string[] t in cat){
					Array.Sort(t);
				}
				string[] allVals = ArrayUtils.UniqueValues(ArrayUtils.Concat(cat));
				foreach (string val in allVals){
					int[] inds = GetIndices(cat, val);
					if (inds.Length < minSize){
						continue;
					}
					float[] expProfile = new float[mdata.ColumnCount];
					for (int i = 0; i < expProfile.Length; i++){
						var vals = new List<double>();
						foreach (int ind in inds){
							double v = mdata.Values.Get(ind, i);
							if (!double.IsNaN(v) && !double.IsInfinity(v)){
								vals.Add(v);
							}
						}
						expProfile[i] = vals.Count > 0 ? Calc(vals, type) : float.NaN;
					}
					int prevInd = LookupPreviousInd(exVals, expProfile);
					if (prevInd == -1){
						catNames.Add(val);
						exVals.Add(expProfile);
						for (int i = 0; i < stringAnnot.Length; i++){
							var vals = new List<string>();
							foreach (int ind in inds){
								string v = mdata.StringColumns[i][ind];
								if (v.Length > 0){
									string[] m1 = v.Split(';');
									vals.AddRange(m1);
								}
							}
							string[] q = vals.ToArray();
							stringAnnot[i].Add(q);
						}
					} else{
						catNames[prevInd] = StringUtils.Concat(";",
							ArrayUtils.UniqueValues(ArrayUtils.Concat(catNames[prevInd].Split(';'), new[]{val})));
						for (int i = 0; i < stringAnnot.Length; i++){
							var vals = new List<string>();
							foreach (int ind in inds){
								string v = mdata.StringColumns[i][ind];
								if (v.Length > 0){
									string[] m1 = v.Split(';');
									vals.AddRange(m1);
								}
							}
							string[] q = vals.ToArray();
							stringAnnot[i].Add(q);
						}
					}
				}
			}
			List<string> stringColumnNames = new List<string>(new[]{"Category"});
			List<string[]> stringAnn = new List<string[]>{catNames.ToArray()};
			List<string> catColumnNames = mdata.StringColumnNames;
			List<string[][]> catAnn = new List<string[][]>();
			foreach (var w in stringAnnot.Select(t => t.ToArray())){
				foreach (var t1 in w){
					Array.Sort(t1);
				}
				catAnn.Add(w);
			}
			float[,] expressionValues = new float[exVals.Count, exVals[0].Length];
			for (int i = 0; i < expressionValues.GetLength(0); i++){
				for (int j = 0; j < expressionValues.GetLength(1); j++){
					expressionValues[i, j] = exVals[i][j];
				}
			}
			mdata.Values.Set(expressionValues);
			mdata.SetAnnotationColumns(stringColumnNames, stringAnn, catColumnNames, catAnn, new List<string>(),
				new List<double[]>(), new List<string>(), new List<double[][]>());
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> choice = mdata.CategoryColumnNames;
			int[] selection = ArrayUtils.ConsecutiveInts(choice.Count);
			return new Parameters(new MultiChoiceParam("Categories", selection){Values = choice},
				new SingleChoiceParam("Average type"){Values = new[]{"median", "mean", "sum", "standard deviation"}},
				new IntParam("Min. size", 3));
		}

		private static float Calc(IList<double> vals, SummaryType type){
			switch (type){
				case SummaryType.Median:
					return (float) ArrayUtils.Median(vals);
				case SummaryType.Mean:
					return (float) ArrayUtils.Mean(vals);
				case SummaryType.Sum:
					return (float) ArrayUtils.Sum(vals);
				case SummaryType.StandardDeviation:
					return (float) ArrayUtils.StandardDeviation(vals);
				default:
					throw new Exception("Never get here.");
			}
		}

		private static int LookupPreviousInd(IList<float[]> vals, IList<float> profile){
			for (int i = 0; i < vals.Count; i++){
				if (ArrayUtils.EqualArrays(vals[i], profile)){
					return i;
				}
			}
			return -1;
		}

		private static int[] GetIndices(IList<string[]> cat, string val){
			var result = new List<int>();
			for (int i = 0; i < cat.Count; i++){
				if (Array.BinarySearch(cat[i], val) >= 0){
					result.Add(i);
				}
			}
			return result.ToArray();
		}
	}
}