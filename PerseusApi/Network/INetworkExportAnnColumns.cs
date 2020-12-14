using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Network
{
	/// <summary>
	/// Network export
	/// </summary>
	public interface INetworkExportAnnColumns : INetworkActivity, IExport
	{
		/// <summary>
		/// Export the network given the parameters from <see cref="GetParameters"/>.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="ndata"></param>
		/// <param name="processInfo"></param>
		void Export(Parameters parameters, INetworkDataAnnColumns ndata, ProcessInfo processInfo);

		/// <summary>
		/// Define here the parameters that determine the specifics of the export.
		/// </summary>
		/// <param name="ndata">The parameters might depend on the data matrix.</param>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(INetworkDataAnnColumns ndata, ref string errString);
	}
}