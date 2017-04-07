using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Network{
    /// <summary>
    /// Base interface for all network-specific analyses
    /// </summary>
	public interface INetworkAnalysis : INetworkActivity, IAnalysis{
        /// <summary>
        /// Run the analysis with the parameters specified in <see cref="GetParameters"/>.
        /// </summary>
        /// <param name="ndata"></param>
        /// <param name="param"></param>
        /// <param name="processInfo"></param>
        /// <returns></returns>
		IAnalysisResult AnalyzeData(INetworkData ndata, Parameters param, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the analysis.
		/// </summary>
		/// <param name="ndata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(INetworkData ndata, ref string errString);
	}
}