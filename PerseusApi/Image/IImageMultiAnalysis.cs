using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageMultiAnalysis : IImageActivity, IMultiAnalysis{
		IAnalysisResult AnalyzeData(IImageData[] mdata, Parameters param, ProcessInfo processInfo);

		Parameters GetParameters(IImageData[] mdata, ref string errString);
	}
}