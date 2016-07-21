using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Basic{
	public class DeHyphenateIds : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "De-hyphenate ids";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 10;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:DeHyphenateIds";
		public string Description => "Identifiers will be truncated at the occurence of hyphens.";
		public string HelpOutput => "";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int colInd = param.GetParam<int>("Id column").Value;
			if (colInd < 0){
				processInfo.ErrString = "Please specify an id column.";
				return;
			}
			string[] x = mdata.StringColumns[colInd];
			for (int i = 0; i < x.Length; i++){
				string s = x[i];
				if (string.IsNullOrEmpty(s)){
					continue;
				}
				string[] q = s.Split(';');
				string[] r = new string[q.Length];
				for (int j = 0; j < r.Length; j++){
					r[j] = Cut(q[j]);
				}
				x[i] = StringUtils.Concat(";", ArrayUtils.UniqueValues(r));
			}
		}

		private static string Cut(string s){
			int i = s.IndexOf('-');
			return i < 0 ? s : s.Substring(0, i);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return new Parameters(new Parameter[]{new SingleChoiceParam("Id column"){Values = mdata.StringColumnNames}});
		}
	}
}