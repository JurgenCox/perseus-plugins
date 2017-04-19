using System.Linq;
using NUnit.Framework;
using PerseusApi.Utils;
using PerseusPluginLib.Annot;

namespace PerseusPluginLib.Test.Annot
{
    [TestFixture]
    public class AddAnnotationToMatrixTest : BaseTest
    {
        [Test]
        public void ReadMappingTest()
        {
            Assert.Inconclusive("Should be moved to integration tests, using conf");
            //[DeploymentItem("conf", "conf")]
            string[] baseNames, files;
            var annots = PerseusUtils.GetAvailableAnnots(out baseNames, out files);
            var uniprotIndex = baseNames.ToList().FindIndex(name => name.ToLower().Equals("uniprot"));
            var selection = annots[uniprotIndex].ToList().FindIndex(name => name.ToLower().Equals("gene name"));
            var ids = new[] {"P08908"};
            var mapping = AddAnnotationToMatrix.ReadMapping(ids, files[1], new[] {selection});
            CollectionAssert.AreEqual(new [] {"HTR1A"}, mapping[ids[0]]);
        }
    }
}