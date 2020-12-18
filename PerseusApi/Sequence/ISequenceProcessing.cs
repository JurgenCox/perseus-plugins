using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Sequence{
	public interface ISequenceProcessing : ISequenceActivity, IProcessing{
		void ProcessData(ISequenceData mdata, Parameters param, ref IData[] supplData, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="mdata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(ISequenceData mdata, ref string errString);
	}
}