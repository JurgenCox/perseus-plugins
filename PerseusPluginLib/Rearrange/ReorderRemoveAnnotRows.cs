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
			data.NumericRows = ArrayUtils.SubList(data.NumericRows, numColInds);
			data.NumericRowNames = ArrayUtils.SubList(data.NumericRowNames, numColInds);
			data.NumericRowDescriptions = ArrayUtils.SubList(data.NumericRowDescriptions, numColInds);
			data.MultiNumericRows = ArrayUtils.SubList(data.MultiNumericRows, multiNumColInds);
			data.MultiNumericRowNames = ArrayUtils.SubList(data.MultiNumericRowNames, multiNumColInds);
			data.MultiNumericRowDescriptions = ArrayUtils.SubList(data.MultiNumericRowDescriptions, multiNumColInds);
			data.CategoryRows = PerseusPluginUtils.GetCategoryRows(data, catColInds);
			data.CategoryRowNames = ArrayUtils.SubList(data.CategoryRowNames, catColInds);
			data.CategoryRowDescriptions = ArrayUtils.SubList(data.CategoryRowDescriptions, catColInds);
			data.StringRows = ArrayUtils.SubList(data.StringRows, textColInds);
			data.StringRowNames = ArrayUtils.SubList(data.StringRowNames, textColInds);
			data.StringRowDescriptions = ArrayUtils.SubList(data.StringRowDescriptions, textColInds);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> numRows = mdata.NumericRowNames;
			List<string> multiNumRows = mdata.MultiNumericRowNames;
			List<string> catRows = mdata.CategoryRowNames;
			List<string> textRows = mdata.StringRowNames;
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Numerical rows"){
						Value = ArrayUtils.ConsecutiveInts(numRows.Count),
						Values = numRows,
						Help = "Specify here the new order in which the numerical rows should appear."
					},
					new MultiChoiceParam("Multi-numerical rows"){
						Value = ArrayUtils.ConsecutiveInts(multiNumRows.Count),
						Values = multiNumRows,
						Help = "Specify here the new order in which the numerical rows should appear."
					},
					new MultiChoiceParam("Categorical rows"){
						Value = ArrayUtils.ConsecutiveInts(catRows.Count),
						Values = catRows,
						Help = "Specify here the new order in which the categorical rows should appear."
					},
					new MultiChoiceParam("Text rows"){
						Value = ArrayUtils.ConsecutiveInts(textRows.Count),
						Values = textRows,
						Help = "Specify here the new order in which the text rows should appear."
					}
				});
		}
	}
}