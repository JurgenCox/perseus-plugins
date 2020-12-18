using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Sequence {
	public interface ISequenceMultiAnalysis : ISequenceActivity, IMultiAnalysis {
		IAnalysisResult AnalyzeData(ISequenceData[] ndata, Parameters param, ProcessInfo processInfo);
		/// <summary>
		/// Define here the parameters that determine the specifics of the analysis.
		/// </summary>
		/// <param name="sdata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(ISequenceData[] sdata, ref string errString);
	}
}
