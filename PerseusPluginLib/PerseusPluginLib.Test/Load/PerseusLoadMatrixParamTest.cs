using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusPluginLib.Load;

namespace PerseusPluginLib.Test.Load
{
    [TestClass]
    public class PerseusLoadMatrixParamTest
    {
        [TestMethod]
        public void TestLoadMatrixParam()
        {
            var param = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "a;b 1;c;d;e;f", "1;3", "4", "2", "5", "", "true"} };
            Assert.AreEqual("fileName", param.Filename);
            CollectionAssert.AreEquivalent(new [] {"a", "b", "c", "d", "e", "f"}, param.Items);
            CollectionAssert.AreEquivalent(new [] {1, 3}, param.MainColumnIndices);
            CollectionAssert.AreEquivalent(new [] {4}, param.NumericalColumnIndices);
            CollectionAssert.AreEquivalent(new [] {2}, param.CategoryColumnIndices);
            CollectionAssert.AreEquivalent(new [] {5}, param.TextColumnIndices);
        }

        [TestMethod]
        public void TestStringValue()
        {
            var param2 = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "a;b 1;c;d;e;f", "1;3", "4", "2", "5", "", "true"} };
            var stringValue = param2.StringValue;

            var param = new PerseusLoadMatrixParam("test");
            param.StringValue = stringValue;
            Assert.AreEqual("fileName", param.Filename);
            CollectionAssert.AreEquivalent(new [] {"a", "b 1", "c", "d", "e", "f"}, param.Items);
            CollectionAssert.AreEquivalent(new [] {1, 3}, param.MainColumnIndices);
            CollectionAssert.AreEquivalent(new [] {4}, param.NumericalColumnIndices);
            CollectionAssert.AreEquivalent(new [] {2}, param.CategoryColumnIndices);
            CollectionAssert.AreEquivalent(new [] {5}, param.TextColumnIndices);
        }
    }
}