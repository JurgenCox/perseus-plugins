using System.Collections.Generic;

namespace PerseusApi.Network
{
    /// <summary>
    /// Node-link graph
    /// </summary>
    public interface IGraph : IEnumerable<INode>
    {
        #warning This API is experimental and might change frequently
        /// <summary>
        /// Edges
        /// </summary>
        IEnumerable<IEdge> Edges { get; }

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
    }
}
