using System.Collections.Generic;
using BaseLibS.Api.Image;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<ISubject>{
		ISubject this[int i]{ get; }
		void AddSubject(string name, SubjectData subjectData);
		void AddAnat(float[,,] data, float voxelSizeXmm, float voxelSizeYmm, float voxelSizeZmm);

		void AddFunc(float[,,,] data, float repetitionTimeSeconds, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm);

		void AddDwi(float[,,,] data, float repetitionTimeSeconds, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm);

		int Count{ get; }
	}
}