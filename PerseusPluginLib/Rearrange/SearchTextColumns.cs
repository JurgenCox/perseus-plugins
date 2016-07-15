using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public class SearchTextColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "A new categorical column is generated representing search results in a text column.";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Search text column";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 23;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:SearchTextColumns";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string word = param.GetParam<string>("Find what").Value;
			int colInd = param.GetParam<int>("Look in").Value;
			bool matchCase = param.GetParam<bool>("Match case").Value;
			bool matchWholeWord = param.GetParam<bool>("Match whole word").Value;
			string scolName = mdata.StringColumnNames[colInd];
			string[] scol = mdata.StringColumns[colInd];
			string[][] catCol = new string[mdata.RowCount][];
			for (int i = 0; i < catCol.Length; i++){
				bool found = Find(scol[i], word, matchCase, matchWholeWord);
				catCol[i] = found ? new[]{"+"} : new string[0];
			}
			mdata.AddCategoryColumn("Search: " + scolName, "Search: " + scolName, catCol);
		}

		private static bool Find(string text, string word, bool matchCase, bool matchWholeWord){
			text = text.Trim();
			if (!matchCase){
				text = text.ToLower();
				word = word.ToLower();
			}
			if (!matchWholeWord){
				return text.Contains(word);
			}
			string[] q = text.Split(';');
			foreach (string t in q){
				string r = t.Trim();
				if (r.Equals(word)){
					return true;
				}
			}
			return false;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new StringParam("Find what"), new SingleChoiceParam("Look in"){Values = mdata.StringColumnNames},
					new BoolParam("Match case"){Value = true}, new BoolParam("Match whole word")
				});
		}
	}
}