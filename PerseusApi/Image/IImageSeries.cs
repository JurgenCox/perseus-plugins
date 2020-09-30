using System;

namespace PerseusApi.Image{
	public interface IImageSeries : ICloneable{
		int LengthT{ get; }
		int LengthX{ get; }
		int LengthY{ get; }
		int LengthZ{ get; }
		float GetValueAt(int t, int x, int y, int z);

		float MinValue{ get; }
		float MaxValue{ get; }
	}
}