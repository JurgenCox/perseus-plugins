using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusApi.Network{
    /// <summary>
    /// Network collection table interface
    /// </summary>
	public interface INetworkData : IDataWithAnnotationColumns, IData, IEnumerable<INetworkInfo>
	{
        #warning This API is experimental and might change frequently
        /// <summary>
        /// Get network by index
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
		INetworkInfo this[int i] { get; }

        /// <summary>
        /// Add a network to the collection
        /// </summary>
        /// <param name="networks"></param>
        void AddNetworks(params INetworkInfo[] networks);
	}
}