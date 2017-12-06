using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using PerseusApi.Generic;
using PerseusLibS.Data;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test.Utils
{
    [TestFixture]
    public class IMatrixDataExtensionsTest
    {
        [Test]
        public void TestCombine()
        {
            var data1 = new DataWithAnnotationColumns();
            data1.AddStringColumn("Id", "id", new []{"1", "2"});
            var data2 = new DataWithAnnotationColumns();
            data2.AddStringColumn("Id", "id", new []{"2", "1"});
            data2.AddStringColumn("String", "string", new []{"two", "one"});
            data1.Concat(data2, new []{0}, new []{0});
            CollectionAssert.AreEqual(new []{"one", "two"}, data1.StringColumns[1] );
        }

        [Test]
        public void TestCombineWithSeparatedIds()
        {
            var data1 = new DataWithAnnotationColumns();
            data1.AddStringColumn("Id", "id", new []{"1;2", "3", "4"});
            var data2 = new DataWithAnnotationColumns();
            data2.AddStringColumn("Id", "id", new []{"0", "2", "1", "3;4", "5"});
            data2.AddStringColumn("String", "string", new []{"zero", "two", "one", "three_or_four", "five"});
            data1.Concat(data2, new []{0}, new []{0});
            CollectionAssert.AreEqual(new []{"two;one", "three_or_four", "three_or_four"}, data1.StringColumns[1] );
        }

        [Test]
        public void TestCombineOnTwoColumns()
        {
            var data1 = new DataWithAnnotationColumns();
            data1.AddStringColumn("Id", "id", new []{"1;2", "3", "4"});
            data1.AddStringColumn("Id2", "id", new []{"match", "noMatch", "match"});
            var data2 = new DataWithAnnotationColumns();
            data2.AddStringColumn("Id", "id", new []{"0", "2", "1", "3;4", "5"});
            data2.AddStringColumn("Id2", "id", new []{"nomatch", "match", "nomatch", "match", "nomatch"});
            data2.AddStringColumn("String", "string", new []{"zero", "two", "one", "three_or_four", "five"});
            data1.Concat(data2, new []{0, 1}, new []{0, 1});
            CollectionAssert.AreEqual(new []{"two", "", "three_or_four"}, data1.StringColumns[2] );
        }
    }
}