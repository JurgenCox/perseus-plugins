using System;

namespace PerseusApi.Network{
    /// <summary>
    /// Network edge connecting source and target
    /// </summary>
    public interface IEdge : IIdentifiable, ICloneable
    {
        #warning This API is experimental and might change frequently
        /// <summary>
        /// Edge source
        /// </summary>
        INode Source { get; }
        /// <summary>
        /// Edge target
        /// </summary>
        INode Target { get; }
    }
}