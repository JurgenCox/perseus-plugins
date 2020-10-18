using BaseLibS.Api.Generic;

namespace PerseusApi.Generic {
	public interface IMultiProcessing : IActivityWithHeading {
		string HelpOutput { get; }
		string[] HelpSupplTables { get; }
		int NumSupplTables { get; }
		string[] HelpDocuments { get; }
		int NumDocuments { get; }
		int MinNumInput { get; }
		int MaxNumInput { get; }
		string GetInputName(int index);
	}
}
