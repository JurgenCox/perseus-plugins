using BaseLibS.Api.Generic;

namespace PerseusApi.Generic{
	public interface IMergeWithMatrix : IActivityWithHeading{
		string HelpOutput { get; }
		string[] HelpSupplTables { get; }
		int NumSupplTables { get; }
		string[] HelpDocuments { get; }
		int NumDocuments { get; }
	}
}