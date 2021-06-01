using System.Collections.Generic;
using BaseLibS.Api.Image;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<IImageSubject>{
		IImageSubject this[int i]{ get; }

		void AddEventName(string name);
		string[] GetEventNames();
		void AddSubject(string name, SubjectData subjectData);
		void AddAnat(float[,,] data, float voxelSizeXmm, float voxelSizeYmm, float voxelSizeZmm, 
			IImageMetadata metadata = null, string name = "");

		void AddFunc(float[][,,] data, float repetitionTimeSeconds, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddDwi(float[][,,] data, float repetitionTimeSeconds, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddParMap(float[,,] data, float voxelSizeXmm, float voxelSizeYmm, float voxelSizeZmm,
			IImageMetadata metadata = null, string name = "");

		void AddAnatAt(int subInd, int sesInd, float[,,] data, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddDefAt(int subInd, int sesInd, float[][,,] data, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddFuncAt(int subInd, int sesInd, float[][,,] data, float repetitionTimeSeconds, float voxelSizeXmm,
			float voxelSizeYmm, float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddDwiAt(int subInd, int sesInd, float[][,,] data, float repetitionTimeSeconds, float voxelSizeXmm,
			float voxelSizeYmm, float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddParMapAt(int subInd, int sesInd, float[,,] data, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		int Count{ get; }
	}
}