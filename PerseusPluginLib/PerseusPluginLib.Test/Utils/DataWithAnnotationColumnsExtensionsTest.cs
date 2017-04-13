using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusApi.Generic;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test.Utils
{
    [TestClass()]
    public class DataWithAnnotationColumnsExtensionsTest
    {
        [TestMethod()]
        public void UniqueValuesTest()
        {
            var moq = new Moq.Mock<IDataWithAnnotationColumns>();
            var testList = new List<string[]> { new[] { "a;b", "a;a" } };
            moq.Setup(data => data.StringColumns).Returns(testList);
            var asdf = moq.Object;
            asdf.UniqueValues(new[] { 0 });
            CollectionAssert.AreEqual(new [] {"a;b", "a"}, testList[0]);
        }
    }
}