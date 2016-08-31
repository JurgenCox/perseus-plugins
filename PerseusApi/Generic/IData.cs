using System;
using BaseLibS.Api;

namespace PerseusApi.Generic{
	/// <summary>
	/// Generic data structure holding the data that flows through the network. 
	/// For example, this could be <code>IMatrixData</code>.
	/// </summary>
	public interface IData : INamedItem, IDisposable, ICloneable{
		/// <summary>
		/// A name that can be displayed as an alternative to <code>Name</code>.
		/// </summary>
		string AltName { get; set; }
		/// <summary>
		/// For data that has been read from a file this string will contain the file name. If it was originally 
		/// created by an activity (e.g. 'Create random matrix') it will contain the name of the creating activity.
		/// </summary>
		string Origin { get; set; }
		/// <summary>
		/// Specifies the date and time on which this item has been created.
		/// </summary>
		DateTime CreationDate { get; set; }
		/// <summary>
		/// Name of the user who created this data item.
		/// </summary>
		string User { get; set; }
		/// <summary>
		/// Creates an instance of the same data type.
		/// </summary>
		/// <returns>New instance.</returns>
		IData CreateNewInstance();
		/// <summary>
		/// Creates an instance of the specified data type.
		/// </summary>
		/// <returns>New instance.</returns>
		IData CreateNewInstance(DataType type);
		/// <summary>
		/// Clears up all data from this instance.
		/// </summary>
		void Clear();
	}
}