using NUnit.Framework;
using PerseusPluginLib.Load;

namespace PerseusPluginLib.Test.Load
{
    [TestFixture]
    public class PerseusLoadMatrixParamTest
    {
        [Test]
        public void TestLoadMatrixParam()
        {
            PerseusLoadMatrixParam param = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "a;b 1;c;d;e;f", "1;3", "4", "2", "5", "", "true"} };
            Assert.AreEqual("fileName", param.Filename);
            CollectionAssert.AreEquivalent(new [] {"a", "b 1", "c", "d", "e", "f"}, param.Items);
            CollectionAssert.AreEquivalent(new [] {1, 3}, param.MainColumnIndices);
            CollectionAssert.AreEquivalent(new [] {4}, param.NumericalColumnIndices);
            CollectionAssert.AreEquivalent(new [] {2}, param.CategoryColumnIndices);
            CollectionAssert.AreEquivalent(new [] {5}, param.TextColumnIndices);
        }

        [Test]
        public void TestStringValue()
        {
            PerseusLoadMatrixParam param2 = new PerseusLoadMatrixParam("test") { Value = new []{"fileName", "a;b 1;c;d;e;f", "1;3", "4", "2", "5", "", "true"} };
            string stringValue = param2.StringValue;

            PerseusLoadMatrixParam param = new PerseusLoadMatrixParam("test") {StringValue = stringValue};
            Assert.AreEqual("fileName", param.Filename);
            CollectionAssert.AreEquivalent(new [] {"a", "b 1", "c", "d", "e", "f"}, param.Items);
            CollectionAssert.AreEquivalent(new [] {1, 3}, param.MainColumnIndices);
            CollectionAssert.AreEquivalent(new [] {4}, param.NumericalColumnIndices);
            CollectionAssert.AreEquivalent(new [] {2}, param.CategoryColumnIndices);
            CollectionAssert.AreEquivalent(new [] {5}, param.TextColumnIndices);
        }
    }
}