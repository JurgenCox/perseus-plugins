using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PerseusApi.Generic;
using PerseusApi.Utils;

namespace PerseusPluginLib.Test.Utils
{
    [TestFixture]
    public class PerseusUtilsTest
    {
        [Test]
        public void TestWriteMultiNumericColumnWithNulls()
        {
            var data = PerseusFactory.CreateDataWithAnnotationColumns();
            data.AddMultiNumericColumn("Test", "", new double[1][]);
            data.AddStringColumn("Test2", "", new string[1]);
            Assert.AreEqual(1, data.RowCount);
            var writer = new StreamWriter(new MemoryStream());
            PerseusUtils.WriteDataWithAnnotationColumns(data, writer);
        }

        [Test]
        public void TestReadEmptyMatrixFromFile()
        {
            var data = PerseusFactory.CreateDataWithAnnotationColumns();
            PerseusUtils.ReadDataWithAnnotationColumns(data, BaseTest.CreateProcessInfo(), () =>
            {
                var memstream = new MemoryStream(Encoding.UTF8.GetBytes("Node\n#!{Type}T\n"));
                return new StreamReader(memstream);
            }, "test", '\t');
            Assert.AreEqual(0, data.RowCount);
        }


	    [Test]
	    public void TestReadMatrixFromTabsepFileWithDoubleQuotes()
	    {
		    var mdata = PerseusFactory.CreateMatrixData();
		    var processInfo = new ProcessInfo(new Settings(), s => { }, i => { }, 1);
			var lines = new[]
			{
				"Col\tStringCol\tNumCol",
				"#!{Type}E\tT\tN",
				"-1.0\thello\t12",
				"1.0\t\"Actin family, ARP subfamily\";Actin family\t4",
				"2.0\t\"Regular quoted text\"\t4",
				"3.0\t\"Escaped\tseparator\"with extra\t4",
			};
			var bytes = Encoding.UTF8.GetBytes(string.Join("\n", lines));
			PerseusUtils.ReadMatrix(mdata, processInfo, () => new StreamReader(new MemoryStream(bytes)), "name", '\t');
			Assert.AreEqual("Col", mdata.ColumnNames.Single());
			Assert.AreEqual("StringCol", mdata.StringColumnNames.Single());
			Assert.AreEqual("NumCol", mdata.NumericColumnNames.Single());
			CollectionAssert.AreEqual(new [] {-1.0, 1.0, 2.0, 3.0}, mdata.Values.GetColumn(0).ToArray());
			CollectionAssert.AreEqual(new [] {"hello", "\"Actin family, ARP subfamily\";Actin family", "Regular quoted text", "\"Escaped\tseparator\"with extra"}, mdata.StringColumns.Single());
			CollectionAssert.AreEqual(new [] {12, 4, 4, 4}, mdata.NumericColumns.Single());

		}

	    [Test]
	    public void TestReadWriteMatrixRoundTrip()
	    {
		    var mdata = PerseusFactory.CreateMatrixData();
			mdata.AddStringColumn("StringCol", "", new []
			{
				"Regular text",
				"\"Regular quoted text\"",
				"\"Quote stops\" in the middle",
				"\"Escaped\tseparator\"with extra",
			});
			mdata.AddNumericColumn("NumCol", "", mdata.StringColumns[0].Select(_ => 1.0).ToArray());
			mdata.AddCategoryColumn("CatCol", "", mdata.StringColumns[0].Select(_ => new string[0]).ToArray());
		    string content;
		    using (var memory = new MemoryStream())
		    using (var writer = new StreamWriter(memory))
		    {
				PerseusUtils.WriteMatrix(mdata, writer);
				writer.Flush();
			    content = Encoding.UTF8.GetString(memory.ToArray());
		    }
		    var mdata2 = PerseusFactory.CreateMatrixData();
			var processInfo = new ProcessInfo(new Settings(), s => { }, i => { }, 1);
			PerseusUtils.ReadMatrix(mdata2, processInfo, () => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content))), "name", '\t');
		    var expected = mdata.StringColumns[0];
		    expected[1] = expected[1].Trim('\"');
			CollectionAssert.AreEqual(mdata.StringColumns[0], mdata2.StringColumns[0]);
		}

	}
}