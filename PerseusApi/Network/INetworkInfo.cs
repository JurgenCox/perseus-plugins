using System;
using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Network{
    /// <summary>
    /// Network interface. See also <see cref="INetworkData"/> for a collection of networks.
    /// </summary>
	public interface INetworkInfo : IIdentifiable, ICloneable
	{
        #warning This API is experimental and might change frequently
        /// <summary>
        /// Node Table
        /// </summary>
	    IDataWithAnnotationColumns NodeTable { get; }
        /// <summary>
        /// Edge Table
        /// </summary>
	    IDataWithAnnotationColumns EdgeTable { get; }

        /// <summary>
        /// Maps the node from the <see cref="Graph"/> to the corresponding row in the <see cref="NodeTable"/>.
        /// </summary>
	    IDictionary<INode, int> NodeIndex { get; }
        /// <summary>
        /// Maps the edge from the <see cref="Graph"/> to the corresponding row in the <see cref="EdgeTable"/>.
        /// </summary>
	    IDictionary<IEdge, int> EdgeIndex { get; }

        /// <summary>
        /// Node-link graph
        /// </summary>
        IGraph Graph { get; }
        /// <summary>
        /// Network name
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Contais arbitrary meta-data for the networks.
        /// </summary>
        Dictionary<string, object> MetaData { get; set; }
	}
}