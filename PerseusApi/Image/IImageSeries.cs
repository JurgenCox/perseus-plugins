namespace PerseusApi.Image{
	public interface IImageSeries{
		int LenthT { get; }
		int LenthX { get; }
		int LenthY { get; }
		int LenthZ { get; }
		float GetValueAt(int t, int x, int y, int z);
	}
}