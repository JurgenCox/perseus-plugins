using System.Linq;
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
	public class ChangeColumnTypeTest
	{
		[Test]
		public void TestNumericToMainWithStringRow()
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
			var values = Enumerable.Range(0, mdata.RowCount).Select(i => (double) i).ToArray();
			mdata.AddNumericColumn("Test", "", values);
			mdata.AddStringRow("TestRow", "", mdata.ColumnNames.ToArray());
			Assert.IsTrue(string.IsNullOrEmpty(pInfo.ErrString), pInfo.ErrString);
			Assert.IsTrue(mdata.IsConsistent(out string randCons), randCons);

			var processing = new ChangeColumnType();
			parameters = processing.GetParameters(mdata, ref errString);
			Assert.IsTrue(string.IsNullOrEmpty(errString), errString);
			var param = parameters.GetParamWithSubParams<int>("Source type");
			param.Value = 1;
			var subparam = param.GetSubParameters();
			subparam.GetParam<int[]>("Columns").Value = new[] { 0 };
			subparam.GetParam<int>("Target type").Value = 1;
			processing.ProcessData(mdata, parameters, ref suppl, ref supplD, pInfo);
			Assert.IsTrue(string.IsNullOrEmpty(pInfo.ErrString), pInfo.ErrString);
			Assert.IsTrue(mdata.IsConsistent(out var isConsistent), isConsistent);
			Assert.AreEqual("Test", mdata.ColumnNames.Last());
			CollectionAssert.AreEqual(values, mdata.Values.GetColumn(mdata.ColumnCount - 1).ToArray());
		}	
	}
}