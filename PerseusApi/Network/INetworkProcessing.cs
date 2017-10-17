using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Network{
    /// <summary>
    /// Process a network.
    /// </summary>
	public interface INetworkProcessing : INetworkActivity, IProcessing{
        /// <summary>
        /// Process a network given the parameters specified in <see cref="GetParameters"/>.
        /// </summary>
        /// <param name="ndata"></param>
        /// <param name="param"></param>
        /// <param name="supplData"></param>
        /// <param name="processInfo"></param>
		void ProcessData(INetworkData ndata, Parameters param, ref IData[] supplData, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="ndata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(INetworkData ndata, ref string errString);
	}
}