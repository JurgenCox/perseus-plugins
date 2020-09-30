namespace PerseusApi.Image{
	public interface IImageSeries{
		int LengthT { get; }
		int LengthX { get; }
		int LengthY { get; }
		int LengthZ { get; }
		float GetValueAt(int t, int x, int y, int z);
	}
}