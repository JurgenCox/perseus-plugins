using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse.Misc;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Mods{
	public class AddRegulatorySites : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=> "PSP information on regulatory sites is added based on UniProt identifiers and sequence windows.";

		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Add regulatory sites";
		public string Heading => "Modifications";
		public bool IsActive => true;
		public float DisplayRank => 15;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Modifications:AddRegulatorySites";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> colChoice = mdata.StringColumnNames;
			int colInd = 0;
			for (int i = 0; i < colChoice.Count; i++){
				if (colChoice[i].ToUpper().Equals("UNIPROT")){
					colInd = i;
					break;
				}
			}
			int colSeqInd = 0;
			for (int i = 0; i < colChoice.Count; i++){
				if (colChoice[i].ToUpper().Equals("SEQUENCE WINDOW")){
					colSeqInd = i;
					break;
				}
			}
			return
				new Parameters(
					new SingleChoiceParam("Uniprot column"){
						Value = colInd,
						Help = "Specify here the column that contains Uniprot identifiers.",
						Values = colChoice
					},
					new SingleChoiceParam("Sequence window"){
						Value = colSeqInd,
						Help = "Specify here the column that contains the sequence windows around the site.",
						Values = colChoice
					});
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			PhosphoSitePlusParser.ParseRegulatorySites(out string[] seqWins, out string[] accs, out string[] function, out string[] process, out string[] protInteract,
				out string[] otherInteract, out string[] notes, out string[] species);
			if (seqWins == null){
				processInfo.ErrString = "File  does not exist.";
				return;
			}
			string[] up = mdata.StringColumns[param.GetParam<int>("Uniprot column").Value];
			string[][] uprot = new string[up.Length][];
			for (int i = 0; i < up.Length; i++){
				uprot[i] = up[i].Length > 0 ? up[i].Split(';') : new string[0];
			}
			string[] win = mdata.StringColumns[param.GetParam<int>("Sequence window").Value];
			Dictionary<string, List<int>> map = new Dictionary<string, List<int>>();
			for (int i = 0; i < seqWins.Length; i++){
				string acc = accs[i];
				if (!map.ContainsKey(acc)){
					map.Add(acc, new List<int>());
				}
				map[acc].Add(i);
			}
			string[][] newCatCol = new string[uprot.Length][];
			string[][] function2 = new string[uprot.Length][];
			string[][] process2 = new string[uprot.Length][];
			string[][] protInteract2 = new string[uprot.Length][];
			string[][] otherInteract2 = new string[uprot.Length][];
			string[][] notes2 = new string[uprot.Length][];
			for (int i = 0; i < uprot.Length; i++){
				string[] win1 = TransformIl(win[i]).Split(';');
				HashSet<string> wins = new HashSet<string>();
				HashSet<string> function1 = new HashSet<string>();
				HashSet<string> process1 = new HashSet<string>();
				HashSet<string> protInteract1 = new HashSet<string>();
				HashSet<string> otherInteract1 = new HashSet<string>();
				HashSet<string> notes1 = new HashSet<string>();
				foreach (string ux in uprot[i]){
					if (map.ContainsKey(ux)){
						List<int> n = map[ux];
						foreach (int ind in n){
							string s = seqWins[ind];
							if (Contains(win1, TransformIl(s.ToUpper().Substring(1, s.Length - 2)))){
								wins.Add(s);
								if (function[ind].Length > 0){
									function1.Add(function[ind]);
								}
								if (process[ind].Length > 0){
									process1.Add(process[ind]);
								}
								if (protInteract[ind].Length > 0){
									protInteract1.Add(protInteract[ind]);
								}
								if (otherInteract[ind].Length > 0){
									otherInteract1.Add(otherInteract[ind]);
								}
								if (notes[ind].Length > 0){
									notes1.Add(notes[ind]);
								}
							}
						}
					}
				}
				if (wins.Count > 0){
					newCatCol[i] = new[]{"+"};
					function2[i] = function1.ToArray();
					process2[i] = process1.ToArray();
					protInteract2[i] = protInteract1.ToArray();
					otherInteract2[i] = otherInteract1.ToArray();
					notes2[i] = notes1.ToArray();
				} else{
					newCatCol[i] = new string[0];
					function2[i] = new string[0];
					process2[i] = new string[0];
					protInteract2[i] = new string[0];
					otherInteract2[i] = new string[0];
					notes2[i] = new string[0];
				}
			}
			mdata.AddCategoryColumn("Regulatory site", "", newCatCol);
			mdata.AddCategoryColumn("Regulatory site function", "", function2);
			mdata.AddCategoryColumn("Regulatory site process", "", process2);
			mdata.AddCategoryColumn("Regulatory site protInteract", "", protInteract2);
			mdata.AddCategoryColumn("Regulatory site otherInteract", "", otherInteract2);
			mdata.AddCategoryColumn("Regulatory site notes", "", notes2);
		}

		public static bool Contains(IEnumerable<string> wins, string x){
			foreach (string win in wins){
				if (Contains(win, x)){
					return true;
				}
			}
			return false;
		}

		private static bool Contains(string wins, string s){
			if (wins.Length == s.Length){
				return wins.Equals(s);
			}
			return wins.Length > s.Length ? CenterEquals(wins, s) : CenterEquals(s, wins);
		}

		private static bool CenterEquals(string wins, string s){
			int offset = wins.Length/2 - s.Length/2;
			return wins.Substring(offset, wins.Length - 2*offset).Equals(s);
		}

		public static string TransformIl(string p0){
			List<char> result = new List<char>();
			foreach (char c in p0){
				result.Add(c == 'L' ? 'I' : c);
			}
			return new string(result.ToArray());
		}
	}
}