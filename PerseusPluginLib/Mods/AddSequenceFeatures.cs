using System;
using System.Collections.Generic;
using System.Globalization;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse.Misc;
using BaseLibS.Parse.Uniprot;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Mods{
	public class AddSequenceFeatures : IMatrixProcessing{
		public string Name => "Add sequence features";
		public float DisplayRank => 12;
		public string Description => "Site-specific sequence features are added, which were extracted from UniProt.";
		public bool IsActive => true;
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Heading => "Modifications";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Modifications:AddSequenceFeatures";

		public Parameters GetParameters(IMatrixData mdata, ref string errString){
			IList<string> choice = mdata.StringColumnNames;
			int proteinsIndex = 0;
			int positionsIndex = 0;
			for (int i = 0; i < choice.Count; i++){
				if (choice[i].Equals("Proteins")){
					proteinsIndex = i;
				}
				if (choice[i].Equals("Positions within proteins")){
					positionsIndex = i;
				}
			}
			return new Parameters(new SingleChoiceParam("Proteins"){Values = choice, Value = proteinsIndex},
				new SingleChoiceParam("Positions within proteins"){Values = choice, Value = positionsIndex},
				new BoolParam("Add status column"){Value = false});
		}

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string folder = FileUtils.executablePath + "\\conf";
			string file = folder + "\\maxquantAnnot.txt.gz";
			int protInd = param.GetParam<int>("Proteins").Value;
			int posInd = param.GetParam<int>("Positions within proteins").Value;
			bool addStatus = param.GetParam<bool>("Add status column").Value;
			string[] protCol = mdata.StringColumns[protInd];
			HashSet<string> allProtIds = new HashSet<string>();
			string[][] protIds = new string[protCol.Length][];
			for (int i = 0; i < protCol.Length; i++){
				protIds[i] = protCol[i].Length > 0 ? protCol[i].Split(';') : new string[0];
				foreach (string s in protIds[i]){
					if (!allProtIds.Contains(s)){
						allProtIds.Add(s);
					}
				}
			}
			Dictionary<string, MiniProteinAnnotation> map = MiniProteinAnnotation.ReadMapping(file, allProtIds);
			string[] posCol = mdata.StringColumns[posInd];
			int nrows = protCol.Length;
			string[][] pfamCol = new string[nrows][];
			Dictionary<FeatureType, string[][]> cols = new Dictionary<FeatureType, string[][]>();
			Dictionary<FeatureType, string[][]> statusCols = new Dictionary<FeatureType, string[][]>();
			foreach (FeatureType t in FeatureType.allFeatureTypes){
				cols.Add(t, new string[nrows][]);
				statusCols.Add(t, new string[nrows][]);
			}
			for (int i = 0; i < protCol.Length; i++){
				string[] posString = posCol[i].Length > 0 ? posCol[i].Split(';') : new string[0];
				HashSet<string> pfams = new HashSet<string>();
				Dictionary<FeatureType, HashSet<string>> others = new Dictionary<FeatureType, HashSet<string>>();
				Dictionary<FeatureType, HashSet<string>> othersStatus = new Dictionary<FeatureType, HashSet<string>>();
				for (int j = 0; j < protIds[i].Length; j++){
					string protId = protIds[i][j];
					int pos = Parser.Int(posString[j]);
					if (map.ContainsKey(protId)){
						MiniProteinAnnotation mpa = map[protId];
						for (int k = 0; k < mpa.PfamIds.Length; k++){
							if (Fits(pos, mpa.PfamStart[k], mpa.PfamEnd[k])){
								pfams.Add(mpa.PfamNames[k]);
							}
						}
						foreach (FeatureType featureType in mpa.Features.Keys){
							foreach (UniprotFeature uf in mpa.Features[featureType]){
								int begin;
								int end;
								if (!Parser.TryInt(uf.FeatureBegin, out begin)){
									begin = int.MaxValue;
								}
								if (!Parser.TryInt(uf.FeatureEnd, out end)){
									end = int.MinValue;
								}
								if (Fits(pos, begin, end)){
									if (!others.ContainsKey(featureType)){
										others.Add(featureType, new HashSet<string>());
										othersStatus.Add(featureType, new HashSet<string>());
									}
									string x = uf.FeatureDescription;
									if (string.IsNullOrEmpty(x)){
										x = "+";
									}
									others[featureType].Add(x);
									string y = uf.FeatureStatus;
									if (!string.IsNullOrEmpty(y)){
										othersStatus[featureType].Add(y);
									}
								}
							}
						}
					}
				}
				pfamCol[i] = ToArray(pfams);
				foreach (FeatureType t in FeatureType.allFeatureTypes){
					if (others.ContainsKey(t)){
						cols[t][i] = ToArray(others[t]);
					} else{
						cols[t][i] = new string[0];
					}
					if (othersStatus.ContainsKey(t)){
						statusCols[t][i] = ToArray(othersStatus[t]);
					} else{
						statusCols[t][i] = new string[0];
					}
				}
			}
			mdata.AddCategoryColumn("Pfam domains", "", pfamCol);
			foreach (FeatureType t in FeatureType.allFeatureTypes){
				mdata.AddCategoryColumn(t.UniprotName, "", cols[t]);
				if (addStatus){
					mdata.AddCategoryColumn(t.UniprotName + " status", "", statusCols[t]);
				}
			}
		}

		private static string[] ToArray(HashSet<string> pfams){
			string[] q = ArrayUtils.ToArray(pfams);
			Array.Sort(q);
			return q;
		}

		private static bool Fits(int pos, int start, int end){
			return pos >= start && pos <= end;
		}
	}
}