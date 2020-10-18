using BaseLibS.Api.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Matrix{
	/// <summary>
	/// Marker interface indicating activities that operate on <code>IMatrixData</code>. There are currently five 
	/// <code>IMatrixActivity</code> types implemented: <code>IMatrixUpload</code>, <code>IMatrixProcessing</code>, 
	/// <code>IMatrixAnalysis</code>, <code>IMatrixCombination</code> and <code>IMatrixExport</code>.
	/// </summary>
	public interface IMatrixActivity : IActivity {}
}