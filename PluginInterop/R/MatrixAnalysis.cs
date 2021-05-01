using BaseLibS.Param;

namespace PluginInterop.R{
	public abstract class MatrixAnalysis : PluginInterop.MatrixAnalysis{
		protected override string CodeFilter => "R script, *.R | *.R";

		protected override FileParam ExecutableParam(){
			return Utils.CreateCheckedFileParam(InterpreterLabel, InterpreterFilter, TryFindExecutable);
		}

		protected override bool TryFindExecutable(out string path){
			return Utils.TryFindRExecutable(out path);
		}
	}
}