using BaseLibS.Api.Generic;
using BaseLibS.Param;

namespace PerseusApi.Generic{
	public interface IUpload : IActivity{
		/// <summary>
		/// Define here the parameters that determine the specifics of the upload.
		/// </summary>
		/// <param name="errString">Set this to a value != null if an error occured. The error string will be displayed to the user.</param>
		/// <returns>The set of parameters.</returns>
		Parameters GetParameters(ref string errString);
        string[] HelpSupplTables { get; }
        int NumSupplTables { get; }
        string[] HelpDocuments { get; }
        int NumDocuments { get; }
    }
}