using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using BaseLibS.Num.Matrix;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;

namespace PerseusApi.Utils {
	/// <summary>
	/// Factory class that provides static methods for creating instances of data items used in Perseus
	/// </summary>
	public class PerseusFactory {
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

		/// <summary>
		/// Create minimally initialized <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData(float[,] values) {
			return CreateMatrixData(values, Enumerable.Range(1, values.GetLength(0) + 1).Select(i => $"Column {i}").ToList());
		}

		/// <summary>
		/// Create minimally initialized <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData(float[,] values, List<string> columnNames) {
			IMatrixData mdata = CreateMatrixData();
			mdata.Values.Set(values);
			mdata.ColumnNames = columnNames;
			var imputed = new BoolMatrixIndexer();
			imputed.Init(mdata.RowCount, mdata.ColumnCount);
			mdata.IsImputed = imputed;
			return mdata;
		}

		/// <summary>
		/// Creates an empty default implementation of <see cref="IDocumentData"/>.
		/// </summary>
		public static IDocumentData CreateDocumentData() {
			try {
				ObjectHandle o = Activator.CreateInstance("DocumentData", "PerseusLibS.Data.Document.DocumentData");
				return (IDocumentData)o.Unwrap();
			} catch (Exception) { }
			return null;
		}
		/// <summary>
		/// Creates an empty default implementation of <see cref="INetworkData"/>.
		/// </summary>
		public static INetworkData CreateNetworkData() {
			try {
				ObjectHandle o = Activator.CreateInstance("NetworkData", "PerseusLibS.Data.Network.NetworkData");
				return (INetworkData)o.Unwrap();
			} catch (Exception) { }
			return null;
		}
		/// <summary>
		/// Creates an empty default implementation of <see cref="IDataWithAnnotationColumns"/>.
		/// </summary>
		public static IDataWithAnnotationColumns CreateDataWithAnnotationColumns() {
			try {
				ObjectHandle o = Activator.CreateInstance("DataWithAnnotationColumns", "PerseusLibS.Data.DataWithAnnotationColumns");
				return (IDataWithAnnotationColumns)o.Unwrap();
			} catch (Exception) { }
			return null;
		}
	}
}