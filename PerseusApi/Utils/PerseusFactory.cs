using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using BaseLibS.Num.Matrix;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;

namespace PerseusApi.Utils{
	/// <summary>
	/// Factory class that provides static methods for creating instances of data items used in Perseus
	/// </summary>
	public class PerseusFactory{
		/// <summary>
		/// Creates an empty default implementation of <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData(){
			ObjectHandle o = Activator.CreateInstance("PerseusLibS", "PerseusLibS.Data.Matrix.MatrixData");
			return (IMatrixData) o.Unwrap();
		}

		/// <summary>
		/// Create minimally initialized <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData(double[,] values, List<string> columnNames){
			ObjectHandle o = Activator.CreateInstance("PerseusLibS", "PerseusLibS.Data.Matrix.MatrixData");
			IMatrixData mdata=(IMatrixData)o.Unwrap();
			mdata.Values.Set(values);
			mdata.ColumnNames = columnNames;
			BoolMatrixIndexer imputed = new BoolMatrixIndexer();
			imputed.Init(mdata.RowCount, mdata.ColumnCount);
			mdata.IsImputed = imputed;
			return mdata;
		}

		/// <summary>
		/// Creates a default implementation of <see cref="INetworkInfo"/> from the given graph
		/// and node/edge tables and indices.
		/// </summary>
		public static INetworkInfo CreateNetworkInfo(IGraph graph, IDataWithAnnotationColumns nodeTable,
			Dictionary<INode, int> nodeIndex, IDataWithAnnotationColumns edgeTable, Dictionary<IEdge, int> edgeIndex,
			string name, Guid guid){
			string networkInfoTypeName =
				Assembly.CreateQualifiedName("PerseusLibS", "PerseusLibS.Data.Network.NetworkInfo");
			Type type = Type.GetType(networkInfoTypeName);
			if (type == null){
				throw new Exception($"Cannot load type {networkInfoTypeName}.");
			}
			return (INetworkInfo) Activator.CreateInstance(type, graph, nodeTable, nodeIndex, edgeTable, edgeIndex,
				name, guid);
		}

		/// <summary>
		/// Creates and default implementation of <see cref="IGraph"/> without nodes or edges.
		/// </summary>
		public static IGraph CreateGraph(){
			string graphTypeName = Assembly.CreateQualifiedName("PerseusLibS", "PerseusLibS.Data.Network.Graph");
			Type type = Type.GetType(graphTypeName);
			if (type == null){
				throw new Exception($"Cannot load type {graphTypeName}.");
			}
			return (IGraph) Activator.CreateInstance(type);
		}

		/// <summary>
		/// Creates an empty default implementation of <see cref="IDataWithAnnotationColumns"/>.
		/// </summary>
		public static IDataWithAnnotationColumns CreateDataWithAnnotationColumns(){
			ObjectHandle o = Activator.CreateInstance("PerseusLibS", "PerseusLibS.Data.DataWithAnnotationColumns");
			return (IDataWithAnnotationColumns) o.Unwrap();
		}
	}
}