using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageProcessingBase : IImageActivity, IProcessing{
		Parameters GetParameters();
		ImageFilterParameterType FilterParameterType{ get; }
	}
}