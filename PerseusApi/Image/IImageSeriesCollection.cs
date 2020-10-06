using System;

namespace PerseusApi.Image{
	public interface IImageSeriesCollection : ICloneable{
		IImageSeries TimeSeries{ get; }
		IImageSeries StaticImage{ get; }

		string Name{ get; }
	}
}