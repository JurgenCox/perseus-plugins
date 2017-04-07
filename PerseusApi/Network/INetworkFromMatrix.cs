using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Network{
    /// <summary>
    /// Derive a network from a matrix
    /// </summary>
	public interface INetworkFromMatrix : INetworkActivity, IFromMatrix {
        /// <summary>
        /// Create the network from the matrix input data and the parameters specified in <see cref="GetParameters"/>.
        /// </summary>
        /// <param name="inData"></param>
        /// <param name="outData"></param>
        /// <param name="param"></param>
        /// <param name="supplTables"></param>
        /// <param name="documents"></param>
        /// <param name="processInfo"></param>
		void ProcessData(IMatrixData inData, INetworkData outData, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="mdata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(IMatrixData mdata, ref string errString);
	}
}