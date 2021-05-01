using BaseLibS.Graph;
using BaseLibS.Param;

namespace PluginInterop.Python{
	public class NetworkFromMatrix : PluginInterop.NetworkFromMatrix{
		public override string Name => "Network from matrix";
		public override string Description => "Create a network collection from a matrix";
		protected override string CodeFilter => "Python script, *.py | *.py";

		public override Bitmap2 DisplayImage => Bitmap2.GetImage("python.png");

		/// <summary>
		/// List of all required python packages.
		/// These packages will be checked by <see cref="ExecutableParam"/>.
		/// </summary>
		protected virtual string[] ReqiredPythonPackages => new[]{"perseuspy"};

		protected override FileParam ExecutableParam(){
			return Utils.CreateCheckedFileParam(InterpreterLabel, InterpreterFilter, TryFindExecutable,
				ReqiredPythonPackages);
		}

		protected override bool TryFindExecutable(out string path){
			return Utils.TryFindPythonExecutable(out path);
		}
	}
}