using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusApi.Sequence{
	public interface ISequenceMergeWithMatrix : ISequenceActivity, IMergeWithMatrix{
		void ProcessData(ISequenceData data, IMatrixData inMatrix, Parameters param, ref IData[] supplData, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the processing.
		/// </summary>
		/// <param name="data">The parameters might depend on the data.</param>
		/// <param name="inMatrix">and on the matrix as well.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(ISequenceData data, IMatrixData inMatrix, ref string errString);
	}
}