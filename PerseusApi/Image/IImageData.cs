using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<IImageSeries>{
		/// <summary>
		/// Get image info by index.
		/// </summary>
		IImageSeries this[int i] { get; }

		/// <summary>
		/// Add a IImageInfo to the collection.
		/// </summary>
		void AddImageInfo(params IImageSeries[] data);

		int ImageSeriesCount{ get; }

		IImageSeries GetImageSeriesAt(int index);

	}
}