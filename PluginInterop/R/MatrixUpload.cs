using BaseLibS.Graph;
using BaseLibS.Param;

namespace PluginInterop.R{
	public class MatrixUpload : PluginInterop.MatrixUpload{
		public override string Name => "R upload";
		public override string Description => "Upload a matrix using R";
		public override Bitmap2 DisplayImage => Bitmap2.GetImage("Rlogo.png");

		protected override string CodeFilter => "R script, *.r | *.r";

		protected virtual string[] ReqiredPythonPackages => new[]{"perseusR"};

		protected override FileParam ExecutableParam(){
			return Utils.CreateCheckedFileParam(InterpreterLabel, InterpreterFilter, TryFindExecutable);
		}

		protected override bool TryFindExecutable(out string path){
			return Utils.TryFindRExecutable(out path);
		}
	}
}