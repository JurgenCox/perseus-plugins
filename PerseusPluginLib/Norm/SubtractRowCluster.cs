using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	public class SubtractRowCluster : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Subtract the average pattern of the selected rows from all rows.";
		public string HelpOutput => "Normalized matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Subtract row cluster";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:SubtractRowCluster";
		public int GetMaxThreads(Parameters parameters) { return 1; }

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string[][] col = mdata.GetCategoryColumnAt(param.GetParam<int>("Indicator column").Value);
			string term = param.GetParam<string>("Value").Value;
			List<int> inds = new List<int>();
			for (int i = 0; i < col.Length; i++){
				if (Contains(col[i], term)){
					inds.Add(i);
				}
			}
			double[][] profiles = new double[inds.Count][];
			for (int i = 0; i < profiles.Length; i++){
				profiles[i] = ArrayUtils.ToDoubles(mdata.Values.GetRow(inds[i]));
				double mean = profiles[i].Mean();
				for (int j = 0; j < profiles[i].Length; j++){
					profiles[i][j] -= mean;
				}
			}
			double[] totalProfile = new double[mdata.ColumnCount];
			for (int i = 0; i < totalProfile.Length; i++){
				List<double> vals = new List<double>();
				foreach (double[] t in profiles){
					double val = t[i];
					if (double.IsNaN(val) || double.IsInfinity(val)){
						continue;
					}
					vals.Add(val);
				}
				totalProfile[i] = vals.Count > 0 ? ArrayUtils.Median(vals) : double.NaN;
			}
			for (int i = 0; i < mdata.RowCount; i++){
				for (int j = 0; j < mdata.ColumnCount; j++){
					mdata.Values.Set(i, j, mdata.Values.Get(i, j)- totalProfile[j]);
				}
			}
		}

		private static bool Contains(IEnumerable<string> strings, string term){
			foreach (string s in strings){
				if (s.Equals(term)){
					return true;
				}
			}
			return false;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new SingleChoiceParam("Indicator column"){Values = mdata.CategoryColumnNames}, new StringParam("Value", "+"){
					Help = "Rows matching this term in the indicator column will be used as control for the normalization."
				});
		}
	}
}