using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusApi.Document;
using PerseusApi.Matrix;
using PerseusPluginLib.Load;
using PerseusPluginLib.Rearrange;

namespace PerseusPluginLibTest
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
            /*
				string[] result = new string[8];
				result[0] = Filename;
				result[1] = StringUtils.Concat(";", MultiListSelector1.items);
				result[2] = StringUtils.Concat(";", MainColumnIndices);
				result[3] = StringUtils.Concat(";", NumericalColumnIndices);
				result[4] = StringUtils.Concat(";", CategoryColumnIndices);
				result[5] = StringUtils.Concat(";", TextColumnIndices);
				result[6] = StringUtils.Concat(";", MultiNumericalColumnIndices);
				result[7] = "" + (ShortenCheckBox.IsChecked == true);
				return result;
                */
        }
    }
}