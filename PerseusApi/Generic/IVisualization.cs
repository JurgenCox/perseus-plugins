using BaseLibS.Param;

namespace PerseusApi.Generic{
	/// <summary>
	/// Please do not implement this interface. It is still experimental.
	/// </summary>
	public interface IVisualization : IActivityWithHeading{
		IAnalysisResult AnalyzeData(IData[] mdata, Parameters param, ProcessInfo processInfo);
		Parameters GetParameters(IData[] mdata, ref string errString);
		int MinNumInput { get; }
		int MaxNumInput { get; }
		string GetInputName(int index);
	}
}