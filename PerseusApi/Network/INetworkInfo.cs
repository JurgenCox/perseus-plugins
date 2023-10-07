using System;
using System.Collections.Generic;
using System.IO;
using PerseusApi.Generic;

namespace PerseusApi.Network{
	/// <summary>
	/// Network interface. See also <see cref="INetworkData"/> for a collection of networks.
	/// </summary>
	public interface INetworkInfo : IIdentifiable, ICloneable{
		/// <summary>
		/// Node Table
		/// </summary>
		DataWithAnnotationColumns NodeTable{ get; }

		/// <summary>
		/// Edge Table
		/// </summary>
		DataWithAnnotationColumns EdgeTable{ get; }

		/// <summary>
		/// Maps the node from the <see cref="Graph"/> to the corresponding row in the <see cref="NodeTable"/>.
		/// </summary>
		Dictionary<INode, int> NodeIndex{ get; }

		/// <summary>
		/// Maps the edge from the <see cref="Graph"/> to the corresponding row in the <see cref="EdgeTable"/>.
		/// </summary>
		Dictionary<IEdge, int> EdgeIndex{ get; }

		/// <summary>
		/// Node-link graph
		/// </summary>
		Graph Graph{ get; }

		/// <summary>
		/// Network name
		/// </summary>
		string Name{ get; set; }

		/// <summary>
		/// Check if table and graph representation are consistent
		/// </summary>
		/// <param name="errString"></param>
		/// <returns></returns>
		bool IsConsistent(out string errString);
		void Write(BinaryWriter writer);
	}
}