using System.Collections.Generic;
using NUnit.Framework;
using PerseusLibS.Data;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test.Utils
{
    [TestFixture]
    public class DataWithAnnotationRowsExtensionsTest
    {
        [Test]
        public void TestConcatOnUniqueRow()
        {
            var left = new DataWithAnnotationRows {ColumnNames = new List<string> {"Col 5"}};
            left.AddNumericRow("num", "descr", new []{1.0});
            var right = new DataWithAnnotationRows
            {
                ColumnNames = new List<string> {"Col 1", "Col 2", "Col 3", "Col 4"}
            };
            right.AddStringRow("string", "descr", new []{"1", "2", "3", "4"});
            Assert.IsTrue(right.IsConsistent(out string con0), con0);
            var output = new DataWithAnnotationRows {ColumnNames = new List<string> {"Col 5", "Col 1"}};
            output.AddStringRow("string", "descr", new [] {"", "1"});
            output.AddNumericRow("num", "descr", new []{1.0, double.NaN});
            left.Concat(right, new []{0});
            Assert.IsTrue(left.IsConsistent(out string con1), con1);
            Assert.IsTrue(output.IsConsistent(out _));
            Assert.AreEqual(output, left);
        }
        [Test]
        public void TestConcatOnCommonRow()
        {
            var left = new DataWithAnnotationRows {ColumnNames = new List<string> {"Col 5"}};
            left.AddStringRow("string", "descr", new []{"5"});
            var right = new DataWithAnnotationRows
            {
                ColumnNames = new List<string> {"Col 1", "Col 2", "Col 3", "Col 4"}
            };
            right.AddStringRow("string", "descr", new []{"1", "2", "3", "4"});
            Assert.IsTrue(right.IsConsistent(out string con0), con0);
            var output = new DataWithAnnotationRows {ColumnNames = new List<string> {"Col 5", "Col 3", "Col 2"}};
            output.AddStringRow("string", "descr", new [] {"5", "3", "2"});
            left.Concat(right, new []{2,1});
            Assert.IsTrue(left.IsConsistent(out string con1), con1);
            Assert.IsTrue(output.IsConsistent(out _));
            Assert.AreEqual(output, left);
        }
    }
}