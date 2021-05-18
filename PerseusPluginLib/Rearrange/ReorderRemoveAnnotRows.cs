using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Rearrange{
	public class ReorderRemoveAnnotRows : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string HelpOutput => "Same matrix but with annotation rows removed or in new order.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Heading => "Rearrange";
		public string Name => "Reorder/remove annotation rows";
		public bool IsActive => true;
		public float DisplayRank => 2.9f;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:ReorderRemoveAnnotRows";

		public string Description => "Annotation rows can be removed with this activity.";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData data, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] numColInds = param.GetParam<int[]>("Numerical rows").Value;
			int[] multiNumColInds = param.GetParam<int[]>("Multi-numerical rows").Value;
			int[] catColInds = param.GetParam<int[]>("Categorical rows").Value;
			int[] textColInds = param.GetParam<int[]>("Text rows").Value;
			data.NumericRows = data.NumericRows.SubList(numColInds);
			data.NumericRowNames = data.NumericRowNames.SubList(numColInds);
			data.NumericRowDescriptions = data.NumericRowDescriptions.SubList(numColInds);
			data.MultiNumericRows = data.MultiNumericRows.SubList(multiNumColInds);
			data.MultiNumericRowNames = data.MultiNumericRowNames.SubList(multiNumColInds);
			data.MultiNumericRowDescriptions = data.MultiNumericRowDescriptions.SubList(multiNumColInds);
			data.CategoryRows = PerseusPluginUtils.GetCategoryRows(data, catColInds);
			data.CategoryRowNames = data.CategoryRowNames.SubList(catColInds);
			data.CategoryRowDescriptions = data.CategoryRowDescriptions.SubList(catColInds);
			data.StringRows = data.StringRows.SubList(textColInds);
			data.StringRowNames = data.StringRowNames.SubList(textColInds);
			data.StringRowDescriptions = data.StringRowDescriptions.SubList(textColInds);
           
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> numRows = mdata.NumericRowNames;
			List<string> multiNumRows = mdata.MultiNumericRowNames;
			List<string> catRows = mdata.CategoryRowNames;
			List<string> textRows = mdata.StringRowNames;
			return
				new Parameters(new MultiChoiceParam("Numerical rows"){
					Value = ArrayUtils.ConsecutiveInts(numRows.Count),
					Values = numRows,
					Help = "Specify here the new order in which the numerical rows should appear."
				}, new MultiChoiceParam("Multi-numerical rows"){
					Value = ArrayUtils.ConsecutiveInts(multiNumRows.Count),
					Values = multiNumRows,
					Help = "Specify here the new order in which the numerical rows should appear."
				}, new MultiChoiceParam("Categorical rows"){
					Value = ArrayUtils.ConsecutiveInts(catRows.Count),
					Values = catRows,
					Help = "Specify here the new order in which the categorical rows should appear."
				}, new MultiChoiceParam("Text rows"){
					Value = ArrayUtils.ConsecutiveInts(textRows.Count),
					Values = textRows,
					Help = "Specify here the new order in which the text rows should appear."
				});
		}
	}
}