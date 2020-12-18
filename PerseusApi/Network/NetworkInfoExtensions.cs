using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num;
using PerseusApi.Generic;

namespace PerseusApi.Network{
	public static class NetworkInfoExtensions{
		/// <summary>
		/// Remove the edges from the network, returning orphaned nodes (no in/out edges).
		/// </summary>
		/// <param name="network"></param>
		/// <param name="badEdges"></param>
		/// <param name="orphans"></param>
		public static void RemoveEdges(this INetworkInfo network, ICollection<IEdge> badEdges,
			out HashSet<INode> orphans){
			network.Graph.RemoveEdges(badEdges, out orphans);
			RepairEdgeIndexAndEdgeTable(network, badEdges);
		}

		/// <summary>
		/// Remove the nodes and their dangling edges from the network.
		/// </summary>
		/// <param name="network"></param>
		/// <param name="badNodes"></param>
		public static void RemoveNodes(this INetworkInfo network, ICollection<INode> badNodes){
			network.Graph.RemoveNodes(badNodes, out HashSet<IEdge> danglingEdges, out _);
			// make nodes consistent
			INode[] good = network.Graph.Except(badNodes).ToArray();
			int[] goodRows = good.Select(node => network.NodeIndex[node]).ToArray();
			network.NodeTable.ExtractRows(goodRows);
			foreach (INode badNode in badNodes){
				network.NodeIndex.Remove(badNode);
			}
			foreach ((INode node, int newRow) in good.Select((node, i) => (node, i))){
				network.NodeIndex[node] = newRow;
			}
			RepairEdgeIndexAndEdgeTable(network, danglingEdges);
		}

		/// <summary>
		/// Repairs the edge index and edge table after edges are removed from the graph.
		/// </summary>
		/// <param name="network"></param>
		/// <param name="badEdges"></param>
		private static void RepairEdgeIndexAndEdgeTable(INetworkInfo network, IEnumerable<IEdge> badEdges){
			IDictionary<IEdge, int> edgeIndex = network.EdgeIndex;
			IReadOnlyCollection<IEdge> goodEdges = network.Graph.Edges;
			int[] goodEdgeRows = goodEdges.Select(edge => edgeIndex[edge]).ToArray();
			network.EdgeTable.ExtractRows(goodEdgeRows);
			foreach (IEdge edge in badEdges){
				edgeIndex.Remove(edge);
			}
			foreach ((IEdge edge, int newIndex) in goodEdges.Select((edge, i) => (edge, i))){
				edgeIndex[edge] = newIndex;
			}
		}

		public static void SortByNodeColumn(this INetworkInfo network){
			var nodes = network.NodeTable.GetStringColumn("node");
			int[] order = ArrayUtils.Order(nodes);
			Dictionary<int, INode> reverseNodeIndex = network.NodeIndex.ToDictionary(kv => kv.Value, kv => kv.Key);
			for (int newIndex = 0; newIndex < order.Length; newIndex++){
				int oldIndex = order[newIndex];
				network.NodeIndex[reverseNodeIndex[oldIndex]] = newIndex;
			}
			network.NodeTable.ExtractRows(order);
		}

		public static void RenameNode(this INetworkInfo network, int row, string name, string nodeColumn = "Node",
			string sourceColumn = "Source", string targetColumn = "Target"){
			INode node = network.Graph.Single(n => network.NodeIndex[n] == row);
			network.NodeTable.GetStringColumn(nodeColumn)[row] = name;
			foreach (IEdge inEdge in node.InEdges){
				network.EdgeTable.GetStringColumn(targetColumn)[network.EdgeIndex[inEdge]] = name;
			}
			foreach (IEdge outEdge in node.OutEdges){
				network.EdgeTable.GetStringColumn(sourceColumn)[network.EdgeIndex[outEdge]] = name;
			}
		}

		public static IEdge AddEdge(this INetworkInfo network, INode source, INode target){
			return network.AddEdge(network.NodeIndex[source], network.NodeIndex[target]);
		}

		public static IEdge AddEdge(this INetworkInfo network, int sourceRow, int targetRow){
			IGraph graph = network.Graph;
			var sourceNode = graph.Single(n => network.NodeIndex[n] == sourceRow);
			var targetNode = graph.Single(n => network.NodeIndex[n] == targetRow);
			IEdge edge = graph.AddEdge(sourceNode, targetNode);
			int nEdges = graph.NumberOfEdges;
			network.EdgeIndex[edge] = nEdges - 1;
			string[] nodes = network.NodeTable.GetStringColumn("Node");
			IDataWithAnnotationColumns edgeTable = network.EdgeTable;
			int sourceCol = edgeTable.StringColumnNames.FindIndex(col => col.ToLower().Equals("source"));
			List<string> sources = edgeTable.StringColumns[sourceCol].ToList();
			sources.Add(nodes[sourceRow]);
			edgeTable.StringColumns[sourceCol] = sources.ToArray();
			int targetCol = edgeTable.StringColumnNames.FindIndex(col => col.ToLower().Equals("target"));
			List<string> targets = edgeTable.StringColumns[targetCol].ToList();
			targets.Add(nodes[targetRow]);
			edgeTable.StringColumns[targetCol] = targets.ToArray();
			return edge;
		}

		public static INode AddNode(this INetworkInfo network, string name){
			INode node = network.Graph.AddNode();
			int nNodes = network.Graph.NumberOfNodes;
			network.NodeIndex[node] = nNodes - 1;
			int nodeCol = network.NodeTable.StringColumnNames.FindIndex(col => col.ToLower().Equals("node"));
			List<string> nodes = network.NodeTable.StringColumns[nodeCol].ToList();
			nodes.Add(name);
			network.NodeTable.StringColumns[nodeCol] = nodes.ToArray();
			return node;
		}
	}
}