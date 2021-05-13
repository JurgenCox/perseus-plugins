using BaseLibS.Api.Image;
using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageProcessingSession : IImageProcessingBase{
		void ProcessData(IImageSession mdata, Parameters param, ProcessInfo processInfo);
	}
}