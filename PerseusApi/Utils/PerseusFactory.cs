using System;
using System.Runtime.Remoting;
using PerseusApi.Matrix;

namespace PerseusApi.Utils {
	/// <summary>
	/// Factory class that provides static methods for creating instances of data items used in Perseus
	/// </summary>
	public class PerseusFactory2 {
		/// <summary>
		/// Creates an empty default implementation of <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData() {
			try {
				ObjectHandle o = Activator.CreateInstance("MatrixData", "PerseusLibS.Data.Matrix.MatrixData");
				return (IMatrixData) o.Unwrap();
			} catch (Exception) {}
			return null;
		}

		///// <summary>
		///// Create minimal initialized <see cref="IMatrixData"/>.
		///// </summary>
		///// <param name="values"></param>
		///// <returns></returns>
		//public static IMatrixData CreateMatrixData(float[,] values)
		//{
		//	return CreateNewMatrixData(values, Enumerable.Range(1, values.GetLength(0) + 1).Select(i => $"Column {i}").ToList());
		//}
		///// <summary>
		///// Create minimal initialized <see cref="IMatrixData"/>.
		///// </summary>
		///// <param name="values"></param>
		///// <param name="columnNames"></param>
		///// <returns></returns>
		//public static IMatrixData CreateMatrixData(float[,] values, List<string> columnNames)
		//{
		//	IMatrixData mdata = new MatrixData();
		//	mdata.Values.Set(values);
		//	mdata.ColumnNames = columnNames;
		//	var imputed = new BoolMatrixIndexer();
		//	imputed.Init(mdata.RowCount, mdata.ColumnCount);
		//	mdata.IsImputed = imputed;
		//	return mdata;
		//}
		//public static IDocumentData CreateNewDocumentData()
		//{
		//	return new DocumentData();
		//}
		//internal static ISequenceData CreateNewSequenceData()
		//{
		//	return new SequenceData();
		//}
		//public static INetworkData CreateNewNetworkData()
		//{
		//	return new NetworkData();
		//}
		//public static IDataWithAnnotationColumns CreateDataWithAnnotationColumns()
		//{
		//	return new DataWithAnnotationColumns();
		//}
		//public static InternalData Create(DataType2 dataType)
		//{
		//	switch (dataType)
		//	{
		//		case DataType2.Matrix:
		//			return (MatrixData)CreateNewMatrixData();
		//		case DataType2.Network:
		//			return (NetworkData)CreateNewNetworkData();
		//		default:
		//			throw new NotImplementedException($"Data type {dataType} not implemented!");
		//	}
		//}
	}
}