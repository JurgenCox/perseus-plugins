using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Basic{
	public class AddNoise : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Modulate the data with Gaussian noise.";
		public string HelpOutput => "Same as input matrix with random noise added to the expression columns.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Add noise";
		public string Heading => "Basic";
		public bool IsActive => true;
		public float DisplayRank => 200;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:AddNoise";

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			Random2 rand = new Random2();
			double std = param.GetParam<double>("Standard deviation").Value;
			int[] inds = param.GetParam<int[]>("Columns").Value;
			List<int> mainInds = new List<int>();
			List<int> numInds = new List<int>();
			foreach (int ind in inds){
				if (ind < mdata.ColumnCount){
					mainInds.Add(ind);
				} else{
					numInds.Add(ind - mdata.ColumnCount);
				}
			}
			foreach (int j in mainInds){
				for (int i = 0; i < mdata.RowCount; i++){
					mdata.Values.Set(i, j, mdata.Values.Get(i, j)+(float) rand.NextGaussian(0, std));
				}
			}
			foreach (int j in numInds){
				for (int i = 0; i < mdata.RowCount; i++){
					mdata.NumericColumns[j][i] += (float) rand.NextGaussian(0, std);
				}
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new DoubleParam("Standard deviation", 0.1){Help = "Standard deviation of the noise distribution."},
					new MultiChoiceParam("Columns", ArrayUtils.ConsecutiveInts(mdata.ColumnCount)){
						Values = ArrayUtils.Concat(mdata.ColumnNames, mdata.NumericColumnNames)
					}
				});
		}
	}
}