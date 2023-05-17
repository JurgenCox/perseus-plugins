using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BaseLib.Graphic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Network;
using PluginInterop;
using Path = System.IO.Path;

namespace PluginPHOTON
{
	public class PhotonFromNetwork : PluginInterop.Python.NetworkProcessing
	{
		public override string Heading => "Modifications";
		public override string Name => "PHOTON";

		public override string Description =>
			"Calculate signaling functionality scores for all proteins in the network.";

		public override Bitmap2 DisplayImage =>
			Bitmap2.GetImage(Assembly.GetExecutingAssembly(), "PluginPHOTON.img.icon.png");

		protected override string[] ReqiredPythonPackages => new[] { "perseuspy", "phos", "joblib", "numpy",
        "pandas","scipy", "matplotlib", "networkx", "flask","requests","celery","redis","goenrich","scikit-learn","pytest-runner" };

        public override int NumSupplTables => 2;
		public override DataType[] SupplDataTypes => new[] { DataType.Network, DataType.Matrix };

		protected override bool TryGetCodeFile(Parameters param, out string codeFile)
		{
			codeFile = Path.GetTempFileName();
			File.WriteAllText(codeFile, Encoding.UTF8.GetString(Properties.Resources.photon_from_network));
			return true;
		}

		protected override string GetCommandLineArguments(Parameters param)
		{
			string tempFile = Path.GetTempFileName();
			param.ToFile(tempFile);
			return tempFile;
		}

		protected override Parameter[] SpecificParameters(INetworkData ndata, ref string errString)
		{
			if (ndata.Count() != 1)
			{
				errString = "Please make sure to have only one network in the collection.";
				return null;
			}
			INetworkInfo network = ndata.Single();
			IDataWithAnnotationColumns nodeTable = network.NodeTable;
			if (nodeTable.MultiNumericColumnCount < 1)
			{
				errString = "Please add at least one multi numeric column to the node table." +
							" Peptide-level information might have been collapsed to protein-level during 'Annotate nodes'." +
							" Use 'Combine copied main values' with `keep separate` option to retain peptide-level information.";
				return null;
			}
			IDataWithAnnotationColumns edgeTable = network.EdgeTable;
			int confidenceColumn = edgeTable.NumericColumnNames.FindIndex(col => col.ToLower().Equals("confidence"));
			StringParam signalingSourceParam = new StringParam("Signaling source")
			{
				Help =
					"Select the starting point (anchor) of the signaling network. Leave blank for un-rooted network. " +
					"Has to be a Node from the network."
			};
			IntParam topTerminalsParam = new IntParam("Restrict terminals to n top-scoring proteins", 100)
			{
				Help = "Restrict the size of the reconstructed network by considering only the n top-scoring proteins."
			};
			return new Parameter[]{
				new MultiChoiceParam("Data columns"){
					Value = Enumerable.Range(0, nodeTable.MultiNumericColumnCount).Where(i =>
						!new[]{"position"}.Contains(nodeTable.MultiNumericColumnNames[i].ToLower())).ToArray(),
					Values = nodeTable.MultiNumericColumnNames,
					Help = "PTM data of different conditions for the nodes in the network."
				},
				new SingleChoiceParam("Confidence column"){
					Value = confidenceColumn == -1 ? edgeTable.NumericColumnCount : confidenceColumn,
					Values = edgeTable.NumericColumnNames.Concat(new[]{"Use constant value"}).ToList(),
					Help =
						"Confidence score for interactions. Will be used as weights in the signaling score calculation"
				},
				new SingleChoiceParam("Side"){
					Values = new[]{"greater", "twosided", "lesser"},
					Help =
						"Sidedness of the test. Choose 'greater' for proteins with increased signaling functionality."
				},
				new BoolWithSubParams("Reconstruct signaling networks with ANAT", false){
					Help =
						"Reconstruct a signaling network that connects all significant proteins. Uses the ANAT web server.",
					SubParamsFalse = new Parameters(),
					SubParamsTrue = new Parameters(signalingSourceParam, topTerminalsParam)
				},
				new IntParam("Required number of observations", 4){
					Help = "Required minimum number of observations for score calculation."
				},
				new IntParam("Number of permutations", 1000){
					Help = "Number of permutations used for empirical p-value calculation"
				},
				new BoolParam("Additional columns", false){
					Help =
						"Score and significance are always reported. Select for additional columns such as one-sided p-values."
				}
			};
		}
	}
}
