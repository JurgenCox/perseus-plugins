using System.Collections.Generic;

namespace PerseusApi.Image{
	public interface IImageSeriesCollection: IEnumerable<IImageSeries>{
		int Count{ get; }
		IImageSeries this[int i] { get; }

		string Name{ get; }
	}
}