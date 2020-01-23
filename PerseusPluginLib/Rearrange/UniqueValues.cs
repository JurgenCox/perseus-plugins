using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Rearrange{
	public class UniqueValues : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"Values within each row in the selected text columns are made unique by removing duplicates. The entries are " +
				"interpreted as separated by semicolons.";

		public string Name => "Unique values";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 16;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string HelpOutput => "";
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:UniqueValues";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param1, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] stringCols = param1.GetParam<int[]>("Text columns").Value;
			if (stringCols.Length == 0){
				processInfo.ErrString = "Please select some columns.";
				return;
			}
		    mdata.UniqueValues(stringCols);
		}

	    public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Text columns"){
						Values = mdata.StringColumnNames,
						Value = new int[0],
						Help = "Select here the text columns that should be expanded."
					}
				});
		}
	}
}