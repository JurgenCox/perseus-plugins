using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Image{
	public interface IImageUpload : IImageActivity, IUpload{
		void LoadData(IImageData mdata, Parameters param, ref IMatrixData[] supplTables, ref IDocumentData[] documents,
			ProcessInfo processInfo);
	}
}