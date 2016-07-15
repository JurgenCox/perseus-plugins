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

namespace PerseusPluginLib.Norm{
	public class Subtract : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "The specified quantity calculated on each row/column is subtracted from each value.";
		public string HelpOutput => "Normalized expression matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Subtract";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => -6;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:Subtract";

		public int GetMaxThreads(Parameters parameters) {
			return int.MaxValue;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
				ParameterWithSubParams<int> access = param.GetParamWithSubParams<int>("Matrix access");
			bool rows = access.Value == 0;
			int groupInd;
			if (rows){
				groupInd = access.GetSubParameters().GetParam<int>("Grouping").Value - 1;
			} else{
				groupInd = -1;
			}
			int what = param.GetParam<int>("Subtract what").Value;
			if (groupInd < 0){
				SubtractValues(rows, GetFunc(what), mdata, processInfo.NumThreads);
			} else{
				string[][] catRow = mdata.GetCategoryRowAt(groupInd);
				foreach (string[] t in catRow){
					if (t.Length > 1){
						processInfo.ErrString = "The groups are overlapping.";
						return;
					}
				}
				SubtractGroups(mdata, catRow, GetFunc(what));
			}
		}

		private static void SubtractGroups(IMatrixData mdata, IList<string[]> catRow, Func<double[], double> func){
			string[] groupVals = ArrayUtils.UniqueValuesPreserveOrder(catRow);
			foreach (int[] inds in groupVals.Select(groupVal => ZScore.GetIndices(catRow, groupVal))){
				SubtractGroup(mdata, inds, func);
			}
		}

		private static void SubtractGroup(IMatrixData data, IList<int> inds, Func<double[], double> func){
			for (int i = 0; i < data.RowCount; i++){
				double[] vals = new double[inds.Count];
				for (int j = 0; j < inds.Count; j++){
					double q = data.Values.Get(i, inds[j]);
					vals[j] = q;
				}
				double mean = func(vals);
				foreach (int t in inds){
					data.Values.Set(i, t, (float)((data.Values.Get(i, t) - mean)));
				}
			}
		}

		private static Func<double[], double> GetFunc(int what){
			switch (what){
				case 0:
					return ArrayUtils.Mean;
				case 1:
					return ArrayUtils.Median;
				case 2:
					return ArrayUtils.MostFrequentValue;
				case 3:
					return ArrayUtils.TukeyBiweight;
				default:
					throw new Exception("Never get here.");
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString) {
			return
				new Parameters(new Parameter[]{
					new SingleChoiceWithSubParams("Matrix access"){
						Values = new[]{"Rows", "Columns"}, ParamNameWidth = 136, TotalWidth = 731,
						SubParams =
							new[]{
								new Parameters(new SingleChoiceParam("Grouping")
								{Values = ArrayUtils.Concat(new[]{"<No grouping>"}, mdata.CategoryRowNames)}),
								new Parameters()
							},
						Help = "Specifies if the subtraction is performed on the rows or the columns of the matrix."
					},
					new SingleChoiceParam("Subtract what"){
						Values = new[]{
							"Mean", "Median", "Most frequent value", "Tukey's biweight"
						},
						Value = 1
					}
				});
		}

		public static void SubtractValues(bool rows, Func<double[], double> summarize, IMatrixData data, int nthreads){
			if (rows){
				new ThreadDistributor(nthreads, data.RowCount, i => Calc1(i, summarize, data)).Start();
			} else{
				new ThreadDistributor(nthreads, data.ColumnCount, j => Calc2(j, summarize, data)).Start();
			}
		}

		private static void Calc1(int i, Func<double[], double> summarize, IMatrixData data){
			List<double> vals = new List<double>();
			for (int j = 0; j < data.ColumnCount; j++){
				double q = data.Values.Get(i, j);
				if (!double.IsNaN(q) && !double.IsInfinity(q)){
					vals.Add(q);
				}
			}
			double med = summarize(vals.ToArray());
			for (int j = 0; j < data.ColumnCount; j++){
				data.Values.Set(i, j, data.Values.Get(i, j)-(float)med);
			}
		}

		private static void Calc2(int j, Func<double[], double> summarize, IMatrixData data){
			List<double> vals = new List<double>();
			for (int i = 0; i < data.RowCount; i++){
				double q = data.Values.Get(i, j);
				if (!double.IsNaN(q) && !double.IsInfinity(q)){
					vals.Add(q);
				}
			}
			double med = summarize(vals.ToArray());
			for (int i = 0; i < data.RowCount; i++){
				data.Values.Set(i, j, data.Values.Get(i, j)-(float)med);
			}
		}
	}
}