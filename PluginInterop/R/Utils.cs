using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BaseLibS.Param;

namespace PluginInterop.R{
	public class Utils{
		/// <summary>
		/// Searches for python executable with perseuspy installed in PATH and installation folders.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool TryFindRExecutable(out string path){
			if (CheckRInstallation("Rscript")){
				Debug.WriteLine("Found 'Rscript' in PATH");
				path = "Rscript";
				return true;
			}
			IEnumerable<string> folders =
				new[]{
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
					Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
				}.Where(f => !string.IsNullOrEmpty(f));
			foreach (string folder in folders.Select(f => Path.Combine(f, "R")).Where(Directory.Exists)){
				foreach (string subFolder in Directory.EnumerateDirectories(folder, "R*")){
					string exePath = Path.Combine(subFolder, "bin", "Rscript.exe");
					if (CheckRInstallation(exePath)){
						Debug.WriteLine($"Found 'R' in default installation folder: {exePath}");
						path = exePath;
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
		public static bool CheckRInstallation(string exeName){
			try{
				Process p = new Process{
					StartInfo = {
						UseShellExecute = false,
						CreateNoWindow = true,
						FileName = exeName,
						Arguments = "--vanilla -e \"library('PerseusR')\"",
						RedirectStandardOutput = true,
					}
				};
				StringBuilder output = new StringBuilder();
				p.OutputDataReceived += (sender, args) => { output.Append(args.Data); };
				p.Start();
				p.BeginOutputReadLine();
				p.WaitForExit();
				return p.ExitCode == 0;
			} catch (Exception){
				return false;
			}
		}

		/// <summary>
		/// Try to find R executable and show green light if found.
		/// </summary>
		public static FileParam CreateCheckedFileParam(string interpreterLabel, string interpreterFilter,
			Python.Utils.TryFindExecutableDelegate tryFindExecutable){
			Tuple<string, bool> CheckFileName(string s){
				if (string.IsNullOrWhiteSpace(s)){
					return null;
				}
				if (CheckRInstallation(s)){
					return new Tuple<string, bool>("R installation was found", true);
				}
				return new Tuple<string, bool>(
					"A valid R installation was not found. Make sure to select a R installation with 'PerseusR' installed",
					false);
			}

			CheckedFileParam fileParam =
				new CheckedFileParam(interpreterLabel, CheckFileName){Filter = interpreterFilter};
			if (tryFindExecutable(out string path)){
				fileParam.Value = path;
			}
			return fileParam;
		}

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

		public delegate bool TryFindExecutableDelegate(out string path);

		public static FileParam CreateCheckedFileParamforupload(string interpreterLabel, string interpreterFilter,
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
			string path;
			if (tryFindExecutable(out path)){
				fileParam.Value = path;
			}
			return fileParam;
		}
	}
}