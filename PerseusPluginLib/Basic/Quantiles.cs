using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Basic{
	public class Quantiles : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("quantiles.png");
		public string Name => "Quantiles";
		public string Heading => "Basic";
		public bool IsActive => true;
		public float DisplayRank => -4;

		public string HelpOutput
			=> "For each selected expression coulumn a categorical column is added containing the quantile information.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Basic:Quantiles";

		public string Description
			=>
				"Expression columns are transformed into quantiles. These can then, for instance, be used in a subsequent enrichment analysis."
			;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int numQuantiles = param.GetParam<int>("Number of quantiles").Value;
			int[] colInds = param.GetParam<int[]>("Columns").Value;
			foreach (int colInd in colInds){
				double[] vals = GetValues(mdata, colInd);
				List<int> v = new List<int>();
				for (int i = 0; i < vals.Length; i++){
					if (!double.IsNaN(vals[i])){
						v.Add(i);
					}
				}
				int[] o = v.ToArray();
				vals = ArrayUtils.SubArray(vals, o);
				int[] q = ArrayUtils.Order(vals);
				o = ArrayUtils.SubArray(o, q);
				string[][] catCol = new string[mdata.RowCount][];
				for (int i = 0; i < catCol.Length; i++){
					catCol[i] = new[]{"missing"};
				}
				for (int i = 0; i < o.Length; i++){
					int catVal = i*numQuantiles/o.Length + 1;
					catCol[o[i]] = new[]{"Q" + catVal};
				}
				string name = GetName(mdata, colInd);
				string nameq = name + "_q";
				string desc = "The column " + name + " has been divided into " + numQuantiles + " quantiles.";
				mdata.AddCategoryColumn(nameq, desc, catCol);
			}
		}

		private static string GetName(IMatrixData mdata, int colInd){
			if (colInd < mdata.ColumnCount){
				return mdata.ColumnNames[colInd];
			}
			colInd -= mdata.ColumnCount;
			return mdata.NumericColumnNames[colInd];
		}

		private static double[] GetValues(IMatrixData mdata, int colInd){
			if (colInd < mdata.ColumnCount){
				return ArrayUtils.ToDoubles(mdata.Values.GetColumn(colInd));
			}
			colInd -= mdata.ColumnCount;
			return mdata.NumericColumns[colInd];
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			string[] values = ArrayUtils.Concat(mdata.ColumnNames, mdata.NumericColumnNames);
			return
				new Parameters(new IntParam("Number of quantiles", 5){
					Help = "This defines the number of quantiles that each column is going to be divided into."
				}, new MultiChoiceParam("Columns"){
					Value = ArrayUtils.ConsecutiveInts(mdata.ColumnCount),
					Values = values,
					Help = "Please select here the columns that should be transformed into quantiles."
				});
		}
	}
}