using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Image{
	public interface IImageFromMatrix : IImageActivity, IFromMatrix{
		void ProcessData(IMatrixData inData, IImageData outData, Parameters param, ref IData[] supplData,
			ProcessInfo processInfo);

		Parameters GetParameters(IMatrixData mdata, ref string errString);
	}
}