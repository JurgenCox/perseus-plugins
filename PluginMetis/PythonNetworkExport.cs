using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using System.Collections.Generic;


namespace PluginMetis
{
	public class PythonNetworkExport : IMatrixProcessing
	{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "SIF export byte to string";
		public string Heading => "Metabolic network export";
		public bool IsActive => true;
		public float DisplayRank => 1;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters)
		{
			return 1;
		}

		public string Url => null;

		public string Description => "Use this function on the output of the \"SIF export for metabolic reactions\" to convert the byte output to string.";

		public Parameters GetParameters(IMatrixData mdata, ref string errorString)
		{
			if (mdata.StringColumnCount == 0)
			{
				errorString = "Network is not loaded.";
				return null;
			}
			return new Parameters(
				new SingleChoiceParam("Source") { Values = mdata.StringColumnNames },
				new SingleChoiceParam("Relationship Type") { Values = mdata.StringColumnNames },
				new SingleChoiceParam("Target") { Values = mdata.StringColumnNames });
		}


		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ref IDocumentData[] documents, ProcessInfo processInfo)
		{
			var columns = new List<string>();
			columns.Add("Source");
			columns.Add("Relationship Type");
			columns.Add("Target");

			foreach (var name in columns)
			{
				int colInd = param.GetParam<int>(name).Value;
				string[] v = mdata.StringColumns[colInd];
				bool[] isoform = new bool[mdata.RowCount];
				List<int> valids = new List<int>();
				List<int> notvalids = new List<int>();
				for (int i = 0; i < mdata.RowCount; i++)
				{
					v[i] = IsDetected(v[i]);

				}
			}
		}


		private static string IsDetected(string y)
		{
			string test = "\"\"";
			if (y.ToString().Contains("b'") || y.ToString().Contains(test) || y.ToString().Contains("'")
				|| y.ToString().Contains("b" + "\"\""))
			{
				y = y.Replace("b'", "");
				y = y.Replace("'", "");
				y = y.Replace(test, "");
				y = y.Replace("b" + "\"\"", "");
			}
			if (y.ToString().StartsWith("b1") || y.ToString().StartsWith("b2"))
			{
				y = y.Replace("b1", "1");
				y = y.Replace("b2", "2");
			}
			return y;
		}
	}
}

