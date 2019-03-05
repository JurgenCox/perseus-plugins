using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Load;
using PerseusPluginLib.Rearrange;

namespace PerseusPluginLib.Test.Rearrange
{
	[TestFixture]
	public class TransposeTest
	{
		[Test]
		public void TestIsConsistent()
		{
			var random = new CreateRandomMatrix();
			var errString = string.Empty;
			var parameters = random.GetParameters(ref errString);
			Assert.IsTrue(string.IsNullOrEmpty(errString), errString);
			var mdata = PerseusFactory.CreateMatrixData();
			IMatrixData[] suppl = null;
			IDocumentData[] supplD = null;
			var pInfo = new ProcessInfo(new Settings(), s => { }, i => { }, 1);
			random.LoadData(mdata, parameters, ref suppl, ref supplD, pInfo);
			Assert.IsTrue(string.IsNullOrEmpty(pInfo.ErrString), pInfo.ErrString);
			Assert.IsTrue(mdata.IsConsistent(out string randCons), randCons);

			var transpose = new Transpose();
			parameters = transpose.GetParameters(mdata, ref errString);
			Assert.IsTrue(string.IsNullOrEmpty(errString), errString);
			transpose.ProcessData(mdata, parameters, ref suppl, ref supplD, pInfo);
			Assert.IsTrue(string.IsNullOrEmpty(pInfo.ErrString), pInfo.ErrString);
			Assert.IsTrue(mdata.IsConsistent(out var transCons), transCons);
		}	
	}
}