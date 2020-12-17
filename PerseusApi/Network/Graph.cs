using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PerseusApi.Network{
	/// <summary>
	/// Graph data structure with nodes and edges. Provides consistent removal of
	/// nodes and edges.
	/// 
	/// Graph is not marked as [Serializable] since default binary serialization produces
	/// too many references, limiting the size of network that can be serialized.
	/// <see cref="NetworkInfo"/> implements a custom binary serialization that can handle
	/// large graphs.
	/// </summary>
	public class Graph : IGraph{
		public IReadOnlyCollection<INode> Nodes => nodes;
		private readonly List<INode> nodes;
		public readonly List<IEdge> edges;
		private readonly Func<Guid> guidFactory;
		public IReadOnlyCollection<IEdge> Edges => edges;
		public int Count => NumberOfNodes;

		public Graph() : this(new List<INode>(), new List<IEdge>()){ }

		public Graph(IEnumerable<INode> nodes, IEnumerable<IEdge> edges) : this(nodes, edges, Guid.NewGuid){ }

		public Graph(IEnumerable<INode> nodes, IEnumerable<IEdge> edges, Func<Guid> guidFactory){
			this.nodes = nodes.ToList();
			this.edges = edges.ToList();
			this.guidFactory = guidFactory;
		}

		public IEnumerator<INode> GetEnumerator(){
			return Nodes.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}

		public INode AddNode(){
			Node node = new Node(guidFactory());
			nodes.Add(node);
			return node;
		}

		public IEdge AddEdge(INode source, INode target){
			IEdge edge = new Edge(source, target);
			source.OutEdges.Add(edge);
			target.InEdges.Add(edge);
			edges.Add(edge);
			return edge;
		}

		public void RemoveNodes(params INode[] nodes1){
			RemoveNodes(nodes1, out _, out _);
		}

		public void RemoveNodes(IEnumerable<INode> nodes1){
			RemoveNodes(nodes1, out _, out _);
		}

		public void RemoveNodes(IEnumerable<INode> nodes, out HashSet<IEdge> danglingEdges,
			out HashSet<INode> orphanedNodes){
			HashSet<INode> badNodes = new HashSet<INode>(nodes);
			danglingEdges = new HashSet<IEdge>(badNodes.SelectMany(node => node.InEdges.Concat(node.OutEdges)));
			RemoveEdges(danglingEdges, out orphanedNodes);
			this.nodes.RemoveAll(badNodes.Contains);
		}

		public void RemoveEdges(params IEdge[] edges1){
			RemoveEdges(edges1, out _);
		}

		public void RemoveEdges(IEnumerable<IEdge> edges, out HashSet<INode> orphans){
			orphans = new HashSet<INode>();
			HashSet<IEdge> badEdges = new HashSet<IEdge>();
			foreach (IEdge edge in edges){
				INode source = edge.Source;
				source.OutEdges.Remove(edge);
				if (source.IsOrphan()){
					orphans.Add(source);
				}
				INode target = edge.Target;
				target.InEdges.Remove(edge);
				if (target.IsOrphan()){
					orphans.Add(target);
				}
				badEdges.Add(edge);
			}
			this.edges.RemoveAll(badEdges.Contains);
		}

		public int NumberOfNodes => Nodes.Count;
		public int NumberOfEdges => edges.Count;

		public IGraph Clone(out Dictionary<INode, INode> nodeMapping, out Dictionary<IEdge, IEdge> edgeMapping){
			Graph graph = new Graph();
			nodeMapping = new Dictionary<INode, INode>();
			edgeMapping = new Dictionary<IEdge, IEdge>();
			foreach (INode oldSource in this){
				if (!nodeMapping.ContainsKey(oldSource)){
					INode ns = graph.AddNode();
					nodeMapping[oldSource] = ns;
				}
				INode newSource = nodeMapping[oldSource];
				foreach (IEdge edge in oldSource.OutEdges){
					INode oldTarget = edge.Target;
					if (!nodeMapping.ContainsKey(oldTarget)){
						INode nt = graph.AddNode();
						nodeMapping[oldTarget] = nt;
					}
					INode newTarget = nodeMapping[oldTarget];
					IEdge newEdge = graph.AddEdge(newSource, newTarget);
					edgeMapping[edge] = newEdge;
				}
			}
			return graph;
		}
	}
}