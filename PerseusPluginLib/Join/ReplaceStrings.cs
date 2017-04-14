using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Join{
	public class ReplaceStrings : IMatrixMultiProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Replace strings";
		public bool IsActive => true;
		public float DisplayRank => -2;
		public string HelpOutput => "Same as first input matrix except that the selected text column has been edited.";
		public string Description
			=>
				"Replace strings in a text column according to a key value table. The first matrix contains the " +
				"column that will be edited while the second matrix is used to define the key-value table. In " +
				"case entries in the column that is edited contains semicola, the replacement happens for the " +
				"terms separated by these.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public int MinNumInput => 2;
		public int MaxNumInput => 2;
		public string Heading => "Basic";
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixMultiProcessing:Basic:ReplaceStrings";

		public string GetInputName(int index){
			return index == 0 ? "Base matrix" : "Other matrix";
		}

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData[] inputData, ref string errString){
			IMatrixData matrixData1 = inputData[0];
			IMatrixData matrixData2 = inputData[1];
			return
				new Parameters(new SingleChoiceParam("Column in matrix 1 to be edited"){
					Values = matrixData1.StringColumnNames,
					Value = 0,
					Help =
						"The column in the first matrix in which strings will be replaced " +
						"according to the key-value table specified in matrix 2."
				}, new SingleChoiceParam("Keys in matrix 2"){
					Values = matrixData2.StringColumnNames,
					Value = 0,
					Help = "The keys for the replacement table."
				}, new SingleChoiceParam("Values in matrix 2"){
					Values = matrixData2.StringColumnNames,
					Value = 1,
					Help = "The values for the replacement table."
				});
		}

		public IMatrixData ProcessData(IMatrixData[] inputData, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			IMatrixData mdata1 = inputData[0];
			Dictionary<string, string> map = GetMap(inputData[1], parameters);
			IMatrixData result = (IMatrixData) mdata1.Clone();
			int ind = parameters.GetParam<int>("Column in matrix 1 to be edited").Value;
			string[] x = mdata1.StringColumns[ind];
			for (int i = 0; i < x.Length; i++){
				x[i] = Process(x[i], map);
			}
			return result;
		}

		private static string Process(string s, Dictionary<string, string> map){
			if (!s.Contains(";")){
				return map.ContainsKey(s) ? map[s] : "";
			}
			string[] q = s.Split(';');
			List<string> result = new List<string>();
			foreach (string s1 in q){
				if (map.ContainsKey(s1)){
					result.Add(map[s1]);
				}
			}
			if (result.Count == 0){
				return "";
			}
			return StringUtils.Concat(";", result);
		}

		private static Dictionary<string, string> GetMap(IMatrixData mdata2, Parameters parameters){
			string[] keys = mdata2.StringColumns[parameters.GetParam<int>("Keys in matrix 2").Value];
			string[] values = mdata2.StringColumns[parameters.GetParam<int>("Values in matrix 2").Value];
			Dictionary<string, string> map = new Dictionary<string, string>();
			for (int i = 0; i < keys.Length; i++){
				if (string.IsNullOrWhiteSpace(keys[i])){
					continue;
				}
				if (!map.ContainsKey(keys[i])){
					map.Add(keys[i], values[i]);
				}
			}
			return map;
		}
	}
}