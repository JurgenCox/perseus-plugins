using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Network{
	public interface IGraphLoader{
		Parameters GetParameters();
		void Load(INetworkDataAnnColumns ndata, Parameters parameters, ProcessInfo processInfo);
	}
}