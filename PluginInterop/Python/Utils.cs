using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BaseLibS.Param;

namespace PluginInterop.Python{
	public static class Utils{
		/// <summary>
		/// Searches for python executable with perseuspy installed in PATH and installation folders.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool TryFindPythonExecutable(out string path){
			if (CheckPythonInstallation("python")){
				Debug.WriteLine("Found 'python' in PATH");
				path = "python";
				return true;
			}
			List<string> folders =
				new[]{
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
				}.Where(f => !string.IsNullOrEmpty(f)).ToList();
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!string.IsNullOrEmpty(appData)){
				folders.Add(Path.Combine(appData, "Programs"));
			}
			foreach (string folder in folders.Select(f => Path.Combine(f, "Python")).Where(Directory.Exists)){
				foreach (string pyFolder in Directory.EnumerateDirectories(folder, "Python*")){
					string pyPath = Path.Combine(pyFolder, "python.exe");
					if (CheckPythonInstallation(pyPath)){
						Debug.WriteLine($"Found 'python' in default installation folder: {pyPath}");
						path = pyPath;
						return true;
					}
				}
			}
			path = default(string);
			return false;
		}

		/// <summary>
		/// Returns true if executable path points to python and can import perseuspy.
		/// </summary>
		/// <param name="exeName"></param>
		/// <returns></returns>
		public static bool CheckPythonInstallation(string exeName){
			return CheckPythonInstallation(exeName, new[]{"perseuspy"});
		}

		/// <summary>
		/// Returns true if executable path points to python and can import the specified packages.
		/// </summary>
		/// <param name="exeName"></param>
		/// <param name="packages"></param>
		/// <returns></returns>
		public static bool CheckPythonInstallation(string exeName, string[] packages){
			try{
				string imports = string.Join("; ", packages.Select(package => $"import {package}"));
				Process p = new Process{
					StartInfo = {
						UseShellExecute = false,
						CreateNoWindow = true,
						FileName = exeName,
						Arguments = $"-c \"{imports}; print('hello')\"",
						RedirectStandardOutput = true,
					}
				};
				StringBuilder output = new StringBuilder();
				p.OutputDataReceived += (sender, args) => { output.Append(args.Data); };
				p.Start();
				p.BeginOutputReadLine();
				p.WaitForExit();
				return p.ExitCode == 0 && output.ToString().StartsWith("hello");
			} catch (Exception){
				return false;
			}
		}

		/// <summary>
		/// Type definition for the <see cref="TryFindExecutableDelegate"/> used in <see cref="Utils.CreateCheckedFileParam"/>.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public delegate bool TryFindExecutableDelegate(out string path);

		/// <summary>
		/// Create a checked file param which changes color if the python installation passes <see cref="CheckPythonInstallation"/>.
		/// </summary>
		/// <param name="interpreterLabel"></param>
		/// <param name="interpreterFilter"></param>
		/// <param name="tryFindExecutable"></param>
		/// <param name="packages">Passed directly to python.exe -e</param>
		/// <returns></returns>
		public static FileParam CreateCheckedFileParam(string interpreterLabel, string interpreterFilter,
			TryFindExecutableDelegate tryFindExecutable, string[] packages){
			CheckedFileParam fileParam = new CheckedFileParam(interpreterLabel, s => {
				if (string.IsNullOrWhiteSpace(s)){
					return null;
				}
				if (CheckPythonInstallation(s, packages)){
					return new Tuple<string, bool>("Python installation was found", true);
				}
				return new Tuple<string, bool>(
					"A valid Python installation was not found.\n" + "Could not import one or more packages:\n" +
					string.Join(", ", packages), false);
			}){Filter = interpreterFilter};
			if (tryFindExecutable(out string path)){
				fileParam.Value = path;
			}
			return fileParam;
		}
	}
}