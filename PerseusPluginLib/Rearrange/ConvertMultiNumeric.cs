using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public delegate double Conversion(double[] x);

	public class ConvertMultiNumeric : IMatrixProcessing{
		private static readonly string[] names = {"Count", "Sum", "Product", "Average", "Median"};

		private static readonly Conversion[] operations ={
			x => x.Length, x => ArrayUtils.Sum(x), x => ArrayUtils.Product(x),
			x => ArrayUtils.Mean(x), x => ArrayUtils.Median(x)
		};

		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"Creates for the specified multi-numeric columns a numeric column containing the result of the specified operation " +
				"applied to the items in each cell of each selected multi-numeric column.";

		public string Name => "Convert multi-numeric column";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 17;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string HelpOutput => "If n multi-numeric columns are selected, n numeric columns will be added to the matrix.";
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:ConvertMultiNumeric";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param1, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] cols = param1.GetParam<int[]>("Columns").Value;
			int[] ops = param1.GetParam<int[]>("Operation").Value;
			foreach (int t in ops){
				double[][] vals = new double[cols.Length][];
				for (int i = 0; i < cols.Length; i++){
					double[][] x = mdata.MultiNumericColumns[cols[i]];
					vals[i] = new double[x.Length];
					for (int j = 0; j < vals[i].Length; j++){
						vals[i][j] = operations[t](x[j]);
					}
				}
				for (int i = 0; i < cols.Length; i++){
					mdata.AddNumericColumn(mdata.MultiNumericColumnNames[cols[i]] + "_" + names[t], "", vals[i]);
				}
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			IList<string> values = mdata.MultiNumericColumnNames;
			int[] sel = ArrayUtils.ConsecutiveInts(values.Count);
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Operation"){
						Values = names,
						Help = "How should the numbers in a cell of the multi-numeric columns be transformed to a single number?"
					},
					new MultiChoiceParam("Columns"){
						Values = values,
						Value = sel,
						Help = "Select here the multi-numeric colums that should be converted to numeric columns."
					}
				});
		}
	}
}