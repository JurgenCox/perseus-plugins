namespace PerseusApi.Generic {
	/// <summary>
	/// Common denominator of all activities (processing, analysis) that perform on multiple data.
	/// </summary>
	public interface IMultiActivity
	{
		/// <summary>
		/// Minimal number of input data.
		/// </summary>
		int MinNumInput { get; }
		/// <summary>
		/// Maximal number of input data.
		/// </summary>
		int MaxNumInput { get; }
		/// <summary>
		/// Specify a name of the n-th input.
		/// </summary>
		string GetInputName(int index);
	}
}
