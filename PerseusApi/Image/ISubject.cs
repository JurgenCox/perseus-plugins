using System;

namespace PerseusApi.Image{
	public interface ISubject : ICloneable{
		int AnatCount{ get; }
		int FuncCount{ get; }
		int DwiCount{ get; }
		IImageSeries GetAnatAt(int index);
		IImageSeries GetFuncAt(int index);
		IImageSeries GetDwiAt(int index);
		IImageSeries GetAt(MriType type, int index);

		string Name{ get; }
	}
}