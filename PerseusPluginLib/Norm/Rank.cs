using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	internal class Rank : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "The values in each row/column are replaced by ranks.";
		public string HelpOutput => "Normalized matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Rank";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => -9;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:Rank";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			Parameter<int> access = param.GetParam<int>("Matrix access");
			bool rows = access.Value == 0;
			Rank1(rows, mdata);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Matrix access"){
						Values = new[]{"Rows", "Columns"},
						Help = "Specifies if the analysis is performed on the rows or the columns of the matrix."
					}
				});
		}

		public static void Rank1(bool rows, IMatrixData data){
			if (rows){
				for (int i = 0; i < data.RowCount; i++){
					List<double> vals = new List<double>();
					List<int> indices = new List<int>();
					for (int j = 0; j < data.ColumnCount; j++){
						double q = data.Values.Get(i, j);
						if (!double.IsNaN(q)){
							vals.Add(q);
							indices.Add(j);
						}
					}
					double[] ranks = ArrayUtils.Rank(vals);
					for (int j = 0; j < data.ColumnCount; j++){
						data.Values.Set(i, j, double.NaN);
					}
					for (int j = 0; j < ranks.Length; j++){
						data.Values.Set(i, indices[j], ranks[j]);
					}
				}
			} else{
				for (int j = 0; j < data.ColumnCount; j++){
					List<double> vals = new List<double>();
					List<int> indices = new List<int>();
					for (int i = 0; i < data.RowCount; i++){
						double q = data.Values.Get(i, j);
						if (!double.IsNaN(q)){
							vals.Add(q);
							indices.Add(i);
						}
					}
					double[] ranks = ArrayUtils.Rank(vals);
					for (int i = 0; i < data.RowCount; i++){
						data.Values.Set(i, j, double.NaN);
					}
					for (int i = 0; i < ranks.Length; i++){
						data.Values.Set(indices[i], j, ranks[i]);
					}
				}
			}
		}
	}
}