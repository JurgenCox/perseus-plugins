namespace PerseusApi.Generic {
	/// <summary>
	/// Grandmother of all data multi-analysis activities. They operate on multiple IData and do not produce any new ones 
	/// automatically. They may do so interactively.
	/// </summary>
	public interface IMultiAnalysis : IAnalysis, IMultiActivity
	{
	}
}
