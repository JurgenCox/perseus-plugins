using System.Collections.Generic;
using BaseLib.Graphic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Properties;

namespace PerseusPluginLib.Annot{
	public class AddAnnotationToMatrix : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => GraphUtils.ToBitmap2(Resources.network);
		public string Description
			=>
				"Based on a column containing protein (or gene or transcript) identifies this activity adds columns with " +
				"annotations. These are read from specificially formatted files contained in the folder '\\conf\\annotations' in " +
				"your Perseus installation. Species-specific annotation files generated from UniProt can be downloaded from " +
				"the link specified in the menu at the blue box in the upper left corner.";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Add annotation";
		public string Heading => "Annot. columns";
		public bool IsActive => true;
		public float DisplayRank => -20;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotcolumns:AddAnnotationToMatrix";
		public int GetMaxThreads(Parameters parameters){
			return 1;
		}
		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> colChoice = mdata.StringColumnNames;
			string[] baseNames;
			string[] files;
			string[][] annots = PerseusUtils.GetAvailableAnnots(out baseNames, out files);
			int selFile = 0;
			bool isMainAnnot = false;
			for (int i = 0; i < files.Length; i++){
				if (files[i].ToLower().Contains("perseusannot")){
					selFile = i;
					isMainAnnot = true;
					break;
				}
			}
			Parameters[] subParams = new Parameters[files.Length];
			for (int i = 0; i < subParams.Length; i++){
				int colInd = 0;
				if (isMainAnnot && i == selFile){
					for (int j = 0; j < colChoice.Count; j++){
						if (colChoice[j].ToUpper().Contains("PROTEIN IDS")){
							colInd = j;
							break;
						}
					}
					for (int j = 0; j < colChoice.Count; j++){
						if (colChoice[j].ToUpper().Contains("MAJORITY PROTEIN IDS")){
							colInd = j;
							break;
						}
					}
				} else{
					for (int j = 0; j < colChoice.Count; j++){
						if (colChoice[j].ToUpper().Contains(baseNames[i].ToUpper())){
							colInd = j;
							break;
						}
					}
				}
				subParams[i] =
					new Parameters(new Parameter[]{
						new SingleChoiceParam(baseNames[i] + " column"){
							Values = colChoice,
							Value = colInd,
							Help =
								"Specify here the column that contains the base identifiers which are going to be " +
								"matched to the annotation."
						},
						new MultiChoiceParam("Annotations to be added"){Values = annots[i]}
					});
			}
			return
				new Parameters(new Parameter[]{
					new SingleChoiceWithSubParams("Source", selFile){
						Values = files,
						SubParams = subParams,
						ParamNameWidth = 136,
						TotalWidth = 735
					},
					new MultiChoiceParam("Additional sources"){Values = files}
				});
		}
		private static string[] GetBaseIds(Parameters para, IDataWithAnnotationColumns mdata){
			string[] baseNames;
			AnnotType[][] types;
			string[] files;
			PerseusUtils.GetAvailableAnnots(out baseNames, out types, out files);
			ParameterWithSubParams<int> spd = para.GetParamWithSubParams<int>("Source");
			int ind = spd.Value;
			Parameters param = spd.GetSubParameters();
			int baseCol = param.GetParam<int>(baseNames[ind] + " column").Value;
			string[] baseIds = mdata.StringColumns[baseCol];
			return baseIds;
		}
		public void ProcessData(IMatrixData mdata, Parameters para, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string[] baseIds = GetBaseIds(para, mdata);
			string[] name;
			int[] catColInds;
			int[] textColInds;
			int[] numColInds;
			string[][][] catCols;
			string[][] textCols;
			double[][] numCols;
			bool success = PerseusUtils.ProcessDataAddAnnotation(mdata.RowCount, para, baseIds, processInfo, out name,
				out catColInds, out textColInds, out numColInds, out catCols, out textCols, out numCols);
			if (!success){
				return;
			}
			for (int i = 0; i < catCols.Length; i++){
				mdata.AddCategoryColumn(name[catColInds[i]], "", catCols[i]);
			}
			for (int i = 0; i < textCols.Length; i++){
				mdata.AddStringColumn(name[textColInds[i]], "", textCols[i]);
			}
			for (int i = 0; i < numCols.Length; i++){
				mdata.AddNumericColumn(name[numColInds[i]], "", numCols[i]);
			}
		}
	}
}