using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.AnnotCols{
	public class AddAnnotationToMatrix : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("network.png");

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
			string[][] annots = PerseusUtils.GetAvailableAnnots(out string[] baseNames, out string[] files, out List<string> badFiles);
			if (badFiles.Any())
		    {
                errorString = $"Could not load annotations from file(s): {string.Join(", ", badFiles)}";
            }
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
					new Parameters(
						new SingleChoiceParam(baseNames[i] + " column"){
							Values = colChoice,
							Value = colInd,
							Help =
								"Specify here the column that contains the base identifiers which are going to be " +
								"matched to the annotation."
						}, new MultiChoiceParam("Annotations to be added"){Values = annots[i]});
			}
			return
				new Parameters(
					new SingleChoiceWithSubParams("Source", selFile){
						Values = files,
						SubParams = subParams,
						ParamNameWidth = 136,
						TotalWidth = 735
					}, new MultiChoiceParam("Additional sources"){Values = files});
		}

		private static string[] GetBaseIds(Parameters para, IDataWithAnnotationColumns mdata){
			PerseusUtils.GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files);
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
			bool success = ProcessDataAddAnnotation(mdata.RowCount, para, baseIds, processInfo, out string[] name, out int[] catColInds,
				out int[] textColInds, out int[] numColInds, out string[][][] catCols, out string[][] textCols, out double[][] numCols);
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

		public static bool ProcessDataAddAnnotation(int nrows, Parameters para, string[] baseIds, ProcessInfo processInfo,
			out string[] name, out int[] catColInds, out int[] textColInds, out int[] numColInds, out string[][][] catCols,
			out string[][] textCols, out double[][] numCols){
			string[][] names = PerseusUtils.GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files);
			const bool deHyphenate = true;
			ParameterWithSubParams<int> spd = para.GetParamWithSubParams<int>("Source");
			int ind = spd.Value;
			Parameters param = spd.GetSubParameters();
			AnnotType[] type = types[ind];
			name = names[ind];
			int[] addtlSources = para.GetParam<int[]>("Additional sources").Value;
			addtlSources = ArrayUtils.Remove(addtlSources, ind);
			foreach (int addtlSource in addtlSources){
				AnnotType[] type1 = types[addtlSource];
				string[] name1 = names[addtlSource];
				if (!ArrayUtils.EqualArrays(type, type1)){
					processInfo.ErrString = "Additional annotation file does not have the same column structure.";
					catColInds = new int[]{};
					textColInds = new int[]{};
					numColInds = new int[]{};
					catCols = new string[][][]{};
					textCols = new string[][]{};
					numCols = new double[][]{};
					return false;
				}
				if (!ArrayUtils.EqualArrays(name, name1)){
					processInfo.ErrString = "Additional annotation file does not have the same column structure.";
					catColInds = new int[]{};
					textColInds = new int[]{};
					numColInds = new int[]{};
					catCols = new string[][][]{};
					textCols = new string[][]{};
					numCols = new double[][]{};
					return false;
				}
			}
			int[] selection = param.GetParam<int[]>("Annotations to be added").Value;
			type = ArrayUtils.SubArray(type, selection);
			name = ArrayUtils.SubArray(name, selection);
			HashSet<string> allIds = GetAllIds(baseIds, deHyphenate);
			Dictionary<string, string[]> mapping = ReadMapping(allIds, files[ind], selection);
			foreach (int addtlSource in addtlSources){
				Dictionary<string, string[]> mapping1 = ReadMapping(allIds, files[addtlSource], selection);
				foreach (string key in mapping1.Keys.Where(key => !mapping.ContainsKey(key))){
					mapping.Add(key, mapping1[key]);
				}
			}
			SplitIds(type, out textColInds, out catColInds, out numColInds);
			catCols = new string[catColInds.Length][][];
			for (int i = 0; i < catCols.Length; i++){
				catCols[i] = new string[nrows][];
			}
			textCols = new string[textColInds.Length][];
			for (int i = 0; i < textCols.Length; i++){
				textCols[i] = new string[nrows];
			}
			numCols = new double[numColInds.Length][];
			for (int i = 0; i < numCols.Length; i++){
				numCols[i] = new double[nrows];
			}
			for (int i = 0; i < nrows; i++){
				string[] ids = baseIds[i].Length > 0 ? baseIds[i].Split(';') : new string[0];
				HashSet<string>[] catVals = new HashSet<string>[catCols.Length];
				for (int j = 0; j < catVals.Length; j++){
					catVals[j] = new HashSet<string>();
				}
				HashSet<string>[] textVals = new HashSet<string>[textCols.Length];
				for (int j = 0; j < textVals.Length; j++){
					textVals[j] = new HashSet<string>();
				}
				List<double>[] numVals = new List<double>[numCols.Length];
				for (int j = 0; j < numVals.Length; j++){
					numVals[j] = new List<double>();
				}
				foreach (string id in ids){
					if (mapping.ContainsKey(id)){
						string[] values = mapping[id];
						AddCatVals(ArrayUtils.SubArray(values, catColInds), catVals);
						AddTextVals(ArrayUtils.SubArray(values, textColInds), textVals);
						AddNumVals(ArrayUtils.SubArray(values, numColInds), numVals);
					} else if (id.Contains("-")){
						string q = id.Substring(0, id.IndexOf('-'));
						if (mapping.ContainsKey(q)){
							string[] values = mapping[q];
							AddCatVals(ArrayUtils.SubArray(values, catColInds), catVals);
							AddTextVals(ArrayUtils.SubArray(values, textColInds), textVals);
							AddNumVals(ArrayUtils.SubArray(values, numColInds), numVals);
						}
					}
				}
				for (int j = 0; j < catVals.Length; j++){
					string[] q = ArrayUtils.ToArray(catVals[j]);
					Array.Sort(q);
					catCols[j][i] = q;
				}
				for (int j = 0; j < textVals.Length; j++){
					string[] q = ArrayUtils.ToArray(textVals[j]);
					Array.Sort(q);
					textCols[j][i] = StringUtils.Concat(";", q);
				}
				for (int j = 0; j < numVals.Length; j++){
					numCols[j][i] = ArrayUtils.Median(numVals[j]);
				}
			}
			return true;
		}

		private static void AddCatVals(IList<string> values, IList<HashSet<string>> catVals){
			for (int i = 0; i < values.Count; i++){
				AddCatVals(values[i], catVals[i]);
			}
		}

		private static void AddTextVals(IList<string> values, IList<HashSet<string>> textVals){
			for (int i = 0; i < values.Count; i++){
				AddTextVals(values[i], textVals[i]);
			}
		}

		private static void AddNumVals(IList<string> values, IList<List<double>> numVals){
			for (int i = 0; i < values.Count; i++){
				AddNumVals(values[i], numVals[i]);
			}
		}

		private static void AddCatVals(string value, ISet<string> catVals){
			string[] q = value.Length > 0 ? value.Split(';') : new string[0];
			foreach (string s in q){
				catVals.Add(s);
			}
		}

		private static void AddTextVals(string value, ISet<string> textVals){
			string[] q = value.Length > 0 ? value.Split(';') : new string[0];
			foreach (string s in q){
				textVals.Add(s);
			}
		}

		private static void AddNumVals(string value, ICollection<double> numVals){
			string[] q = value.Length > 0 ? value.Split(';') : new string[0];
			foreach (string s in q){
				numVals.Add(Parser.Double(s));
			}
		}

		private static void SplitIds(IList<AnnotType> types, out int[] textCols, out int[] catCols, out int[] numCols){
			List<int> tc = new List<int>();
			List<int> cc = new List<int>();
			List<int> nc = new List<int>();
			for (int i = 0; i < types.Count; i++){
				switch (types[i]){
					case AnnotType.Categorical:
						cc.Add(i);
						break;
					case AnnotType.Text:
						tc.Add(i);
						break;
					case AnnotType.Numerical:
						nc.Add(i);
						break;
					default:
						throw new Exception("Never get here.");
				}
			}
			textCols = tc.ToArray();
			catCols = cc.ToArray();
			numCols = nc.ToArray();
		}

		public static Dictionary<string, string[]> ReadMapping(ICollection<string> allIds, string file, IList<int> selection){
			Dictionary<string, string[]> result = new Dictionary<string, string[]>();
		    using (var reader = FileUtils.GetReader(file))
		    {
                reader.ReadLine();
                reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null){
                    string[] q = line.Split('\t');
                    string w = q[0];
                    string[] ids = w.Length > 0 ? w.Split(';') : new string[0];
                    string[] value = ArrayUtils.SubArray(q, selection.Select(i => i+1).ToArray());
                    foreach (string id in ids){
                        if (!allIds.Contains(id)){
                            continue;
                        }
                        if (!result.ContainsKey(id)){
                            result.Add(id, value);
                        }
                    }
                }
		    }
			return result;
		}

		private static HashSet<string> GetAllIds(IEnumerable<string> x, bool deHyphenate){
			HashSet<string> result = new HashSet<string>();
			foreach (string y in x){
				string[] z = y.Length > 0 ? y.Split(';') : new string[0];
				foreach (string q in z){
					result.Add(q);
					if (deHyphenate && q.Contains("-")){
						string r = q.Substring(0, q.IndexOf("-", StringComparison.InvariantCulture));
						result.Add(r);
					}
				}
			}
			return result;
		}
	}
}