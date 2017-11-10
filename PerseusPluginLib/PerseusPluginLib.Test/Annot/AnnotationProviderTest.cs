using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PerseusPluginLib.AnnotCols.AnnotationProvider;

namespace PerseusPluginLib.Test.Annot
{
    [TestFixture]
    public class AnnotationProviderTest
    {
        [Test]
        public void TestWritingMockData()
        {
            var provider = new MockAnnotationProvider();
            using (var memstream = new MemoryStream())
            using (var writer = new StreamWriter(memstream))
            {
                AnnotationFormat.WriteMapping(("id", provider.idCol),
                new[] { ("textannot", provider.textannot), ("textannot2", provider.textannot2) },
                new[] { ("catannot", provider.catannot) },
                new[] { ("numannot", provider.numannot) },
                writer);

                var result = Encoding.UTF8.GetString(memstream.ToArray());
                using (var memstream2 = new MemoryStream(Encoding.UTF8.GetBytes(result)))
                using(var reader = new StreamReader(memstream2))
                {
                    var expectedAnnotation = provider.Sources[0];
                    var annotations = AnnotationFormat.Annotations(reader);
                    CollectionAssert.AreEqual(expectedAnnotation.id, annotations.id);
                    var tests = expectedAnnotation.annotation.OrderBy(a => a.name).Zip(annotations.annotations.OrderBy(a => a.name),
                        (expected, actual) => (expected, actual));
                    foreach (var (expeted, actual) in tests)
                    {
                        Assert.AreEqual(expeted.name, actual.name);
                        Assert.AreEqual(expeted.type, actual.type);
                    }
                }

                using (var memstream2 = new MemoryStream(Encoding.UTF8.GetBytes(result)))
                using(var reader = new StreamReader(memstream2))
                {
                    var mapping = AnnotationFormat.ReadMappings(reader, 0, new []{0, 1, 2, 3, 4}).ToArray();
                    var expectedMapping = provider.ReadMappings(0, 0, new[] {0, 1, 2, 3, 4}).ToArray();
                    Assert.AreEqual(mapping.Length, expectedMapping.Length);
                    for (int i = 0; i < mapping.Length; i++)
                    {
                        var expected = expectedMapping[i];
                        var actual = mapping[i];
                        Assert.AreEqual(expected.fromId, actual.fromId);
                        Assert.AreEqual(expected.toIds.Length, actual.toIds.Length);
                        for (int j = 0; j < expected.toIds.Length; j++)
                        {
                            CollectionAssert.AreEqual(expected.toIds[j].Select(x => x.Trim()).OrderBy(x => x),
                                actual.toIds[j].Select(x => x.Trim()).OrderBy(x => x));
                        }
                    }
                }
            }
        }
        [Test]
        public void TestMapOneIdentifier()
        {
            var annotation =
@"Base	Target
#!{Type}	Text
1	2";
            using (var memstream = new MemoryStream(Encoding.UTF8.GetBytes(annotation)))
            using (var reader = new StreamReader(memstream))
            {
                var provider = new StreamAnnotationProvider(("test", reader));
                var mapping = provider.MapToBaseIdentifiers(new []{"2", "3"}, 0, 0);
                CollectionAssert.AreEqual(new [] {"1", ""}, mapping);
            }
        }
        [Test]
        public void TestReadDuplicateIdentifier()
        {
            var annotation =
@"Base	Target
#!{Type}	Text
1	2
3	2";
            using (var memstream = new MemoryStream(Encoding.UTF8.GetBytes(annotation)))
            using (var reader = new StreamReader(memstream))
            {
                var mapping = AnnotationFormat.ReadMappings(reader, 1, new[] {0}).ToArray();
                Assert.AreEqual(1, mapping.Length);
                var (name, toIds) = mapping.Single();
                Assert.AreEqual("2", name);
                Assert.AreEqual(1, toIds.Length);
                CollectionAssert.AreEqual(new [] {"1", "3"}, toIds[0]);
            }
        }
        [Test]
        public void TestMapDuplicateIdentifier()
        {
            var annotation =
@"Base	Target
#!{Type}	Text
1	2
3	2";
            using (var memstream = new MemoryStream(Encoding.UTF8.GetBytes(annotation)))
            using (var reader = new StreamReader(memstream))
            {
                var provider = new StreamAnnotationProvider(("test", reader));
                var mapping = provider.MapToBaseIdentifiers(new []{"2", "3"}, 0, 0);
                CollectionAssert.AreEqual(new [] {"1;3", ""}, mapping);
            }
        }

        [Test]
        public void TestMapAllIdentifiers()
        {
            var provider = new MockAnnotationProvider();
            var mapping = provider.MapToBaseIdentifiers(provider.textannot2, 0, 1);
            var ids = provider.idCol.ToArray();
            ids[2] = "";
            ids[4] = ids[4].Replace(" ", "");
            CollectionAssert.AreEqual(ids, mapping);
        }

        [Test]
        public void TestMapMissingIdentifiers()
        {
            var provider = new MockAnnotationProvider();
            var mapping = provider.MapToBaseIdentifiers(new []{"c", "b", "g"}, 0, 0);
            CollectionAssert.AreEqual(new [] {"2", "1;4;5", ""}, mapping);
        }
        [Test]
        public void TestMapSeparatedIdentifiers()
        {
            var provider = new MockAnnotationProvider();
            var mapping = provider.MapToBaseIdentifiers(new []{"c;b", "0"}, 0, 0);
            CollectionAssert.AreEqual(new [] {"1;2;4;5", ""}, mapping);
        }

        [Test]
        public void TestReadAllAnnotations()
        {
            var provider = new MockAnnotationProvider();
            var annots = provider.ReadAnnotations(provider.idCol, 0, new[] {0, 1, 2, 3});
            Assert.AreEqual(2, annots.text.Length);
            Assert.AreEqual("textannot", annots.text[0].name);
            CollectionAssert.AreEqual(provider.textannot.Select(x => x.Replace(" ", "")).ToArray(), annots.text[0].values);
            Assert.AreEqual("textannot2", annots.text[1].name);
            CollectionAssert.AreEqual(provider.textannot2, annots.text[1].values);
            Assert.AreEqual(1, annots.category.Length);
            Assert.AreEqual(1, annots.numeric.Length);
        }

        [Test]
        public void TestReadAllAnnotationsMissingIdentifiers()
        {
            var provider = new MockAnnotationProvider();
            var annots = provider.ReadAnnotations(new []{"3", "2", "0"}, 0, new[] {0, 1, 2, 3});
            Assert.AreEqual(2, annots.text.Length);
            Assert.AreEqual(1, annots.category.Length);
            Assert.AreEqual(1, annots.numeric.Length);
            CollectionAssert.AreEqual(new [] {1.0, -1, double.NaN}, annots.numeric.Single().values);
        }
    }
}