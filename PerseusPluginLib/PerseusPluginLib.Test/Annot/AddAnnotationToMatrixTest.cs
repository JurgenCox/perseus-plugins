using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PerseusApi.Utils;
using PerseusPluginLib.AnnotCols;

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
            string[][] annots = PerseusUtils.GetAvailableAnnots(out baseNames, out files);
            int uniprotIndex = baseNames.ToList().FindIndex(name => name.ToLower().Equals("uniprot"));
            int selection = annots[uniprotIndex].ToList().FindIndex(name => name.ToLower().Equals("gene name"));
            string[] ids = new[] {"P08908"};
            Dictionary<string, string[]> mapping = AddAnnotationToMatrix.ReadMapping(ids, files[1], new[] {selection});
            CollectionAssert.AreEqual(new [] {"HTR1A"}, mapping[ids[0]]);
        }
    }
}