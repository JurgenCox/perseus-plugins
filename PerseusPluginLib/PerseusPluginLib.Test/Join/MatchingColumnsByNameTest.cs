using System.Collections.Generic;
using System.Linq;
using BaseLibS.Param;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Join;
using PerseusPluginLib.Rearrange;

namespace PerseusPluginLib.Test.Join
{
	[TestFixture]
	public class MatchingColumnsByNameTest
    {
        [Test]
        public void TestSmallExample()
        {
            var m1 = PerseusFactory.CreateMatrixData(new double[,] { { 0, 1 }, { 2, 3 } }, new List<string> { "col 1", "col 2" });
            var m2 = PerseusFactory.CreateMatrixData(new double[,] { { 4, 5 }, { 6, 7 } }, new List<string> { "col 2", "col 3" });

            var m = new[] {m1, m2};
            var matching = new MatchingColumnsByName();
            var errString = string.Empty;
            var parameters = matching.GetParameters(m, ref errString);
            Assert.IsTrue(string.IsNullOrEmpty(errString));

	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var pInfo = BaseTest.CreateProcessInfo();
            var result = matching.ProcessData(m, parameters, ref supplTables, ref documents, pInfo);
            Assert.IsTrue(string.IsNullOrEmpty(pInfo.ErrString));

            Assert.AreEqual(4, result.RowCount);
            CollectionAssert.AreEqual(new [] {"col 2", "col 1", "col 3"}, result.ColumnNames);
            CollectionAssert.AreEqual(new [] {1, 3, 4, 6.0}, result.Values.GetColumn(0).ToArray());
            CollectionAssert.AreEqual(new [] {0, 2, double.NaN, double.NaN}, result.Values.GetColumn(1).ToArray());
            CollectionAssert.AreEqual(new [] {double.NaN, double.NaN, 5, 7}, result.Values.GetColumn(2).ToArray());
        }

        [Test]
        public void TestSmallExampleWithAnnotationColumns()
        {
            var m1 = PerseusFactory.CreateMatrixData(new double[,] { { 0, 1 }, { 2, 3 } }, new List<string> { "col 1", "col 2" });
            m1.AddStringColumn("m1", "", new[] { "a", "b" });
            m1.AddStringColumn("common string column", "", new[] { "c", "d" });
            m1.AddNumericColumn("m1", "", new[] { 0, 1.0 });
            m1.AddNumericColumn("common numeric column", "", new[] { 2, 3.0 });
            m1.AddCategoryColumn("common category column", "", new []{new []{"cat1"}, new []{"cat2", "cat3"}});
            var m2 = PerseusFactory.CreateMatrixData(new double[,] { { 4, 5 }, { 6, 7 } }, new List<string> { "col 2", "col 3" });
            m2.AddStringColumn("common string column", "", new []{"e", "f"});
            m2.AddStringColumn("m2", "", new []{"g", "h"});
            m2.AddNumericColumn("common numeric column", "", new[] { 4, 5.0 });
            m2.AddCategoryColumn("common category column", "", new []{new []{"cat2"}, new []{"cat1", "cat4"}});

            var m = new[] {m1, m2};
            var matching = new MatchingColumnsByName();
            var errString = string.Empty;
            var parameters = matching.GetParameters(m, ref errString);
            Assert.IsTrue(string.IsNullOrEmpty(errString));

	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var pInfo = BaseTest.CreateProcessInfo();
            var result = matching.ProcessData(m, parameters, ref supplTables, ref documents, pInfo);
            Assert.IsTrue(string.IsNullOrEmpty(pInfo.ErrString));

            CollectionAssert.AreEqual(new [] {"common string column", "m1", "m2"}, result.StringColumnNames);
            CollectionAssert.AreEqual(new [] {"c", "d", "e", "f"}, result.StringColumns[0]);
            CollectionAssert.AreEqual(new [] {"a", "b", "", ""}, result.StringColumns[1]);
            CollectionAssert.AreEqual(new [] {"", "", "g", "h"}, result.StringColumns[2]);

            CollectionAssert.AreEqual(new [] {"common numeric column", "m1"}, result.NumericColumnNames);
            CollectionAssert.AreEqual(new [] {2, 3, 4, 5.0}, result.NumericColumns[0]);
            CollectionAssert.AreEqual(new [] {0, 1, double.NaN, double.NaN}, result.NumericColumns[1]);

            CollectionAssert.AreEqual(new [] {"common category column"}, result.CategoryColumnNames);
            var actual = result.GetCategoryColumnAt(0);
            var expected = new[] {new[] {"cat1"}, new[] {"cat2", "cat3"}, new[] {"cat2"}, new[] {"cat1", "cat4"}};
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < actual.Length; i++)
            {
                CollectionAssert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}