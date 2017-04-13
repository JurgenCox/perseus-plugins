using System;
using System.Runtime.Remoting;
using PerseusApi.Matrix;

namespace PerseusApi.Utils
{
	/// <summary>
	/// Factory class that provides static methods for creating instances of data items used in Perseus
	/// </summary>
	public class PerseusFactory2
	{
		/// <summary>
		/// Creates an empty default implementation of IMatrixData
		/// </summary>
		/// The empty MatrixData object.
		/// <returns></returns>
		public static IMatrixData CreateMatrixData()
		{
			try
			{
				ObjectHandle o = Activator.CreateInstance("MatrixData", "PerseusLibS.Data.Matrix.MatrixData");
				return (IMatrixData) o.Unwrap();
			}
			catch (Exception)
			{
			}
			return null;
		}
	}
}