using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using PerseusApi.Generic;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test.Utils
{
    [TestFixture]
    public class DataWithAnnotationColumnsExtensionsTest
    {
        [Test]
        public void UniqueValuesTest()
        {
            Mock<IDataWithAnnotationColumns> moq = new Moq.Mock<IDataWithAnnotationColumns>();
            List<string[]> testList = new List<string[]> { new[] { "a;b", "a;a" } };
            moq.Setup(data => data.StringColumns).Returns(testList);
            IDataWithAnnotationColumns asdf = moq.Object;
            asdf.UniqueValues(new[] { 0 });
            CollectionAssert.AreEqual(new [] {"a;b", "a"}, testList[0]);
        }
    }
}