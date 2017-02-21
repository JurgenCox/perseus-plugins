using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusApi.Utils;

namespace PerseusPluginLib.Test.PerseusApi
{
    [TestClass]
    public class PerseusUtilsTest : BaseTest
    {
        [TestMethod]
        [DeploymentItem("conf", "conf")]
        public void GetAvailableAnnotsTest()
        {
            string[] baseNames, files;
            var annots = PerseusUtils.GetAvailableAnnots(out baseNames, out files);
            Assert.AreEqual(3, files.Length);
            Assert.AreEqual(3, baseNames.Length);
            Assert.AreEqual(3, annots.Length);
            CollectionAssert.AreEqual(new [] {"ENSG", "UniProt", "ENSG"}, baseNames);
            CollectionAssert.AreEqual(new [] {"Chromosome", "Base pair index", "Orientation"}, annots[0]);
        }
    }
}