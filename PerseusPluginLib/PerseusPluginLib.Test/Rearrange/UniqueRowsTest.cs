using BaseLibS.Num;
using NUnit.Framework;
using PerseusApi.Utils;
using PerseusLibS.Data;
using PerseusPluginLib.Rearrange;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test.Rearrange{
	[TestFixture]
    public class UniqueRowsTest
    {
        [Test]
        public void SmallTest()
        {
            var mdata = PerseusFactory.CreateMatrixData(new float[,]
            {
                {0, 4},
                {1, 5},
                {2, 6},
                {3, 7}
            });
            mdata.AddStringColumn("id", "", new []{"a", "b", "b", "b"});
            mdata.AddStringColumn("str", "", new []{"a;b", "b;c", "c;d", "d;e"});
            mdata.AddCategoryColumn("cat", "", new[] { new[] { "a", "b" }, new[] { "b", "c" }, new[] { "c", "d" }, new[] { "d", "e" } });
            mdata.AddNumericColumn("num", "", new []{0, 1, 2, 3, 4.0});
            mdata.AddMultiNumericColumn("mnum", "", new []{ new []{0, 4d}, new []{1, 5d}, new []{2, 6d}, new []{3, 7d}});
            mdata.UniqueRows(mdata.StringColumns[0], ArrayUtils.Median, UniqueRows.Union, UniqueRows.CatUnion, UniqueRows.MultiNumUnion);

            Assert.AreEqual(2, mdata.RowCount);
            CollectionAssert.AreEqual(new [] {0, 2}, mdata.Values.GetColumn(0));
            CollectionAssert.AreEqual(new [] {4, 6}, mdata.Values.GetColumn(1));
            CollectionAssert.AreEqual(new [] {"a;b", "b;c;d;e"}, mdata.GetStringColumn("str"));
            CollectionAssert.AreEqual(new [] {new [] {"a", "b"}, new []{"b", "c", "d" ,"e"}}, mdata.GetCategoryColumnAt(0));
            CollectionAssert.AreEqual(new [] {0, 2}, mdata.NumericColumns[0]);
            CollectionAssert.AreEqual(new [] {new [] {0d, 4}, new []{1d,5, 2, 6, 3, 7}}, mdata.MultiNumericColumns[0]);
        }
    }
}