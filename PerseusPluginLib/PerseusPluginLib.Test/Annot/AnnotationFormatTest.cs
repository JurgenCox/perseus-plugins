using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PerseusApi.Generic;
using PerseusPluginLib.AnnotCols.AnnotationProvider;

namespace PerseusPluginLib.Test.Annot
{
    [TestFixture]
    public class AnnotationFormatTest
    {
        private static IEnumerable<int[]> _selections = new[]
        {
            new [] {1},
            new [] {3,4},
        };

        [Test]
        public void TestReadingAnnotationsWithDifferentSelections([ValueSource(nameof(_selections))] int[] selection)
        {
            using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Examples.MouseUniprotAnnotations))))
            {
                var annotations = AnnotationFormat.ReadMappings(reader, 0, selection).ToArray();
                var expected = new[]
                {
                    "p34968", "b1atn5", "q5wru6", "q91v24", "q9jl36", "q9dbm0", "q6pe15", "q3tmb4", "q8c3s8", "q8c724", "q8k188"
                };
                Assert.AreEqual(expected.Length, annotations.Length);
                CollectionAssert.AreEquivalent(expected, annotations.Select(annot => annot.fromId));
                foreach (var (fromId, toIds) in annotations)
                {
                    Assert.AreEqual(fromId, fromId.ToLower());
                    Assert.AreEqual(toIds.Length, selection.Length);
                }
            }
        }

        [Test]
        public void TestReadingAnnotationsForOneId()
        {
            using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Examples.MouseUniprotAnnotations))))
            {
                var annotations = AnnotationFormat.ReadMappings(reader, 0, new []{1,21}).ToDictionary(annot => annot.fromId, annot => annot.toIds);
                Assert.AreEqual("Htr2c", annotations["p34968"][1][0]);
                Assert.AreEqual("Htr2c", annotations["b1atn5"][1][0]);
                Assert.AreEqual("Abca7", annotations["q91v24"][1][0]);
                Assert.AreEqual("Abhd10", annotations["q8k188"][1][0]);
            }
        }

        [Test]
        public void TestWriteSomeAnnotations()
        {
            var baseIdentifiers = ("ids", new[] {"0;3", "4", "A"});
            var textannot = new[] {("textannot", new[] {"1", "2", "3"}), ("textannot2", new []{"4", "5;6", "8"})};
            var catannot = new[] {("catannot", new[] {new[] {"a", "b", "c"}, new string[0], new[] {"", "d"}})};
            var numannot = new[] {("numannot", new [] {0.0, 1.0, 2.0}) };
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                AnnotationFormat.WriteMapping(baseIdentifiers, textannot, catannot, numannot, writer);
                var result = Encoding.UTF8.GetString(stream.ToArray());
                var lines = Regex.Split(result, @"\r?\n|\r");
                Assert.AreEqual(6, lines.Length);
                Assert.AreEqual("ids\tcatannot\ttextannot\ttextannot2\tnumannot", lines[0]);
                Assert.AreEqual("#!{Type}\tCategorical\tText\tText\tNumerical", lines[1]);
                Assert.AreEqual("0;3\ta;b;c\t1\t4\t0", lines[2]);
                Assert.AreEqual("4\t\t2\t5;6\t1", lines[3]);
                Assert.AreEqual("A\td\t3\t8\t2", lines[4]);
                Assert.AreEqual("", lines[5]);
            }
        }

        [Test]
        public void TestAnnotationInfo()
        {
            using (var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Examples.MouseUniprotAnnotations))))
            {
                var info = AnnotationFormat.Annotations(reader);
                Assert.AreEqual("UniProt", info.id);
                Assert.AreEqual(42, info.annotations.Length);
                Assert.AreEqual("GOBP name", info.annotations[0].name);
                Assert.AreEqual(AnnotType.Categorical, info.annotations[0].type);
                Assert.AreEqual("Gene name", info.annotations[20].name);
                Assert.AreEqual(AnnotType.Text, info.annotations[20].type);
            }
        }
    }
}