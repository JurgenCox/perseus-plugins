using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageProcessing : IImageActivity, IProcessing{
		void ProcessData(IImageData mdata, Parameters param, ref IData[] supplTables, ProcessInfo processInfo);

		Parameters GetParameters(IImageData mdata, ref string errString);
	}
}