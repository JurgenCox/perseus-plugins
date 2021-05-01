using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;
using PerseusApi.Utils;

namespace PluginInterop{
	/// <summary>
	/// Provides a number of utility functions
	/// </summary>
	public static class Utils{
		/// <summary>
		/// Write parameters to temporary file.
		/// Useful as alternative implementation of <see cref="InteropBase.GetCommandLineArguments"/>.
		/// </summary>
		public static string WriteParametersToFile(Parameters param){
			string tempFile = Path.GetTempFileName();
			param.ToFile(tempFile);
			return tempFile;
		}

		/// <summary>
		/// Create a preview button for the GUI which can be used save the data to file.
		/// This is especially useful for development and debugging.
		/// </summary>
		public static Parameter DataPreviewButton(IData data){
			if (data is IMatrixData mdata){
				return MatrixDataPreviewButton(mdata);
			}
			if (data is INetworkDataAnnColumns ndata){
				return NetworkDataPreviewButton(ndata);
			}
			throw new NotImplementedException(
				$"{nameof(DataPreviewButton)} not implemented for type {data.GetType()}!");
		}

		public static Parameter MatrixDataPreviewButton(IMatrixData mdata){
			return new SaveFileParam("Download data for preview", "save", $"{mdata.Name}.txt",
				"tab-separated data, *.txt|*.txt", s => PerseusUtils.WriteMatrixToFile(mdata, s));
		}

		public static Parameter NetworkDataPreviewButton(INetworkDataAnnColumns ndata){
			return new SaveFolderParam("Download data for preview", "save", s => FolderFormat.Write(ndata, s));
		}

		/// <summary>
		/// Create a preview button for the GUI which can be used save the parameters to file.
		/// This is especially useful for development and debugging.
		/// </summary>
		public static Parameter ParametersPreviewButton(Parameters parameters){
			return new SaveFileParam("Download parameter for preview", "save", "parameters.xml", "*.xml|*.xml", s => {
				using (StreamWriter f = new StreamWriter(s)){
					Parameters p = parameters.ConvertNew(ParamUtils.ConvertBack);
					XmlSerializer serializer = new XmlSerializer(p.GetType());
					serializer.Serialize(f, p);
				}
			});
		}

		/// <summary>
		/// Runs the executable with the provided arguments. Returns the exit code of the process,
		/// where 0 indicates success.
		/// </summary>
		public static int RunProcess(string remoteExe, string args, Action<string> status, out string errorString){
			errorString = null; // no error
			ProcessStartInfo externalProcessInfo = new ProcessStartInfo{
				FileName = remoteExe,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			Process process = new Process{StartInfo = externalProcessInfo};
			List<string> outputData = new List<string>();
			process.OutputDataReceived += (sender, output) => {
				Debug.WriteLine(output.Data);
				status(output.Data);
				outputData.Add(output.Data);
			};
			List<string> errorData = new List<string>();
			process.ErrorDataReceived += (sender, error) => {
				Debug.WriteLine(error.Data);
				errorData.Add(error.Data);
			};
			process.Start();
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
			process.WaitForExit();
			int exitCode = process.ExitCode;
			Debug.WriteLine($"Process exited with exit code {exitCode}");
			if (exitCode != 0){
				string statusString = String.Join("\n", outputData);
				string errString = String.Join("\n", errorData);
				errorString = String.Concat("Output\n", statusString, "\n", "Error\n", errString);
			}
			process.Dispose();
			return exitCode;
		}

		/// <summary>
		/// Read supplementary files according to file paths and data types.
		/// </summary>
		public static IData[] ReadSupplementaryData(string[] suppFiles, DataType[] supplDataTypes,
			ProcessInfo processInfo){
			int numSupplTables = suppFiles.Length;
			IData[] supplData = new IData[numSupplTables];
			for (int i = 0; i < numSupplTables; i++){
				switch (supplDataTypes[i]){
					case DataType.Matrix:
						IMatrixData mdata = PerseusFactory.CreateMatrixData();
						PerseusUtils.ReadMatrixFromFile(mdata, processInfo, suppFiles[i], '\t');
						supplData[i] = mdata;
						break;
					case DataType.Network:
						INetworkDataAnnColumns ndata = PerseusFactoryAnnColumns.CreateNetworkData();
						FolderFormat.Read(ndata, suppFiles[i], processInfo);
						supplData[i] = ndata;
						break;
					default:
						throw new NotImplementedException($"Data type {supplDataTypes[i]} not supported!");
				}
			}
			return supplData;
		}

		/// <summary>
		/// Create a temporary path for a specific data type.
		/// </summary>
		public static string CreateTemporaryPath(DataType dataType){
			switch (dataType){
				case DataType.Matrix:
					return Path.GetTempFileName();
				case DataType.Network:
					return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				default:
					throw new NotImplementedException($"Data type {dataType} not supported!");
			}
		}
	}
}