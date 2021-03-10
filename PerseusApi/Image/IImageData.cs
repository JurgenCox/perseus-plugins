﻿using System.Collections.Generic;
using BaseLibS.Api.Image;
using PerseusApi.Generic;

namespace PerseusApi.Image{
	public interface IImageData : IDataWithAnnotationRows, IData, IEnumerable<IImageSubject>{
		IImageSubject this[int i]{ get; }
		void AddSubject(string name, SubjectData subjectData);
		void AddAnat(float[,,] data, float voxelSizeXmm, float voxelSizeYmm, float voxelSizeZmm, 
			IImageMetadata metadata = null, string name = "");

		void AddFunc(float[][,,] data, float repetitionTimeSeconds, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		void AddDwi(float[][,,] data, float repetitionTimeSeconds, float voxelSizeXmm, float voxelSizeYmm,
			float voxelSizeZmm, IImageMetadata metadata = null, string name = "");

		int Count{ get; }
	}
}