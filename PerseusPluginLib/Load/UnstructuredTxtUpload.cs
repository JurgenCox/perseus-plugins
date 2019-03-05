using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Load{
	public class UnstructuredTxtUpload : IMatrixUpload{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("raw.png");
		public string Name => "Raw upload";
		public bool IsActive => true;
		public float DisplayRank => 10;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public int GetMaxThreads(Parameters parameters) { return 1; }
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixUpload:UnstructuredTxtUpload";

		public string Description => "Load all lines from a text file and put them into a single text column or split them into " +
									"multiple text columns.";

	    private static readonly (char separator, string name)[] Separators = {('\t', "Tab"), (',', "Comma"), (' ', "Space")};
		public Parameters GetParameters(ref string errString){
			return
				new Parameters(new FileParam("File"){
					Filter = "All files (*.*)|*.*",
					Help = "Please specify here the name of the file to be uploaded including its full path."
				}, new BoolWithSubParams("Split into columns", false){
					SubParamsTrue = new Parameters(new SingleChoiceParam("Separator"){Values = Separators.Select(sep => sep.name).ToArray()})
				});
		}

		public void LoadData(IMatrixData mdata, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
				string filename = parameters.GetParam<string>("File").Value;
				ParameterWithSubParams<bool> bsp = parameters.GetParamWithSubParams<bool>("Split into columns");
			bool split = bsp.Value;
			if (split){
				int sepIndex = bsp.GetSubParameters().GetParam<int>("Separator").Value;
				LoadSplit(mdata, filename, Separators[sepIndex].separator);
			} else{
				LoadNoSplit(mdata, filename);
			}
		}

		private static void LoadNoSplit(IMatrixData mdata, string filename){
			List<string> lines = new List<string>();
			StreamReader reader = FileUtils.GetReader(filename);
			string line;
			while ((line = reader.ReadLine()) != null){
				lines.Add(line);
			}
			reader.Close();
			mdata.Values.Init(lines.Count,0);
			mdata.SetAnnotationColumns(new List<string>(new[]{"All data"}), new List<string>(new[]{"Complete file in one text column."}),
				new List<string[]>(new[]{lines.ToArray()}), new List<string>(), new List<string>(), new List<string[][]>(),
				new List<string>(), new List<string>(), new List<double[]>(), new List<string>(), new List<string>(),
				new List<double[][]>());
			mdata.Origin = filename;
		}

		private static void LoadSplit(IMatrixData mdata, string filename, char separator){
			string[] colNames = TabSep.GetColumnNames(filename, 0, PerseusUtils.commentPrefix,
				PerseusUtils.commentPrefixExceptions, null, separator);
			string[][] cols = TabSep.GetColumns(colNames, filename, 0, PerseusUtils.commentPrefix,
				PerseusUtils.commentPrefixExceptions, separator);
			var rowCount = (cols.FirstOrDefault() ?? new string[0]).Length;
			mdata.Values.Init(rowCount,0);
			mdata.SetAnnotationColumns(new List<string>(colNames), new List<string>(colNames), new List<string[]>(cols), new List<string>(),
				new List<string>(), new List<string[][]>(), new List<string>(), new List<string>(), new List<double[]>(),
				new List<string>(), new List<string>(), new List<double[][]>());
			mdata.Origin = filename;
		}
	}
}