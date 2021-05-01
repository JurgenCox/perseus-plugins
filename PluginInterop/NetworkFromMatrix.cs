using System.Diagnostics;
using System.IO;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;
using PerseusApi.Utils;

namespace PluginInterop{
	public abstract class NetworkFromMatrix : InteropBase, INetworkFromMatrixAnnColumns{
		public abstract string Name{ get; }
		public abstract string Description{ get; }
		public virtual float DisplayRank => 1;
		public virtual bool IsActive => true;
		public virtual int GetMaxThreads(Parameters parameters) => 1;
		public virtual bool HasButton => false;
		public virtual Bitmap2 DisplayImage => null;
		public virtual string Url => projectUrl;
		public virtual string Heading => "External";
		public virtual string HelpOutput => "";
		public virtual string[] HelpSupplTables => new string[0];
		public virtual int NumSupplTables => 0;
		public virtual string[] HelpDocuments => new string[0];
		public virtual int NumDocuments => 0;
		public virtual DataType[] SupplDataTypes => Enumerable.Repeat(DataType.Matrix, NumSupplTables).ToArray();

		public void ProcessData(IMatrixData inData, INetworkDataAnnColumns outData, Parameters param,
			ref IData[] supplData, ProcessInfo processInfo){
			string remoteExe = param.GetParam<string>(InterpreterLabel).Value;
			if (string.IsNullOrWhiteSpace(remoteExe)){
				processInfo.ErrString = remoteExeNotSpecified;
			}
			string inFile = Path.GetTempFileName();
			PerseusUtils.WriteMatrixToFile(inData, inFile);
			string outFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			if (!TryGetCodeFile(param, out string codeFile)){
				processInfo.ErrString = $"Code file '{codeFile}' was not found";
				return;
			}
			string[] suppFiles = SupplDataTypes.Select(Utils.CreateTemporaryPath).ToArray();
			string commandLineArguments = GetCommandLineArguments(param);
			string args = $"{codeFile} {commandLineArguments} {inFile} {outFolder} {string.Join(" ", suppFiles)}";
			Debug.WriteLine($"executing > {remoteExe} {args}");
			if (Utils.RunProcess(remoteExe, args, processInfo.Status, out string processInfoErrString) != 0){
				processInfo.ErrString = processInfoErrString;
				return;
			}
			FolderFormat.Read(outData, outFolder, processInfo);
			supplData = Utils.ReadSupplementaryData(suppFiles, SupplDataTypes, processInfo);
		}

		/// <summary>
		/// Create the parameters for the GUI with default of generic 'Code file'
		/// and 'Additional arguments' parameters. Overwrite this function for custom structured parameters.
		/// </summary>
		protected virtual Parameter[] SpecificParameters(IMatrixData data, ref string errString){
			return new Parameter[]{CodeFileParam(), AdditionalArgumentsParam()};
		}

		/// <summary>
		/// Create the parameters for the GUI with default of generic 'Executable', 'Code file' and 'Additional arguments' parameters.
		/// Includes buttons for preview downloads of 'Data' and 'Parameters' for development purposes.
		/// Overwrite <see cref="SpecificParameters"/> to add specific parameter. Overwrite this function for full control.
		/// </summary>
		public virtual Parameters GetParameters(IMatrixData data, ref string errString){
			Parameters parameters = new Parameters();
			Parameter[] specificParameters = SpecificParameters(data, ref errString);
			if (!string.IsNullOrEmpty(errString)){
				return null;
			}
			parameters.AddParameterGroup(specificParameters, "Specific", false);
			Parameter previewButton = Utils.DataPreviewButton(data);
			Parameter parametersPreviewButton = Utils.ParametersPreviewButton(parameters);
			parameters.AddParameterGroup(new[]{ExecutableParam(), previewButton, parametersPreviewButton}, "Generic",
				false);
			return parameters;
		}
	}
}