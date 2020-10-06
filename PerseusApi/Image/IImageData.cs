using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<ISubject>{
		ISubject this[int i]{ get; }
		void AddSubject(string name);
		void AddAnat(float[,,] data);
		void AddFunc(float[,,,] data);
		void AddDwi(float[,,,] data);

		int Count{ get; }
	}
}