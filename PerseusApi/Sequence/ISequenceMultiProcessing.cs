using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Sequence {
	public interface ISequenceMultiProcessing : ISequenceActivity, IMultiProcessing {
		ISequenceData ProcessData(ISequenceData[] inputData, Parameters param, ref IData[] supplData, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="inputData">The parameters might depend on the data matrices.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(ISequenceData[] inputData, ref string errString);
	}
}
