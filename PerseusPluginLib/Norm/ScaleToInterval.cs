using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	public class ScaleToInterval : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Scale to interval";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => -7;
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:ScaleToInterval";

		public string Description
			=>
				"A linear transformation is applied to the values in each row/column such that the minima " +
				"and maxima coincide with the specified values.";

		public int GetMaxThreads(Parameters parameters){
			return int.MaxValue;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			bool rows = param.GetParam<int>("Matrix access").Value == 0;
			double min = param.GetParam<double>("Minimum").Value;
			double max = param.GetParam<double>("Maximum").Value;
			MapToInterval1(rows, mdata, min, max, processInfo.NumThreads);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new SingleChoiceParam("Matrix access"){
					Values = new[]{"Rows", "Columns"},
					Help = "Specifies if the analysis is performed on the rows or the columns of the matrix."
				}, new DoubleParam("Minimum", 0), new DoubleParam("Maximum", 1));
		}

		public static void MapToInterval1(bool rows, IMatrixData data, double min, double max, int nthreads){
			if (rows){
				new ThreadDistributor(nthreads, data.RowCount, i => Calc1(i, data, min, max)).Start();
			} else{
				new ThreadDistributor(nthreads, data.ColumnCount, j => Calc2(j, data, min, max)).Start();
			}
		}

		private static void Calc1(int i, IMatrixData data, double min, double max){
			List<double> vals = new List<double>();
			for (int j = 0; j < data.ColumnCount; j++){
				double q = data.Values.Get(i, j);
				if (!double.IsNaN(q) && !double.IsInfinity(q)){
					vals.Add(q);
				}
			}
			ArrayUtils.MinMax(vals, out double mind, out double maxd);
			for (int j = 0; j < data.ColumnCount; j++){
				data.Values.Set(i, j, min + (max - min)/(maxd - mind)*(data.Values.Get(i, j) - mind));
			}
		}

		private static void Calc2(int j, IMatrixData data, double min, double max){
			List<double> vals = new List<double>();
			for (int i = 0; i < data.RowCount; i++){
				double q = data.Values.Get(i, j);
				if (!double.IsNaN(q) && !double.IsInfinity(q)){
					vals.Add(q);
				}
			}
			ArrayUtils.MinMax(vals, out double mind, out double maxd);
			for (int i = 0; i < data.RowCount; i++){
				data.Values.Set(i, j, min + (max - min)/(maxd - mind)*(data.Values.Get(i, j) - mind));
			}
		}
	}
}