using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageExport : IImageActivity, IExport{
		void Export(Parameters parameters, IImageData data, ProcessInfo processInfo);

		Parameters GetParameters(IImageData mdata, ref string errString);
	}
}