using System;
using BaseLibS.Graph;

namespace PerseusApi.Generic{
    /// <summary>
    /// Generic data structure holding the data that flows through the network. 
    /// For example, this could be <code>IMatrixData</code>.
    /// </summary>
    public interface IData : IDisposable, ICloneable
    {
        /// <summary>
        /// This the id of the data object.
        /// </summary>
        int Id { get; set; }
        /// <summary>
        /// This is the DataType of the object.
        /// </summary>
        DataType DataType { get; }
        /// <summary>
        /// Visual representation of the data in the workflow.
        /// </summary>
        Bitmap2 WorkflowImage { get; }
        /// <summary>
        /// Name of the DataType.
        /// </summary>
        string TypeName { get; }
        /// <summary>
        /// This is the name that e.g. appears in drop-down menus.
        /// </summary>
        string Name { get; set; }
		/// <summary>
		/// The context help that will appear in tool tips etc. 
		/// </summary>
		string Description { get; set; }
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
		/// Obsolete: Use <see cref="PerseusApi.Utils.PerseusFactory"/> instead.
		/// </summary>
		/// <returns>New instance.</returns>
		[Obsolete("Use PerseusFactory")]
		IData CreateNewInstance();
		/// <summary>
		/// Creates an instance of the specified data type.
		/// Obsolete: Use <see cref="PerseusApi.Utils.PerseusFactory"/> instead.
		/// </summary>
		/// <returns>New instance.</returns>
		[Obsolete("Use PerseusFactory")]
		IData CreateNewInstance(DataType type);
		/// <summary>
		/// Clears up all data from this instance.
		/// </summary>
		void Clear();
	}
}