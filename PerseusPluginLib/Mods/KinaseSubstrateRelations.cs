using System;
using System.Collections.Generic;
using System.IO;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Mods{
	public class KinaseSubstrateRelations : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description
			=>
				"Kinase-substrate relations are read from PSP files and attached as annotation based on " +
				"UniProt identifiers and sequence windows.";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Kinase-substrate relations";
		public string Heading => "Modifications";
		public bool IsActive => true;
		public float DisplayRank => 6;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Modifications:KinaseSubstrateRelations";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string folder = FileUtils.GetConfigPath() + "\\PSP\\";
			string file = folder + "Kinase_Substrate_Dataset";
			if (!File.Exists(file)){
				if (File.Exists(file + ".gz")){
					file = file + ".gz";
				} else{
					processInfo.ErrString = "File " + file + " does not exist.";
					return;
				}
			}
			string[] seqWins = TabSep.GetColumn("SITE_+/-7_AA", file, 3, '\t');
			string[] subAccs = TabSep.GetColumn("SUB_ACC_ID", file, 3, '\t');
			string[] kinases = TabSep.GetColumn("KINASE", file, 3, '\t');
			string[] kinAccs = TabSep.GetColumn("KIN_ACC_ID", file, 3, '\t');
			string[] up = mdata.StringColumns[param.GetParam<int>("Uniprot column").Value];
			string[][] uprot = new string[up.Length][];
			for (int i = 0; i < up.Length; i++){
				uprot[i] = up[i].Length > 0 ? up[i].Split(';') : new string[0];
			}
			string[] win = mdata.StringColumns[param.GetParam<int>("Sequence window").Value];
			Dictionary<string, List<Tuple<string, string, string>>> substrateProperties =
				new Dictionary<string, List<Tuple<string, string, string>>>();
			for (int i = 0; i < seqWins.Length; i++){
				string subAcc = subAccs[i];
				string seqWin = seqWins[i];
				string kinase = kinases[i];
				string kinAcc = kinAccs[i];
				if (!substrateProperties.ContainsKey(subAcc)){
					substrateProperties.Add(subAcc, new List<Tuple<string, string, string>>());
				}
				substrateProperties[subAcc].Add(new Tuple<string, string, string>(seqWin, kinase, kinAcc));
			}
			string[] kinaseNameColumn = new string[uprot.Length];
			string[] kinaseUniprotColumn = new string[uprot.Length];
			for (int i = 0; i < kinaseNameColumn.Length; i++){
				string[] win1 = AddKnownSites.TransformIl(win[i]).Split(';');
				HashSet<string> kinaseNamesHits = new HashSet<string>();
				HashSet<string> kinaseUniprotHits = new HashSet<string>();
				foreach (string ux in uprot[i]){
					if (substrateProperties.ContainsKey(ux)){
						List<Tuple<string, string, string>> properties = substrateProperties[ux];
						foreach (Tuple<string, string, string> property in properties){
							string w = property.Item1;
							if (AddKnownSites.Contains(win1, AddKnownSites.TransformIl(w.ToUpper().Substring(1, w.Length - 2)))){
								kinaseNamesHits.Add(property.Item2);
								kinaseUniprotHits.Add(property.Item3);
							}
						}
					}
				}
				kinaseNameColumn[i] = kinaseNamesHits.Count > 0 ? StringUtils.Concat(";", ArrayUtils.ToArray(kinaseNamesHits)) : "";
				kinaseUniprotColumn[i] = kinaseUniprotHits.Count > 0
					? StringUtils.Concat(";", ArrayUtils.ToArray(kinaseUniprotHits))
					: "";
			}
			mdata.AddStringColumn("PhosphoSitePlus kinase", "", kinaseNameColumn);
			mdata.AddStringColumn("PhosphoSitePlus kinase uniprot", "", kinaseUniprotColumn);
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
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Uniprot column"){
						Values = colChoice,
						Value = colInd,
						Help = "Specify here the column that contains Uniprot identifiers."
					},
					new SingleChoiceParam("Sequence window"){
						Values = colChoice,
						Value = colSeqInd,
						Help = "Specify here the column that contains the sequence windows around the site."
					}
				});
		}
	}
}