using BaseLibS.Api.Image;
using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageProcessingSubject : IImageProcessingBase{
		void ProcessData(IImageSubject mdata, Parameters param, ProcessInfo processInfo);
	}
}