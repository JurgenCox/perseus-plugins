using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusPluginLib.Load;

namespace PerseusPluginLib.Test
{
    [TestClass]
    public class SerializationTest
    {
        [TestMethod]
        public void TestLoadMatrixParam()
        {
            var param = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "a;b;c;d;e;f", "1;3", "4", "2", "5", "", "true"} };
            var serializer = new XmlSerializer(param.GetType());
            var writer = new StringWriter();
            serializer.Serialize(writer, param);
            var paramString = writer.ToString();
            var reader = new StringReader(paramString);
            var param2 = (PerseusLoadMatrixParam) serializer.Deserialize(reader);
            Assert.AreEqual("fileName", param2.Filename);
            CollectionAssert.AreEquivalent(new [] {"a", "b", "c", "d", "e", "f"}, param2.Items);
            CollectionAssert.AreEquivalent(new [] {1, 3}, param2.MainColumnIndices);
            CollectionAssert.AreEquivalent(new [] {4}, param2.NumericalColumnIndices);
            CollectionAssert.AreEquivalent(new [] {2}, param2.CategoryColumnIndices);
            CollectionAssert.AreEquivalent(new [] {5}, param2.TextColumnIndices);
        }
    }
}