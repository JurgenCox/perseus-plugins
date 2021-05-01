using System.IO;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;

namespace PluginInterop{
	public abstract class MatrixAnalysis : InteropBase, IMatrixAnalysis{
		public abstract string Name{ get; }
		public virtual string Description => "";
		public virtual float DisplayRank => 1;
		public virtual bool IsActive => true;
		public virtual int GetMaxThreads(Parameters parameters) => 1;
		public virtual bool HasButton => true;
		public virtual string Url => projectUrl;
		public virtual Bitmap2 DisplayImage => null;
		public virtual string Heading => "External";
		public virtual DataType[] SupplDataTypes => new DataType[0];

		public IAnalysisResult AnalyzeData(IMatrixData mdata, Parameters param, ProcessInfo processInfo){
			string remoteExe = param.GetParam<string>(InterpreterLabel).Value;
			if (string.IsNullOrWhiteSpace(remoteExe)){
				processInfo.ErrString = remoteExeNotSpecified;
				return null;
			}
			string inFile = Path.GetTempFileName();
			PerseusUtils.WriteMatrixToFile(mdata, inFile);
			string[] outFiles = new[]{Path.GetTempFileName()}
				.Concat(SupplDataTypes.Select(dataType => Path.GetTempFileName())).ToArray();
			if (!TryGetCodeFile(param, out string codeFile)){
				processInfo.ErrString = $"Code file '{codeFile}' was not found";
				return null;
			}
			string commandLineArguments = GetCommandLineArguments(param);
			string args = $"{codeFile} {commandLineArguments} {inFile} {string.Join(" ", outFiles)}";
			if (Utils.RunProcess(remoteExe, args, processInfo.Status, out string errorString) != 0){
				processInfo.ErrString = errorString;
				return null;
			}
			return GenerateResult(outFiles, mdata, processInfo);
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

		protected abstract IAnalysisResult GenerateResult(string[] outFiles, IMatrixData mdata, ProcessInfo pinfo);
	}
}