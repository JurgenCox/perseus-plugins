using System.Collections.Generic;
using System.IO;
using System.Text;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Matrix;
using PluginInterop;
using PluginMetis.Properties;


namespace PluginMetis
{
	public class SIFNetworkMatrixExport : PluginInterop.Python.MatrixProcessing
	{
		public override string Name => "SIF export for metabolic reactions";
		public override bool HasButton => true;
		public override float DisplayRank => 0;
		public override Bitmap2 DisplayImage => null;
		public override string Description => "Export the metabolic reactions in the SIF format (directed) for upload and use in third party software.";
		public override string Heading => "Metabolic network export";
		public override bool IsActive => true;
		public override string Url => null;
		public override string[] HelpSupplTables => new string[0];
		public override int NumSupplTables => 0;
		public override string[] HelpDocuments => new string[0];
		public override int NumDocuments => 0;

		protected override bool TryGetCodeFile(Parameters param, out string codeFile)
		{
			byte[] code = (byte[])Resources.ResourceManager.GetObject("sif_network_export");
			codeFile = Path.GetTempFileName();
			File.WriteAllText(codeFile, Encoding.UTF8.GetString(code));
			return true;
		}

		protected override string GetCommandLineArguments(Parameters param)
		{
			string tempFile = Path.GetTempFileName();
			param.ToFile(tempFile);
			return tempFile;
		}

		protected override Parameter[] SpecificParameters(IMatrixData mdata, ref string errString)
		{
			List<string> textCols = mdata.StringColumnNames;
			return new Parameter[]{
				//new FolderParam("Output folder"),
				new SingleChoiceParam("Reactions"){
					//Value = ArrayUtils.ConsecutiveInts(textCols.Count),
					Values = textCols, Help = "Specify here the column in which the reactions are listed."
				},
				new SingleChoiceParam("Modifiers"){
					//Value = ArrayUtils.ConsecutiveInts(textCols.Count),
					Values = textCols, Help = "Specify here the column in which the modifiers are listed."
				},
				new SingleChoiceParam("Reactants"){
					//Value = ArrayUtils.ConsecutiveInts(textCols.Count),
					Values = textCols, Help = "Specify here the column in which the reactants are listed."
				},
				new SingleChoiceParam("Products"){
					//Value = ArrayUtils.ConsecutiveInts(textCols.Count),
					Values = textCols, Help = "Specify here the column in which the products are listed."
				}
			};
		}
	}
}
