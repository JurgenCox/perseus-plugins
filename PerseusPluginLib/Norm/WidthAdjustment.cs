using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	public class WidthAdjustment : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"The first, second and third quartile (q1, q2, q3) are calculated from the " +
				"distribution of all values. The second quartile (which is the median) is subtracted from each value " +
				"to center the distribution. Then we divide by the width in an asymmetric way. All values that are " +
				"positive after subtraction of the median are divided by q3 - q2 while all negative values are divided by q2 - q1.";

		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Width adjustment";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => -7;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:WidthAdjustment";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			double[] dm = new double[mdata.ColumnCount];
			double[] dp = new double[mdata.ColumnCount];
			double[] med = new double[mdata.ColumnCount];
			for (int i = 0; i < mdata.ColumnCount; i++){
				List<float> v = new List<float>();
				foreach (double f in mdata.Values.GetColumn(i)){
					if (!double.IsNaN(f) && !double.IsInfinity(f)){
						v.Add((float) f);
					}
				}
				float[] d = v.ToArray();
				float[] q = ArrayUtils.Quantiles(d, new[]{0.25, 0.5, 0.75});
				for (int j = 0; j < mdata.RowCount; j++){
					mdata.Values.Set(j, i, mdata.Values.Get(j, i) - q[1]);
				}
				dm[i] = q[1] - q[0];
				dp[i] = q[2] - q[1];
				med[i] = q[1];
			}
			double adm = ArrayUtils.Median(dm);
			double adp = ArrayUtils.Median(dp);
			for (int i = 0; i < mdata.ColumnCount; i++){
				for (int j = 0; j < mdata.RowCount; j++){
					double x = mdata.Values.Get(j, i);
					if (x < 0){
						mdata.Values.Set(j, i, (float) (x*adm/dm[i]));
					} else{
						mdata.Values.Set(j, i, (float) (x*adp/dp[i]));
					}
				}
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return new Parameters(new Parameter[0]);
		}
	}
}