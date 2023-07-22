using System;
using System.IO;
namespace PerseusApi.Generic{
	/// <summary>
	/// The output of a generic <code>IAnalysis</code>. It contains the data for this IAnalysis which is serialized with the session. 
	/// </summary>
	public interface IAnalysisResult{
		/// <summary>
		/// Heading to be displayed on the tab page created for the visual component of this <code>IAnalysisResult</code>.
		/// </summary>
		/// <returns></returns>
		string Heading { get; }
		/// <summary>
		/// Creates the visual component.
		/// </summary>
		/// <param name="updateStatus">Callback for displaying text in the status bar.</param>
		/// <param name="newData">A new <code>IData</code> can be put here interactively into the workflow.</param>
		/// <returns>
		/// The visual component. Usually this is a <code>Control</code> from winforms. Return type is object so that this 
		/// interface can be used on the server side.
		/// </returns>
		object CreateUiElement(Action<string> updateStatus, Action<IData> newData);
		void Write(BinaryWriter writer);
		void Read(BinaryReader reader, Func<BinaryReader, DataType, IData> readData);
	}
}