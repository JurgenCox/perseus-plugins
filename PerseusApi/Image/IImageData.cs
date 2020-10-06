using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<IImageSeriesCollection>{
		IImageSeriesCollection this[int i]{ get; }

		void AddImageData(float[,,,] timeSeries, float[,,] staticData, string name);

		int Count{ get; }
	}
}