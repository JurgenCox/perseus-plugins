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
        /// <param name="supplData"></param>
        /// <param name="processInfo"></param>
        void LoadData(INetworkData ndata, Parameters parameters, ref IData[] supplData, ProcessInfo processInfo);
	}
}