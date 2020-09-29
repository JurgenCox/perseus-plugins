using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Image{
	public interface IImageProcessing : IImageActivity, IProcessing{
		void ProcessData(IImageData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo);

		Parameters GetParameters(IImageData mdata, ref string errString);
	}
}