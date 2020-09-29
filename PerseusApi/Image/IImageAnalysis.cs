using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageAnalysis : IImageActivity, IAnalysis{
		IAnalysisResult AnalyzeData(IImageData mdata, Parameters param, ProcessInfo processInfo);

		Parameters GetParameters(IImageData mdata, ref string errString);

	}
}