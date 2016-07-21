using System;
using System.Collections.Generic;
using System.IO;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;

namespace PerseusPluginLib.Annot{
	public class BackToBaseIdentifiers : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description
			=>
				"This activity does the inverse of the 'Add annotation' activity. " +
				"Any of the columns that can be created by the " +
				"'Add annotation' activity can be mapped back to the base identifiers (typically UniProt ids).";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "To base identifiers";
		public string Heading => "Annot. columns";
		public bool IsActive => true;
		public float DisplayRank => -19.5f;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotcolumns:BackToBaseIdentifiers";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> colChoice = mdata.StringColumnNames;
			int colInd = 0;
			for (int i = 0; i < colChoice.Count; i++){
				if (colChoice[i].ToUpper().Contains("GENE NAME")){
					colInd = i;
					break;
				}
			}
			string[] baseNames;
			int[][] inds;
			string[] files;
			string[][] annots = GetAvailableTextAnnots(out baseNames, out inds, out files);
			int selFile = 0;
			for (int i = 0; i < files.Length; i++){
				if (files[i].ToLower().Contains("perseusannot")){
					selFile = i;
					break;
				}
			}
			Parameters[] subParams = new Parameters[files.Length];
			for (int i = 0; i < subParams.Length; i++){
				int selInd = 0;
				for (int j = 0; j < annots[i].Length; j++){
					if (annots[i][j].ToLower().Contains("gene name")){
						selInd = j;
						break;
					}
				}
				subParams[i] =
					new Parameters(new Parameter[]{
						new SingleChoiceParam("Identifiers"){
							Values = colChoice,
							Value = colInd,
							Help =
								"Specify here the column that contains the identifiers which are going to be matched back to " + baseNames[i] +
								" identifiers."
						},
						new SingleChoiceParam("Identifier type"){Values = annots[i], Value = selInd}
					});
			}
			return
				new Parameters(new SingleChoiceWithSubParams("Source", selFile){
					Values = files,
					SubParams = subParams,
					ParamNameWidth = 136,
					TotalWidth = 735
				});
		}

		public void ProcessData(IMatrixData mdata, Parameters para, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			string[] baseNames;
			int[][] inds;
			string[] files;
			GetAvailableTextAnnots(out baseNames, out inds, out files);
			ParameterWithSubParams<int> spd = para.GetParamWithSubParams<int>("Source");
			int ind = spd.Value;
			Parameters param = spd.GetSubParameters();
			int baseCol = param.GetParam<int>("Identifiers").Value;
			int selection = param.GetParam<int>("Identifier type").Value;
			HashSet<string> allIds = GetAllIds(mdata, baseCol);
			string file = files[ind];
			Dictionary<string, string[]> mapping = ReadMapping(allIds, file, inds[ind][selection]);
			string[] x = mdata.StringColumns[baseCol];
			string[] newCol = new string[x.Length];
			for (int i = 0; i < x.Length; i++){
				string w = x[i];
				string[] q = w.Length > 0 ? w.Split(';') : new string[0];
				List<string> m = new List<string>();
				foreach (string s in q){
					string r = s.ToLower();
					if (mapping.ContainsKey(r)){
						m.AddRange(mapping[r]);
					}
				}
				string[] vals = ArrayUtils.UniqueValues(m);
				newCol[i] = StringUtils.Concat(";", vals);
			}
			mdata.AddStringColumn(baseNames[ind], baseNames[ind], newCol);
		}

		private static Dictionary<string, string[]> ReadMapping(ICollection<string> allIds, string file, int selection){
			selection++;
			StreamReader reader = FileUtils.GetReader(file);
			reader.ReadLine();
			reader.ReadLine();
			string line;
			Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();
			while ((line = reader.ReadLine()) != null){
				string[] q = line.Split('\t');
				string w = q[0];
				string[] ids1 = w.Length > 0 ? w.Split(';') : new string[0];
				string v = q[selection];
				string[] ids2 = v.Length > 0 ? v.Split(';') : new string[0];
				foreach (string id in ids2){
					string idx = id.ToLower();
					if (!allIds.Contains(idx)){
						continue;
					}
					if (!result.ContainsKey(idx)){
						result.Add(idx, new HashSet<string>());
					}
					foreach (string s in ids1){
						result[idx].Add(s);
					}
				}
			}
			Dictionary<string, string[]> result1 = new Dictionary<string, string[]>();
			foreach (KeyValuePair<string, HashSet<string>> pair in result){
				string[] s = ArrayUtils.ToArray(pair.Value);
				Array.Sort(s);
				result1.Add(pair.Key, s);
			}
			return result1;
		}

		private static HashSet<string> GetAllIds(IDataWithAnnotationColumns mdata, int baseCol){
			string[] x = mdata.StringColumns[baseCol];
			HashSet<string> result = new HashSet<string>();
			foreach (string y in x){
				string[] z = y.Length > 0 ? y.Split(';') : new string[0];
				foreach (string q in z){
					result.Add(q.ToLower());
				}
			}
			return result;
		}

		private static string[][] GetAvailableTextAnnots(out string[] baseNames, out int[][] inds, out string[] files){
			AnnotType[][] types;
			string[][] annots = PerseusUtils.GetAvailableAnnots(out baseNames, out types, out files);
			inds = new int[files.Length][];
			for (int i = 0; i < files.Length; i++){
				List<int> result = new List<int>();
				for (int j = 0; j < types[i].Length; j++){
					if (types[i][j] == AnnotType.Text){
						result.Add(j);
					}
				}
				inds[i] = result.ToArray();
				annots[i] = ArrayUtils.SubArray(annots[i], result);
			}
			return annots;
		}
	}
}