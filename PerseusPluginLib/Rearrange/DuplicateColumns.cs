using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;

namespace PerseusPluginLib.Rearrange{
	public class DuplicateColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string HelpOutput => "Same matrix but with duplicated columns added.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Heading => "Rearrange";
		public string Name => "Duplicate columns";
		public bool IsActive => true;
		public float DisplayRank => 3;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Description => "Columns of all types can be duplicated.";

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:DuplicateColumns";

		public void ProcessData(IMatrixData data, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] exColInds = param.GetParam<int[]>("Main columns").Value;
			int[] numColInds = param.GetParam<int[]>("Numerical columns").Value;
			int[] multiNumColInds = param.GetParam<int[]>("Multi-numerical columns").Value;
			int[] catColInds = param.GetParam<int[]>("Categorical columns").Value;
			int[] textColInds = param.GetParam<int[]>("Text columns").Value;
			if (exColInds.Length > 0){
				int ncol = data.ColumnCount;
				data.ExtractColumns(ArrayUtils.Concat(ArrayUtils.ConsecutiveInts(data.ColumnCount), exColInds));
				HashSet<string> taken = new HashSet<string>(data.ColumnNames);
				for (int i = 0; i < exColInds.Length; i++){
					string s = PerseusUtils.GetNextAvailableName(data.ColumnNames[ncol + i], taken);
					data.ColumnNames[ncol + i] = s;
					taken.Add(s);
				}
			}
			foreach (int ind in numColInds){
				HashSet<string> taken = new HashSet<string>(data.NumericColumnNames);
				string s = PerseusUtils.GetNextAvailableName(data.NumericColumnNames[ind], taken);
				data.AddNumericColumn(s, data.NumericColumnDescriptions[ind], (double[]) data.NumericColumns[ind].Clone());
				taken.Add(s);
			}
			foreach (int ind in multiNumColInds){
				HashSet<string> taken = new HashSet<string>(data.MultiNumericColumnNames);
				string s = PerseusUtils.GetNextAvailableName(data.MultiNumericColumnNames[ind], taken);
				data.AddMultiNumericColumn(s, data.MultiNumericColumnDescriptions[ind],
					(double[][]) data.MultiNumericColumns[ind].Clone());
				taken.Add(s);
			}
			foreach (int ind in catColInds){
				HashSet<string> taken = new HashSet<string>(data.CategoryColumnNames);
				string s = PerseusUtils.GetNextAvailableName(data.CategoryColumnNames[ind], taken);
				data.AddCategoryColumn(s, data.CategoryColumnDescriptions[ind], data.GetCategoryColumnAt(ind));
				taken.Add(s);
			}
			foreach (int ind in textColInds){
				HashSet<string> taken = new HashSet<string>(data.StringColumnNames);
				string s = PerseusUtils.GetNextAvailableName(data.StringColumnNames[ind], taken);
				data.AddStringColumn(s, data.StringColumnDescriptions[ind], (string[]) data.StringColumns[ind].Clone());
				taken.Add(s);
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> exCols = mdata.ColumnNames;
			List<string> numCols = mdata.NumericColumnNames;
			List<string> multiNumCols = mdata.MultiNumericColumnNames;
			List<string> catCols = mdata.CategoryColumnNames;
			List<string> textCols = mdata.StringColumnNames;
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Main columns"){
						Value = new int[0],
						Values = exCols,
						Help = "Specify here the main columns that should be duplicated."
					},
					new MultiChoiceParam("Numerical columns"){
						Value = new int[0],
						Values = numCols,
						Help = "Specify here the numerical columns that should be duplicated."
					},
					new MultiChoiceParam("Multi-numerical columns"){
						Value = new int[0],
						Values = multiNumCols,
						Help = "Specify here the multi-numerical columns that should be duplicated."
					},
					new MultiChoiceParam("Categorical columns"){
						Value = new int[0],
						Values = catCols,
						Help = "Specify here the categorical columns that should be duplicated."
					},
					new MultiChoiceParam("Text columns"){
						Value = new int[0],
						Values = textCols,
						Help = "Specify here the text columns that should be duplicated."
					}
				});
		}
	}
}