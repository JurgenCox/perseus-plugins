using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.AnnotRows{
	public class JoinTermsInCategoricalRow : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "The selected terms in the categorical row will be joined to one term.";
		public string HelpOutput => "The filtered matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Join terms in categorical row";
		public string Heading => "Annot. rows";
		public bool IsActive => true;
		public float DisplayRank => 20;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotrows:JoinTermsInCategoricalRow";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			Parameters[] subParams = new Parameters[mdata.CategoryRowCount];
			for (int i = 0; i < mdata.CategoryRowCount; i++){
				string[] values = mdata.GetCategoryRowValuesAt(i);
				int[] sel = values.Length == 1 ? new[]{0} : new int[0];
				subParams[i] =
					new Parameters(new Parameter[]{
						new MultiChoiceParam("Values", sel){
							Values = values,
							Help = "The value that should be present to discard/keep the corresponding row."
						}
					});
			}
			return
				new Parameters(new SingleChoiceWithSubParams("Row"){
					Values = mdata.CategoryRowNames,
					SubParams = subParams,
					Help = "The categorical row that the filtering should be based on.",
					ParamNameWidth = 50,
					TotalWidth = 731
				}, new StringParam("New term"));
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			ParameterWithSubParams<int> p = param.GetParamWithSubParams<int>("Row");
			int colInd = p.Value;
			if (colInd < 0){
				processInfo.ErrString = "No categorical rows available.";
				return;
			}
			Parameter<int[]> mcp = p.GetSubParameters().GetParam<int[]>("Values");
			int[] inds = mcp.Value;
			if (inds.Length < 1){
				processInfo.ErrString = "Please select at least two terms for merging.";
				return;
			}
			string newTerm = param.GetParam<string>("New term").Value;
			if (newTerm.Length == 0){
				processInfo.ErrString = "Please specify a new term.";
				return;
			}
			string[] values = new string[inds.Length];
			for (int i = 0; i < values.Length; i++){
				values[i] = mdata.GetCategoryRowValuesAt(colInd)[inds[i]];
			}
			HashSet<string> value = new HashSet<string>(values);
			string[][] cats = mdata.GetCategoryRowAt(colInd);
			string[][] newCat = new string[cats.Length][];
			for (int i = 0; i < cats.Length; i++){
				string[] w = cats[i];
				bool changed = false;
				for (int j = 0; j < w.Length; j++){
					if (value.Contains(w[j])){
						w[j] = newTerm;
						changed = true;
					}
				}
				if (changed){
					Array.Sort(w);
				}
				newCat[i] = w;
			}
			mdata.SetCategoryRowAt(newCat, colInd);
		}
	}
}