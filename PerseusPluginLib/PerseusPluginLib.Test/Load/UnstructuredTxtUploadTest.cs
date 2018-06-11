using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Load;

namespace PerseusPluginLib.Test.Load
{
	[TestFixture]
	public class UnstructuredTxtUploadTest
	{
		[Test]
		public void TestLoadingRawTxt()
		{
			var lines = new[]
			{
				"Col_1 Col_2",
				"a b",
			};
			var tmpFile = Path.GetTempFileName();
			File.WriteAllLines(tmpFile, lines);
			var upload = new UnstructuredTxtUpload();
			var errString = string.Empty;
			var parameters = upload.GetParameters(ref errString);
			Assert.IsTrue(string.IsNullOrEmpty(errString));
			parameters.GetParam<string>("File").Value = tmpFile;
			var subparam = parameters.GetParamWithSubParams<bool>("Split into columns");
			subparam.Value = true;
			subparam.GetSubParameters().GetParam<int>("Separator").Value = 2;
			var mdata = PerseusFactory.CreateMatrixData();
			IMatrixData[] suppl = null;
			IDocumentData[] supplD = null;
			upload.LoadData(mdata, parameters, ref suppl, ref supplD, new ProcessInfo(new Settings(), s => { }, i => { }, 1));
			CollectionAssert.AreEqual(new [] {"Col_1", "Col_2"}, mdata.StringColumnNames);
			CollectionAssert.AreEqual(new [] {"a"}, mdata.StringColumns[0]);
			CollectionAssert.AreEqual(new [] {"b"}, mdata.StringColumns[1]);
		}
		[Test]
		public void TestLoadingRawTxtFromGzip()
		{
			var lines = new[]
			{
				"Col_1 Col_2",
				"a b",
			};
			var tmpFile = Path.GetTempFileName() + ".gz";
			using (var memory = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines))))
			using (var outFile = File.Create(tmpFile))
			using (var gzip = new GZipStream(outFile, CompressionMode.Compress))
			{
				memory.CopyTo(gzip);
			}
			var upload = new UnstructuredTxtUpload();
			var errString = string.Empty;
			var parameters = upload.GetParameters(ref errString);
			Assert.IsTrue(string.IsNullOrEmpty(errString));
			parameters.GetParam<string>("File").Value = tmpFile;
			var subparam = parameters.GetParamWithSubParams<bool>("Split into columns");
			subparam.Value = true;
			subparam.GetSubParameters().GetParam<int>("Separator").Value = 2;
			var mdata = PerseusFactory.CreateMatrixData();
			IMatrixData[] suppl = null;
			IDocumentData[] supplD = null;
			upload.LoadData(mdata, parameters, ref suppl, ref supplD, new ProcessInfo(new Settings(), s => { }, i => { }, 1));
			CollectionAssert.AreEqual(new [] {"Col_1", "Col_2"}, mdata.StringColumnNames);
			CollectionAssert.AreEqual(new [] {"a"}, mdata.StringColumns[0]);
			CollectionAssert.AreEqual(new [] {"b"}, mdata.StringColumns[1]);
		}
	}
}
