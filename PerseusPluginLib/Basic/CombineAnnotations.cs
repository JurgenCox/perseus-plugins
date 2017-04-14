using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Basic{
	public class CombineAnnotations : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Search multiple categorical or string columns for the occurence of a set of terms.";
		public string HelpOutput => "A new categorical column is generated indicating the presence of any of these terms.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Combine annotations";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 3;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:CombineAnnotations";

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string colName = param.GetParam<string>("Name of new column").Value;
			int[] columns = param.GetParam<int[]>("Categories").Value;
			bool inverse = param.GetParam<bool>("Inverse").Value;
			int[] catCols;
			int[] stringCols;
			Split(columns, out catCols, out stringCols, mdata.CategoryColumnCount);
			string[] word1 = param.GetParam<string[]>("Search terms").Value;
			if (word1.Length == 0){
				processInfo.ErrString = "Please specify one or more search terms.";
				return;
			}
			if (string.IsNullOrEmpty(colName)){
				colName = word1[0];
			}
			string[] word = new string[word1.Length];
			for (int i = 0; i < word.Length; i++){
				word[i] = word1[i].ToLower().Trim();
			}
			bool[] indicator = new bool[mdata.RowCount];
			foreach (int col in catCols){
				for (int i = 0; i < mdata.RowCount; i++){
					foreach (string s in mdata.GetCategoryColumnEntryAt(col, i)){
						foreach (string s1 in word){
							if (s.ToLower().Contains(s1)){
								indicator[i] = true;
								break;
							}
						}
					}
				}
			}
			foreach (string[] txt in stringCols.Select(col => mdata.StringColumns[col])){
				for (int i = 0; i < txt.Length; i++){
					string s = txt[i];
					foreach (string s1 in word){
						if (s.ToLower().Contains(s1)){
							indicator[i] = true;
							break;
						}
					}
				}
			}
			string[][] newCol = new string[indicator.Length][];
			for (int i = 0; i < newCol.Length; i++){
				bool yes = inverse ? !indicator[i] : indicator[i];
				newCol[i] = yes ? new[]{"+"} : new string[0];
			}
			mdata.AddCategoryColumn(colName, "", newCol);
		}

		private static void Split(IEnumerable<int> columns, out int[] catCols, out int[] stringCols, int catColCount){
			List<int> catCols1 = new List<int>();
			List<int> stringCols1 = new List<int>();
			foreach (int column in columns){
				if (column < catColCount){
					catCols1.Add(column);
				} else{
					stringCols1.Add(column - catColCount);
				}
			}
			catCols = catCols1.ToArray();
			stringCols = stringCols1.ToArray();
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			string[] choice = ArrayUtils.Concat(mdata.CategoryColumnNames, mdata.StringColumnNames);
			int[] selection = new int[0];
			return
				new Parameters(new MultiChoiceParam("Categories"){
					Value = selection,
					Values = choice,
					Help = "Search these columns for the search terms specified."
				}, new MultiStringParam("Search terms"){Help = "Look for these terms in the selected columns"}, new StringParam("Name of new column"), new BoolParam("Inverse"){Help = "If true, those rows are indicated which do not contain any of the search terms."});
		}
	}
}