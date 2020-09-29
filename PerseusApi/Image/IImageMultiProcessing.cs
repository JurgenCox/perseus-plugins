using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Image{
	public interface IImageMultiProcessing : IImageActivity, IMultiProcessing{
		IImageData ProcessData(IImageData[] inputData, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo);

		Parameters GetParameters(IImageData[] inputData, ref string errString);
	}
}