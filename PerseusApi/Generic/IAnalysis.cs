using BaseLibS.Api.Generic;

namespace PerseusApi.Generic{
	/// <summary>
	/// Grandmother of all data analysis activities. They operate on one IData and do not produce any new ones 
	/// automatically. They may do so interactively. 
	/// </summary>
	public interface IAnalysis : IActivityWithHeading {
	}
}