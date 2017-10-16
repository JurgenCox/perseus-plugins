using System.Collections.Generic;

namespace PerseusApi.Network
{
    /// <summary>
    /// Node-link graph
    /// </summary>
    public interface IGraph : IReadOnlyCollection<INode>
    {
        #warning This API is experimental and might change frequently
        /// <summary>
        /// Edges
        /// </summary>
        IReadOnlyCollection<IEdge> Edges { get; }

        /// <summary>
        /// Add node and return reference.
        /// </summary>
        /// <returns></returns>
        INode AddNode();

        /// <summary>
        /// Add edge between nodes and return reference.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        IEdge AddEdge(INode source, INode target);

        /// <summary>
        /// Number of nodes in the graph
        /// </summary>
        int NumberOfNodes { get; }

        /// <summary>
        /// Number of edges in the graph
        /// </summary>
        int NumberOfEdges { get; }

        /// <summary>
        /// Remove the nodes from the graph. Removes dangling edges.
        /// </summary>
        /// <param name="nodes"></param>
        void RemoveNodes(params INode[] nodes);
        
        /// <summary>
        /// Remove the nodes from the graph. Removes dangling edges.
        /// </summary>
        /// <param name="nodes"></param>
        void RemoveNodes(IEnumerable<INode> nodes);

        /// <summary>
        /// Remove the nodes from the graph. Removes dangling edges.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="danglingEdges">dangling edges that were removed</param>
        /// <param name="orphans">nodes orphaned (no remaining edges) after removing dangling edges</param>
        void RemoveNodes(IEnumerable<INode> nodes, out HashSet<IEdge> danglingEdges, out HashSet<INode> orphans);

        /// <summary>
        /// Remove edges from the graph.
        /// </summary>
        /// <param name="edges"></param>
        void RemoveEdges(params IEdge[] edges);

        /// <summary>
        /// Remove edges from the graph.
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="orphans">Orphaned nodes without any remaining edges.</param>
        void RemoveEdges(IEnumerable<IEdge> edges, out HashSet<INode> orphans);
    }
}
