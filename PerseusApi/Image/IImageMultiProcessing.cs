using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageMultiProcessing : IImageActivity, IMultiProcessing{
		IImageData ProcessData(IImageData[] inputData, Parameters param, ref IData[] supplTables,
			ProcessInfo processInfo);

		Parameters GetParameters(IImageData[] inputData, ref string errString);
	}
}