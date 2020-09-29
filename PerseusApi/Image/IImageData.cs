using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<IImageInfo>{
		/// <summary>
		/// Get image info by index.
		/// </summary>
		IImageInfo this[int i] { get; }

		/// <summary>
		/// Add a IImageInfo to the collection.
		/// </summary>
		void AddImageInfo(params IImageInfo[] data);

	}
}