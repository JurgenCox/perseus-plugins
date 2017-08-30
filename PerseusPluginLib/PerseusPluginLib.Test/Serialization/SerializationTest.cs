using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using PerseusPluginLib.Load;

namespace PerseusPluginLib.Test.Serialization
{
    [TestFixture]
    public class SerializationTest
    {
        [Test]
        public void TestLoadMatrixParam()
        {
            PerseusLoadMatrixParam param = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "1 a;b 1;c;d;e;f", "1;3", "4", "2", "5", "", "true"} };
            XmlSerializer serializer = new XmlSerializer(param.GetType());
            
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, param);
            string paramString = writer.ToString();
            StringReader reader = new StringReader(paramString);
            PerseusLoadMatrixParam param2 = (PerseusLoadMatrixParam) serializer.Deserialize(reader);
            Assert.AreEqual("fileName", param2.Filename);
            CollectionAssert.AreEquivalent(new [] {"1 a", "b 1", "c", "d", "e", "f"}, param2.Items);
            CollectionAssert.AreEquivalent(new [] {1, 3}, param2.MainColumnIndices);
            CollectionAssert.AreEquivalent(new [] {4}, param2.NumericalColumnIndices);
            CollectionAssert.AreEquivalent(new [] {2}, param2.CategoryColumnIndices);
            CollectionAssert.AreEquivalent(new [] {5}, param2.TextColumnIndices);
            Assert.IsNotNull(param2.FilterParameterValues[0]);
        }
    }
}