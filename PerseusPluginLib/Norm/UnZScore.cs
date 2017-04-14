using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	public class UnZScore : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Un-Z-score";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => 50;
		public string HelpOutput => "Normalized expression matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:UnZScore";

		public string Description
			=>
				"Providing the means and standard deviations used in a z-score transformation the data is " +
				"transformed back to what it was before z-scoring.";

		public int GetMaxThreads(Parameters parameters){
			return int.MaxValue;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			int rowMeanInd = 0;
			int rowDevInd = 0;
			for (int i = 0; i < mdata.NumericColumnCount; i++){
				if (mdata.NumericColumnNames[i].ToLower().Equals("mean")){
					rowMeanInd = i;
				}
				if (mdata.NumericColumnNames[i].ToLower().Equals("std. dev.")){
					rowDevInd = i;
				}
			}
			Parameters rowSubParams =
				new Parameters(new SingleChoiceParam("Mean", rowMeanInd){Values = mdata.NumericColumnNames}, new SingleChoiceParam("Std. dev.", rowDevInd){Values = mdata.NumericColumnNames});
			int colMeanInd = 0;
			int colDevInd = 0;
			for (int i = 0; i < mdata.NumericRowCount; i++){
				if (mdata.NumericRowNames[i].ToLower().Equals("mean")){
					colMeanInd = i;
				}
				if (mdata.NumericRowNames[i].ToLower().Equals("std. dev.")){
					colDevInd = i;
				}
			}
			Parameters columnSubParams =
				new Parameters(new SingleChoiceParam("Mean", colMeanInd){Values = mdata.NumericRowNames}, new SingleChoiceParam("Std. dev.", colDevInd){Values = mdata.NumericRowNames});
			return
				new Parameters(new Parameter[]{
					new SingleChoiceWithSubParams("Matrix access"){
						Values = new[]{"Rows", "Columns"},
						ParamNameWidth = 136,
						TotalWidth = 731,
						Help = "Specifies if the z-scoring is performed on the rows or the columns of the matrix.",
						SubParams = new[]{rowSubParams, columnSubParams}
					}
				});
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			ParameterWithSubParams<int> access = param.GetParamWithSubParams<int>("Matrix access");
			bool rows = access.Value == 0;
			int meanInd = access.GetSubParameters().GetParam<int>("Mean").Value;
			int devInd = access.GetSubParameters().GetParam<int>("Std. dev.").Value;
			double[] means = rows ? mdata.NumericColumns[meanInd] : mdata.NumericRows[meanInd];
			double[] stddevs = rows ? mdata.NumericColumns[devInd] : mdata.NumericRows[devInd];
			UnZscore(rows, mdata, processInfo.NumThreads, means, stddevs);
		}

		private static void UnZscore(bool rows, IMatrixData data, int nthreads, double[] means, double[] stddevs){
			if (rows){
				double[] doubles = means;
				double[] stddevs1 = stddevs;
				new ThreadDistributor(nthreads, data.RowCount, i => Calc1(i, data, doubles, stddevs1)).Start();
			} else{
				double[] doubles = means;
				double[] stddevs1 = stddevs;
				new ThreadDistributor(nthreads, data.ColumnCount, j => Calc2(j, data, doubles, stddevs1)).Start();
			}
		}

		private static void Calc1(int i, IMatrixData data, IList<double> means, IList<double> stddevs){
			double[] vals = new double[data.ColumnCount];
			for (int j = 0; j < data.ColumnCount; j++){
				vals[j] = data.Values.Get(i, j);
			}
			double stddev = stddevs[i];
			double mean = means[i];
			for (int j = 0; j < data.ColumnCount; j++){
				data.Values.Set(i, j, (float) ((data.Values.Get(i, j)*stddev) + mean));
			}
		}

		private static void Calc2(int j, IMatrixData data, IList<double> means, IList<double> stddevs){
			double[] vals = new double[data.RowCount];
			for (int i = 0; i < data.RowCount; i++){
				vals[i] = data.Values.Get(i, j);
			}
			double stddev = stddevs[j];
			double mean = means[j];
			for (int i = 0; i < data.RowCount; i++){
				data.Values.Set(i, j, (float) ((data.Values.Get(i, j)*stddev) + mean));
			}
		}
	}
}