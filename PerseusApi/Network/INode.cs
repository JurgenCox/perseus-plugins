using System;
using System.Collections.Generic;

namespace PerseusApi.Network{
    /// <summary>
    /// Graph node
    /// </summary>
    public interface INode : IIdentifiable, ICloneable
    {
        #warning This API is experimental and might change frequently
        /// <summary>
        /// Incoming edges
        /// </summary>
        List<IEdge> InEdges { get; }
        /// <summary>
        /// Outgoing edges
        /// </summary>
        List<IEdge> OutEdges { get; }

        /// <summary>
        /// Neighboring nodes
        /// </summary>
        IEnumerable<INode> Neighbors  { get; } 

        /// <summary>
        /// Degree of incoming edges
        /// </summary>
        int InDegree { get; }
        /// <summary>
        /// Degree of outgoing edges
        /// </summary>
        int OutDegree { get; }
    }
}