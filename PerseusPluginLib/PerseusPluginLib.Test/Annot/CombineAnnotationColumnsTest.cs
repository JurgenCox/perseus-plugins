using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PerseusPluginLib.AnnotCols;

namespace PerseusPluginLib.Test.Annot
{
    [TestFixture]
    public class CombineAnnotationColumnsTest
    {
        private static readonly string[] Empty = { };
        private static readonly string[][] None = { };
        private static readonly string[][] Some = { new [] {"a", "b"}, Empty, new [] {"a"} };
        private static readonly string[][] Full = { new [] {"a", "b"}, new [] {"a"}, new []{"c", "a"} };
        private static readonly string[][] ManyEmpty = { new [] {"a", "b"}, Empty, Empty, Empty,  new []{"c", "a"} };

        [Test]
        public void TestCombineUnion()
        {
            CollectionAssert.AreEqual(Empty, CombineAnnotationColumns.Union(None));
            CollectionAssert.AreEqual(new [] {"a", "b"}, CombineAnnotationColumns.Union(Some));
            CollectionAssert.AreEqual(new [] {"a", "b", "c"}, CombineAnnotationColumns.Union(Full));
        }

        [Test]
        public void TestCombineIntersection()
        {
            CollectionAssert.AreEqual(Empty, CombineAnnotationColumns.Intersection(None));
            CollectionAssert.AreEqual(Empty, CombineAnnotationColumns.Intersection(Some));
            CollectionAssert.AreEqual(new [] {"a"}, CombineAnnotationColumns.Intersection(Full));
        }

        [Test]
        public void TestCombineMajority()
        {
            CollectionAssert.AreEqual(Empty, CombineAnnotationColumns.Majority(None));
            CollectionAssert.AreEqual(new [] {"a"}, CombineAnnotationColumns.Majority(Some));
            CollectionAssert.AreEqual(new [] {"a"}, CombineAnnotationColumns.Majority(Full));
            CollectionAssert.AreEqual(Empty, CombineAnnotationColumns.Majority(ManyEmpty));
        }
    }
}
