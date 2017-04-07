using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Network{
    /// <summary>
    /// Process a network collection into a matrix
    /// </summary>
	public interface INetworkToMatrix : INetworkActivity, IToMatrix {
        /// <summary>
        /// Process a network collection into a matrix
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="outData"></param>
        /// <param name="param"></param>
        /// <param name="supplTables"></param>
        /// <param name="documents"></param>
        /// <param name="processInfo"></param>
		void ProcessData(INetworkData inData, IMatrixData outData, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="mdata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(INetworkData mdata, ref string errString);
	}
}