using System.Collections.Generic;
using System.Linq;

namespace PerseusApi.Network{
	public static class NetworkExtensions{
		public static bool IsOrphan(this INode node){
			return node.InDegree + node.OutDegree == 0;
		}

		public static bool IsValid(this IGraph graph){
			IEnumerable<INode> edgeNodes = graph.Edges.SelectMany(edge => new[]{edge.Source, edge.Target});
			bool containsNodes = edgeNodes.All(graph.Contains);
			IEnumerable<IEdge> nodeEdges = graph.SelectMany(node => node.InEdges.Concat(node.OutEdges));
			bool containsEdges = nodeEdges.All(graph.Edges.Contains);
			return containsNodes && containsEdges;
		}

		public static INode[] AddNodes(this IGraph graph, int n){
			return Enumerable.Range(0, n).Select(_ => graph.AddNode()).ToArray();
		}
	}
}