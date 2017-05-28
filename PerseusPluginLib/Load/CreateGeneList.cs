using System.Collections.Generic;
using System.IO;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Load{
	public class CreateGeneList : IMatrixUpload{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("list.png");
		public string Description => "Start with a list of all protein-coding genes from an organism.";
		public string Name => "Create gene list";
		public bool IsActive => true;
		public float DisplayRank => 4;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixUpload:CreateGeneList";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void LoadData(IMatrixData matrixData, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
				int ind = parameters.GetParam<int>("Organism").Value;
			string filename = GetOrganismFiles()[ind];
			List<string> stringColnames = new List<string>(TabSep.GetColumnNames(filename, '\t'));
			List<string[]> stringCols = new List<string[]>();
			foreach (string t in stringColnames){
				string[] col = TabSep.GetColumn(t, filename, '\t');
				stringCols.Add(col);
			}
			matrixData.Name = "Gene list";
			matrixData.ColumnNames = new List<string>();
			matrixData.Values.Init(stringCols[0].Length,0);
			matrixData.SetAnnotationColumns(stringColnames, stringCols,
				new List<string>(), new List<string[][]>(), new List<string>(), new List<double[]>(), new List<string>(),
				new List<double[][]>());
			matrixData.Origin = "Gene list";
		}

		private static string[] GetOrganismFiles(){
			string path = FileUtils.GetConfigPath() + "\\perseus\\genelists";
			string[] files = Directory.GetFiles(path);
			List<string> result = new List<string>();
			foreach (string s in files){
				if (s.EndsWith(".txt")){
					result.Add(s);
				}
			}
			return result.ToArray();
		}

		private static string[] GetOrganismNames(){
			string[] files = GetOrganismFiles();
			for (int i = 0; i < files.Length; i++){
				files[i] = files[i].Substring(files[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
				files[i] = files[i].Substring(0, files[i].Length - 4);
			}
			return files;
		}

		public Parameters GetParameters(ref string errorString){
			string[] organisms = GetOrganismNames();
			return
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Organism"){
						Values = organisms,
						Help = "Select the organism for which the gene list should be created."
					}
				});
		}
	}
}