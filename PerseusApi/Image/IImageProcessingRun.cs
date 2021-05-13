using BaseLibS.Api.Image;
using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageProcessingRun : IImageProcessingBase{
		void ProcessData(IImageSeries mdata, Parameters param, ProcessInfo processInfo);
	}
}