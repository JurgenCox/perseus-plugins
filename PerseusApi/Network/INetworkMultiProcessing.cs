using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Network {
    /// <summary>
    /// Process multiple network collections.
    /// </summary>
	public interface INetworkMultiProcessing : INetworkActivity, IMultiProcessing {
        /// <summary>
        /// Process multiple network collections.
        /// </summary>
		INetworkData ProcessData(INetworkData[] inputData, Parameters param, ref IData[] supplData, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="inputData">The parameters might depend on the data matrices.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(INetworkData[] inputData, ref string errString);
	}
}
