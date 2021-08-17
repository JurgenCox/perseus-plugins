using System;
using System.Collections.Generic;
using BaseLibS.Param;
using PerseusApi.Matrix;
using System.IO;
using PluginInterop;
using System.Text;
using PluginPyMOL.Properties; // replace PluginTutorial to your project or solution name
using System.Text.RegularExpressions;

using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Utils;
using System.Diagnostics;
using System.Linq;
using BaseLibS.Graph;


namespace PluginPyMOL
{
    public class PyMOL : PluginInterop.Python.MatrixProcessing
    {
        public override string Heading => "Crosslink";
        public override string Name => "Visualize crosslink distances with PyMOL";
        public override string Description => "Open a pdb file in pymol and visualize the crosslinks in " +
            "your MaxQuant CrosslinkMsms table. Distances for each crosslink is added to the output matrix.";

        protected override string[] ReqiredPythonPackages => new[] { "perseuspy", "Bio", "numpy" };
        // protected override string[] ReqiredPythonPackages => new string[] {};

        /// <summary>
        /// FASTA file filter for file chooser in parameter GUI.
        /// </summary>
        protected string FastaFilter => "Fasta file, (*.fasta; *.fna; *.ffn; *.faa; *.frn; *.fa)|" +
            "*.fasta; *.fna; *.ffn; *.faa; *.frn; *.fa";
        /// <summary>
        /// FASTA file filter for file chooser in parameter GUI.
        /// </summary>
        protected string PdbFilter => "PDB File, *.pdb|*.pdb";
        protected string AdminRequestParamName => "Run first-time setup (requires admin)";
        private string requestExecutable => "Please specify a PyMol executable. It can be found in your" +
            "PyMol installation directory as \"python.exe\". For example, for user installations, see" +
            " AppData\\Local\\Schrodinger\\PyMOL2\\python.exe";

        protected override bool TryGetCodeFile(Parameters param, out string codeFile)
        {
            byte[] code = (byte[])Properties.Resources.ResourceManager.GetObject("pymol_crosslink");
            codeFile = Path.GetTempFileName();
            File.WriteAllText(codeFile, Encoding.UTF8.GetString(code));
            return true;
        }
        /// <summary>
        /// Attempt to write the import codefile to the <param name="codeFile"/> for execution.
        /// </summary>
        protected bool TryGetImportCodeFile(out string codeFile)
        {
            byte[] code = (byte[])Properties.Resources.ResourceManager.GetObject("admin_installer");
            codeFile = Path.GetTempFileName();
            File.WriteAllText(codeFile, Encoding.UTF8.GetString(code));
            return true;
        }
        protected override string GetCommandLineArguments(Parameters param)
        {
            var pdb = param.GetParam("Protein PDB file").StringValue;
            var fasta = param.GetParam("FASTA file").StringValue;
            var identifier = param.GetParam("Protein identifier").StringValue;
            var identifierRegex = new Regex(@".+: (.+)").Match(param.GetParam("Protein identifier type").StringValue).Groups[1];
            var stringifiedParams = $"\"{pdb}\" \"{fasta}\" \"{identifier}\" \"{identifierRegex}\"";
            return stringifiedParams;
        }
        /// <summary>
        /// Create the parameters for the GUI with default of 'Code file' and 'Executable'. Includes buttons
        /// for preview downloads of 'Parameters' for development purposes.
        /// Overwrite this function to provide custom parameters.
        /// </summary>
        public override Parameters GetParameters(IMatrixData data, ref string errString)
        {
            Parameters parameters = new Parameters();
            parameters.AddParameterGroup(FirstTimeParameters(), "First Time", false);
            Parameter[] specificParameters = SpecificParameters(data, ref errString);
            if (!string.IsNullOrEmpty(errString))
            {
                return null;
            }
            parameters.AddParameterGroup(specificParameters, "Specific", false);
            Parameter parametersPreviewButton = Utils.ParametersPreviewButton(parameters);
            Parameter previewButton = Utils.DataPreviewButton(data);
            parameters.AddParameterGroup(new[] { ExecutableParam(), previewButton, parametersPreviewButton }, "Generic",
                false);
            return parameters;
        }

        protected Parameter[] FirstTimeParameters()
        {
            return new Parameter[]
            {
                new LabelParam("Instructions")
                {
                    Help = "To make a pymol visualization you need to install PyMOL from the website (pymol.org). " +
                    "After you've installed it, select the python.exe executable that is bundled with the pymol installation" +
                    "([pymol installation directory]/python.exe) and check the first time setup box.",
                    Value = "Select the PyMOL python runtime as the executable. You must install PyMOL (pymol.org)." +
                    "Hover over \"instructions\" for details. Only the first chain will be used."
                },
                new BoolParam(AdminRequestParamName)
                {
                    Help = "Set to true if this is your first time running this script. This " +
                    "installs the necessary dependencies to pymol."
                }
            };
        }
        /// <summary>
        /// The pymol python is almost never the python executable specified in PATH. 
        /// This could create confusion, so do NOT try to find default python executable.
        /// </summary>
        private bool DontFindExecutable(out string path)
        {
            path = null;
            return false;
        }
        protected override FileParam ExecutableParam()
        {
            return PluginInterop.Python.Utils.CreateCheckedFileParam(InterpreterLabel,
                InterpreterFilter, DontFindExecutable, ReqiredPythonPackages);
        }
        /// <summary>
        /// Install necessary dependencies via an admin process if necessary.
        /// </summary>
        public override void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ref IDocumentData[] documents,
            ProcessInfo processInfo)
        {
            bool runAdminSetup = param.GetParam<bool>(AdminRequestParamName).Value;
            HashSet<string> cols = new HashSet<string>(mdata.CategoryColumnNames);
            // Note: apparenlty mdata.ColumnNames is always empty and ColumnCounts is always 0.
            // What's the point of even having it there?!
            // Signed --an angry dev
            for (int i = 0; i < mdata.NumericColumnCount; i++)
            {
                cols.Add(mdata.NumericColumnNames[i]);
            }
            for (int i = 0; i < mdata.StringColumnCount; i++)
            {
                cols.Add(mdata.StringColumnNames[i]);
            }
            for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
            {
                cols.Add(mdata.MultiNumericColumnNames[i]);
            }
            string[] reqCols = { "Decoy", "Crosslink Product Type", "Pro_InterLink1", "Pro_InterLink2", "Proteins1", "Proteins2", "InterLinks"};
            List<String> missingCols = new List<String>();
            foreach (string reqCol in reqCols)
            {
                if (!cols.Contains(reqCol))
                {
                    missingCols.Add(reqCol);
                }
            }
            if (missingCols.Count != 0)
            {
                processInfo.ErrString = $"Your matrix is missing the following required columns: " + string.Join(", ", missingCols) + ". " +
                    "This plugin is only supported for the MaxQuant crosslinkMsms table. If you have uploaded a MaxQuant crosslinkMsms.txt" +
                    " file as your matrix, include \"Crosslink Product Type\" and \"InterLiks\" in addition to all default fields. " +
                    "Note that the \"Main\" column type is not used.";
                return;
            }
            string remoteExe = param.GetParam<string>(InterpreterLabel).Value;
            if (string.IsNullOrWhiteSpace(remoteExe))
            {
                processInfo.ErrString = requestExecutable;
                return;
            }
            if (runAdminSetup)
            {
                TryGetImportCodeFile(out string importFile);
                var setupProcessInfo = new ProcessStartInfo
                {
                    FileName = remoteExe,
                    Arguments = importFile,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Verb = "runas",
                };
                Debug.WriteLine($"executing > {remoteExe} {importFile}");
                var setupProcess= new Process { StartInfo = setupProcessInfo };
                setupProcess.Start();
                setupProcess.WaitForExit();
                var exitCode = setupProcess.ExitCode;
                Debug.WriteLine($"Setup process exited with exit code {exitCode}");
                if (exitCode != 0)
                {
                    processInfo.ErrString = $"Unable to install the necessary packages to pymol.";
                    return;
                }
            }
            base.ProcessData(mdata, param, ref supplTables, ref documents, processInfo);
            if (processInfo.ErrString != null)
            {
                Match match = Regex.Match(processInfo.ErrString, @"Exception: (.+)");
                if (match.Success)
                {
                    processInfo.ErrString = Regex.Match(processInfo.ErrString, @"Exception: (.+)").Groups[0].Value;
                }
                if (processInfo.ErrString.Contains("Cannot cast array data"))
                {
                    processInfo.ErrString = "For reasons unknown to the developer of this plugin, the inclusion of " +
                        "any column whose name starts with \"#\" will terminate the program. Please remove all such columns " +
                        "and try again.";
                }
            }
        }

        protected override Parameter[] SpecificParameters(IMatrixData mdata, ref string errString)
        {
            return new Parameter[]
            {
                new FileParam("Protein PDB file")
                {
                    Help = "A PDB file of the protein of interest.",
                    Filter = PdbFilter
                },
                new FileParam("FASTA file")
                {
                    Help = "File containing primary sequence information. You can download " +
                    "FASTA Files for your organism/protein from UniProt.",
                    Filter = FastaFilter
                },

                new StringParam("Protein identifier")
                {
                    Help = "The protein identifier of your protein in UniProt. Check your fasta file if you" +
                    " are unsure."
                },
                new SingleChoiceParam("Protein identifier type")
                {
                    Help = "How your protein identifier is displayed in the fasta file. ",
                    Values = new List<String>(){
                        @"Up to first whitespace: >(\S*)",
                        @"Uniprot: >.*\|(.*)\|",
                        @"NCBI accession: >(gi\[0-9]*)",
                        @"IPI accession: >IPI:([^| .]*)",
                        @"Everything after >: >(.*)",
                        @"Up to first space: >([^ ]*)",
                        @"Up to first tab character: >([^\t]*)",
                    },
                }
            };
        }
    }
}