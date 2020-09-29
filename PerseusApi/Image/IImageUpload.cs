using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageUpload : IImageActivity, IUpload{
		void LoadData(IImageData mdata, Parameters param, ref IData[] supplTables, ProcessInfo processInfo);
	}
}