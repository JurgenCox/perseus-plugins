using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse.Misc;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Mods{
	public class AddModificationCounts : IMatrixProcessing{
		public bool HasButton => false;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Modifications:AddModificationCounts";

		public Bitmap2 DisplayImage => null;
		public string Description => "Count how many modifcations are known in PSP for the specified modification type(s).";

		public string HelpOutput
			=> "A numerical column is added containing the number of modifications for the given protein.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Add modification counts";
		public string Heading => "Modifications";
		public bool IsActive => true;
		public float DisplayRank => 5;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string[] mods = param.GetParam<int[]>("Modifications").StringValue.Split(new[]{';'},
				StringSplitOptions.RemoveEmptyEntries);
			string[] up = mdata.StringColumns[param.GetParam<int>("Uniprot column").Value];
			string[][] uprot = new string[up.Length][];
			for (int i = 0; i < up.Length; i++){
				uprot[i] = up[i].Length > 0 ? up[i].Split(';') : new string[0];
			}
			double[][] c = new double[mods.Length][];
			for (int index = 0; index < mods.Length; index++){
				string mod = mods[index];
				string filename = PhosphoSitePlusParser.GetFilenameForMod(mod);
				if (filename == null){
					processInfo.ErrString = "File does not exist.";
					return;
				}
				string[] seqWins;
				string[] accs;
				string[] pubmedLtp;
				string[] pubmedMs2;
				string[] cstMs2;
				string[] species;
				PhosphoSitePlusParser.ParseKnownMods(filename, out seqWins, out accs, out pubmedLtp, out pubmedMs2, out cstMs2, out species);
				for (int i = 0; i < seqWins.Length; i++){
					seqWins[i] = seqWins[i].ToUpper();
				}
				Dictionary<string, HashSet<string>> counts = new Dictionary<string, HashSet<string>>();
				for (int i = 0; i < accs.Length; i++){
					string acc = accs[i];
					if (!counts.ContainsKey(acc)){
						counts.Add(acc, new HashSet<string>());
					}
					counts[acc].Add(seqWins[i]);
				}
				c[index] = new double[up.Length];
				for (int i = 0; i < up.Length; i++){
					c[index][i] = CountSites(uprot[i], counts);
				}
			}
			string[][] catCol = new string[up.Length][];
			for (int i = 0; i < catCol.Length; i++){
				List<string> x = new List<string>();
				for (int j = 0; j < mods.Length; j++){
					if (c[j][i] > 0){
						x.Add(mods[j]);
					}
				}
				x.Sort();
				catCol[i] = x.ToArray();
			}
			mdata.AddCategoryColumn("Known modifications", "Known modifications", catCol);
			for (int i = 0; i < mods.Length; i++){
				mdata.AddNumericColumn(mods[i] + " count", mods[i] + " count", c[i]);
			}
		}

		private static int CountSites(IEnumerable<string> ups, Dictionary<string, HashSet<string>> counts){
			List<int> vals = new List<int>();
			foreach (string up in ups){
				if (counts.ContainsKey(up)){
					vals.Add(counts[up].Count);
				} else{
					string up1 = DeHyphenate(up);
					if (counts.ContainsKey(up1)){
						vals.Add(counts[up1].Count);
					}
				}
			}
			return vals.Count == 0 ? 0 : ArrayUtils.Max(vals);
		}

		private static string DeHyphenate(string s){
			return !s.Contains("-") ? s : s.Substring(0, s.IndexOf('-'));
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
			string[] choice = PhosphoSitePlusParser.GetAllMods();
			return
				new Parameters(
					new MultiChoiceParam("Modifications"){Value = ArrayUtils.ConsecutiveInts(choice.Length), Values = choice},
					new SingleChoiceParam("Uniprot column"){
						Value = colInd,
						Help = "Specify here the column that contains Uniprot identifiers.",
						Values = colChoice
					});
		}
	}
}