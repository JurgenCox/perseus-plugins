using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Network{
    /// <summary>
    /// Load a network collection into Perseus
    /// </summary>
	public interface INetworkUpload : INetworkActivity, IUpload{
        /// <summary>
        /// Load a network collection into Perseus
        /// </summary>
        /// <param name="ndata"></param>
        /// <param name="parameters"></param>
        /// <param name="supplTables"></param>
        /// <param name="documents"></param>
        /// <param name="processInfo"></param>
        void LoadData(INetworkData ndata, Parameters parameters, ref IMatrixData[] supplTables,
                      ref IDocumentData[] documents, ProcessInfo processInfo);
	}
}