using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Network
{
    /// <summary>
    /// Load a network collection into Perseus
    /// </summary>
	public interface INetworkUploadAnnColumns : INetworkActivity, IUpload
    {
        /// <summary>
        /// Load a network collection into Perseus
        /// </summary>
        /// <param name="ndata"></param>
        /// <param name="parameters"></param>
        /// <param name="supplData"></param>
        /// <param name="processInfo"></param>
        void LoadData(INetworkDataAnnColumns ndata, Parameters parameters, ref IData[] supplData, ProcessInfo processInfo);
    }
}
