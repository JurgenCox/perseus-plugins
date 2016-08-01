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
            var param = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "0;1;2", "3", "4", "", "", "", "true"} };
            var serializer = new XmlSerializer(param.GetType());
            var writer = new StringWriter();
            serializer.Serialize(writer, param);
            var reader = new StringReader(writer.ToString());
            var param2 = (PerseusLoadMatrixParam) serializer.Deserialize(reader);
            Assert.AreEqual("fileName", param2.Filename);
            Assert.IsTrue(param.MainColumnIndices.SequenceEqual(param2.MainColumnIndices));
        }
    }
}