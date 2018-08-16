using System.Collections.Generic;
using NUnit.Framework;
using PerseusApi.Utils;
using PerseusPluginLib.Join;

namespace PerseusPluginLib.Test.Join
{
	[TestFixture]
	public class ConcatenateTest
    {
	    [Test]
	    public void TestConcatenateMainColumns()
	    {
			var mdata = PerseusFactory.CreateMatrixData(new[,] { { 0.1 }, { 1 } }, new List<string> { "A" });
			var mdata2 = PerseusFactory.CreateMatrixData(new[,] { { 2.1 }, { 4 }, {3.1} }, new List<string> { "B" });
			var mdata3 = PerseusFactory.CreateMatrixData(new[,] { { -0.1 }, { 1 } }, new List<string> { "A" });
			Assert.IsTrue(mdata.IsConsistent(out var mdataConsistent), mdataConsistent);
			Assert.IsTrue(mdata2.IsConsistent(out var mdata2Consistent), mdata2Consistent);
			Assert.IsTrue(mdata3.IsConsistent(out var mdata3Consistent), mdata3Consistent);
			var result = ConcatenateProcessing.Concatenate(mdata, mdata2, mdata3);
		    var a = result.Values.GetColumn(0).Unpack();
		    var b = result.Values.GetColumn(1).Unpack();
			Assert.That(a, Is.EqualTo(new [] {0.1, 1, double.NaN, double.NaN, double.NaN, -0.1, 1}).AsCollection.Within(0.00001));
			Assert.That(b, Is.EqualTo(new [] {double.NaN, double.NaN, 2.1, 4, 3.1, double.NaN, double.NaN}).AsCollection.Within(0.00001));
	    }

	    [Test]
	    public void TestConcatenate()
	    {
		    var mdata = PerseusFactory.CreateMatrixData();
			mdata.AddStringColumn("A", "", new []{"1", "2"});
		    var mdata2 = PerseusFactory.CreateMatrixData();
		    var result = ConcatenateProcessing.Concatenate(mdata, mdata2);
			CollectionAssert.AreEqual(new [] {"1", "2"}, result.StringColumns[0]);
	    }

	}
}