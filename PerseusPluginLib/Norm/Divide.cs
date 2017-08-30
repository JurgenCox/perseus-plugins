using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	public class Divide : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Divide all values by the specified quantity calculated on each row/column.";
		public string HelpOutput => "Normalized expression matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Divide";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => -5;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:Divide";
		public int GetMaxThreads(Parameters parameters) { return int.MaxValue; }

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			Parameter<int> access = param.GetParam<int>("Matrix access");
			bool rows = access.Value == 0;
			int what = param.GetParam<int>("Divide by what").Value;
			DivideImpl(rows, ArrayUtils.Mean, mdata, processInfo.NumThreads);
			switch (what){
				case 0:
					DivideImpl(rows, ArrayUtils.Sum, mdata, processInfo.NumThreads);
					break;
				case 1:
					DivideImpl(rows, ArrayUtils.Mean, mdata, processInfo.NumThreads);
					break;
				case 2:
					DivideImpl(rows, ArrayUtils.Median, mdata, processInfo.NumThreads);
					break;
				case 3:
					DivideImpl(rows, ArrayUtils.MostFrequentValue, mdata, processInfo.NumThreads);
					break;
				case 4:
					DivideImpl(rows, ArrayUtils.TukeyBiweight, mdata, processInfo.NumThreads);
					break;
				default:
					throw new Exception("Never get here.");
			}
		}

		public static void DivideImpl(bool rows, Func<double[], double> summarize, IMatrixData data, int nthreads){
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
				data.Values.Set(i, j, data.Values.Get(i, j)/med);
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
				data.Values.Set(i, j, data.Values.Get(i, j)/med);
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new SingleChoiceParam("Matrix access"){
					Values = new[]{"Rows", "Columns"},
					Help = "Specifies if the analysis is performed on the rows or the columns of the matrix."
				}, new SingleChoiceParam("Divide by what"){
					Values = new[]{"Sum", "Mean", "Median", "Most frequent value", "Tukey's biweight"},
					Value = 2
				});
		}
	}
}
