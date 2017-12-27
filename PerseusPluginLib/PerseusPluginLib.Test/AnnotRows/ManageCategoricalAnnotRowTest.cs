using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.AnnotRows;

namespace PerseusPluginLib.Test.AnnotRows
{
    [TestClass]
    public class ManageCategoricalAnnotRowTest
    {
        [TestMethod]
        public void TestGetParameters()
        {
            var processing = new ManageCategoricalAnnotRow();
            var errorString = string.Empty;
            var mdata = PerseusFactory.CreateMatrixData(new[,] {{0.0, 0, 0}, {1, 1, 1}}, new List<string> {"a_1", "a_2", "b_1"});
            var parameters = processing.GetParameters(mdata, ref errorString);
            Assert.AreEqual(string.Empty, errorString);
            var action = parameters.GetParamWithSubParams<int>("Action");
            action.Value = 1;
            action.GetSubParameters().GetParam<string>("Name").Value = "Experiment";
            IMatrixData[] suppl = null;
            IDocumentData[] suppld = null;
            processing.ProcessData(mdata, parameters, ref suppl, ref suppld, new ProcessInfo(new Settings(), s => { },
                i => { }, 1));
            Assert.AreEqual("Experiment", mdata.CategoryRowNames[0]);
            CollectionAssert.AreEquivalent(new [] {"a", "b"}, mdata.GetCategoryRowValuesAt(0));
            CollectionAssert.AreEqual(new [] {"a"}, mdata.GetCategoryRowEntryAt(0,0));
            CollectionAssert.AreEqual(new [] {"a"}, mdata.GetCategoryRowEntryAt(0,1));
            CollectionAssert.AreEqual(new [] {"b"}, mdata.GetCategoryRowEntryAt(0,2));
        }
    }
}