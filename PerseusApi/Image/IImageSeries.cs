using System;

namespace PerseusApi.Image{
	public interface IImageSeries : ICloneable{
		int LengthT{ get; }
		int LengthX{ get; }
		int LengthY{ get; }
		int LengthZ{ get; }
		float GetValueAt(int t, int x, int y, int z);
		float GetWeightAt(int c, int x, int y, int z);
		bool GetIndicatorAt(int c, int x, int y, int z);
		int IndicatorCount { get; }
		float MinValue { get; }
		float MaxValue { get; }
		bool HasTime{ get; }
		bool IsFlat{ get; }
		int FlatDimension{ get; }
		bool HasWeights{ get; }
		int NumComponents{ get; }
		bool IsTwoSided{ get; }
		void SetWeights(float[,,,] weights, bool isTwoSided);
		float RepetitionTimeSeconds{ get; }
		float VoxelSizeXmm { get; }
		float VoxelSizeYmm { get; }
		float VoxelSizeZmm { get; }
	}
}