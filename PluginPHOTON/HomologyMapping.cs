using BaseLibS.Param;
using PerseusApi.Matrix;
using PluginInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Path = System.IO.Path;

namespace PluginPHOTON
{
	public class HomologyMapping : PluginInterop.Python.MatrixProcessing
	{
		public override string Heading => "Homology";
		public override string Name => "Map between human and mouse genes";

		public override string Description =>
			"Map between mouse Entrez gene ids and human Entrez gene ids by homology from MGI.";

		public override bool HasButton => false;

		protected override string[] ReqiredPythonPackages => new[] { "perseuspy" };

		protected override bool TryGetCodeFile(Parameters param, out string codeFile)
		{
			byte[] code = (byte[])Properties.Resources.ResourceManager.GetObject("homology_mapping");
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
			if (mdata.StringColumnCount == 0)
			{
				errString = "Matrix does not have any text columns. Please add a 'GeneID' text column";
				return null;
			}
			int geneid = mdata.StringColumnNames.FindIndex(col => col.ToLower().Equals("geneid"));
			return new Parameter[]{
                new LabelParam("Instructions")
                {
                    Value = "Please make sure to install all the packages required for PluginPHOTON. Visit the link: ",
                },
                new SingleChoiceParam("GeneID", Math.Max(0, geneid)){Values = mdata.StringColumnNames},
				new SingleChoiceParam("Map from", 0){Values = new List<string>{"mouse to human", "human to mouse"}},
			};
		}
	}
}
