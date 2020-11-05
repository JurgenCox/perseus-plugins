﻿using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Network
{
    /// <summary>
    /// Process a network.
    /// </summary>
	public interface INetworkProcessingAnnColumns : INetworkActivity, IProcessing
    {
        /// <summary>
        /// Process a network given the parameters specified in <see cref="GetParameters"/>.
        /// </summary>
        /// <param name="ndata"></param>
        /// <param name="param"></param>
        /// <param name="supplData"></param>
        /// <param name="processInfo"></param>
		void ProcessData(INetworkDataAnnColumns ndata, Parameters param, ref IData[] supplData, ProcessInfo processInfo);

        /// <summary>
        /// Define here the parameters that determine the specifics of the processing.
        /// </summary>
        /// <param name="ndata">The parameters might depend on the data matrix.</param>
        /// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
        /// <returns>The set of parameters.</returns>
        Parameters GetParameters(INetworkDataAnnColumns ndata, ref string errString);
    }
}
