using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Manual{
	public class SelectRowsManually : IMatrixAnalysis{
		public string Description
			=>
				"Rows can be selected interactively and a new matrix can be produced which contains only the selected rows or only the unselected rows."
			;

		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("hand.png");
		public string Heading => null;
		public string Name => "Select rows manually";
		public bool IsActive => true;
		public float DisplayRank => 2;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixAnalysis:Misc:SelectRowsManually";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public IAnalysisResult AnalyzeData(IMatrixData mdata, Parameters param, ProcessInfo processInfo){
			return new SelectRowsManuallyResult(mdata);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return new Parameters();
		}
	}
}