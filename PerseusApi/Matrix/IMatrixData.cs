using BaseLibS.Num.Matrix;
using PerseusApi.Generic;

namespace PerseusApi.Matrix{
	/// <summary>
	///     The data structure representing an augmented data matrix which is the main data object that is flowing through
	///     the Perseus workflow. Note that plugin programmers are not supposed to write implementations of <code>IMatrixData</code>.
	///     The interface only serves to encapsulate the complexity of the implementation for the purpose of plugin programming.
	/// </summary>
	public interface IMatrixData : IData, IDataWithAnnotationRows, IDataWithAnnotationColumns{
		MatrixIndexer Values { get; set; }
		MatrixIndexer Quality { get; set; }
		IBoolMatrixIndexer IsImputed { get; set; }
		string QualityName { get; set; }
		bool QualityBiggerIsBetter { get; set; }
		bool HasQuality { get; }
	}
}