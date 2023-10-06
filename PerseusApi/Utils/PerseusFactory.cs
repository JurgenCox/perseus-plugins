using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLibS.Num.Matrix;
using PerseusApi.Document;
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
			object o = Activator.CreateInstance(Type.GetType("PerseusLibS.Data.Matrix.MatrixData"));
			return (IMatrixData) o;
		}

		/// <summary>
		/// Create minimally initialized <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData(double[,] values){
			return CreateMatrixData(values,
				Enumerable.Range(0, values.GetLength(1)).Select(i => $"Column {i + 1}").ToList());
		}

		/// <summary>
		/// Create minimally initialized <see cref="IMatrixData"/>.
		/// </summary>
		public static IMatrixData CreateMatrixData(double[,] values, List<string> columnNames){
			IMatrixData mdata = CreateMatrixData();
			mdata.Values.Set(values);
			mdata.ColumnNames = columnNames;
			BoolMatrixIndexer imputed = new BoolMatrixIndexer();
			imputed.Init(mdata.RowCount, mdata.ColumnCount);
			mdata.IsImputed = imputed;
			return mdata;
		}

		/// <summary>
		/// Creates an empty default implementation of <see cref="IDocumentData"/>.
		/// </summary>
		public static IDocumentData CreateDocumentData(){
			object o = Activator.CreateInstance(Type.GetType("PerseusLibS.Data.DocumentData"));
			return (IDocumentData) o;
		}

		/// <summary>
		/// Creates an empty default implementation of <see cref="INetworkData"/>.
		/// </summary>
		public static INetworkData CreateNetworkData(){
			object o = Activator.CreateInstance(Type.GetType("PerseusLibS.Data.Network.NetworkData"));
			return (INetworkData) o;
		}

		/// <summary>
		/// Creates a default implementation of <see cref="INetworkInfo"/> from the given graph
		/// and node/edge tables and indices.
		/// </summary>
		public static INetworkInfo CreateNetworkInfo(IGraph graph, IDataWithAnnotationColumns nodeTable,
			Dictionary<INode, int> nodeIndex, IDataWithAnnotationColumns edgeTable, Dictionary<IEdge, int> edgeIndex,
			string name, Guid guid){
			var networkInfoTypeName =
				Assembly.CreateQualifiedName("PerseusLibS", "PerseusLibS.Data.Network.NetworkInfo");
			var type = Type.GetType(networkInfoTypeName);
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
			var graphTypeName = Assembly.CreateQualifiedName("PerseusLibS", "PerseusLibS.Data.Network.Graph");
			var type = Type.GetType(graphTypeName);
			if (type == null){
				throw new Exception($"Cannot load type {graphTypeName}.");
			}
			return (IGraph) Activator.CreateInstance(type);
		}

		/// <summary>
		/// Creates an default implementation of <see cref="IGraph"/> from nodes and edges.
		/// </summary>
		public static IGraph CreateGraph(IEnumerable<INode> nodes, IEnumerable<IEdge> edges){
			var graphTypeName = "PerseusLibS.Data.Network.Graph";
			var type = Type.GetType(graphTypeName);
			if (type == null){
				throw new Exception($"Cannot load type {graphTypeName}.");
			}
			return (IGraph) Activator.CreateInstance(type, nodes, edges);
		}
	}
}