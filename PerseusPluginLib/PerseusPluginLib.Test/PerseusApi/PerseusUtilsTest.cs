using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusFactory = PerseusApi.Utils.PerseusFactory;

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

        [TestMethod]
        public void WriteMatrixTest()
        {
            // main data
            var mdata = PerseusFactory.CreateMatrixData(new float[,] { { 1, 2, 3 }, { 3, 4, 5 } }, new List<string> { "col1", "col2", "col3" });
            // annotation rows
            mdata.AddCategoryRow("catrow", "this is catrow", new[] { new[] { "cat1" }, new[] { "cat1", "cat2" }, new[] { "cat2" } });
            mdata.AddNumericRow("numrow", "this is numrow", new[] { -1.0, 1, 2 });
            // annotation columns
            mdata.AddStringColumn("strcol1", "this is stringcol1", new[] { "1", "2" });
            mdata.AddStringColumn("strcol2", "", new[] { "", "hallo" });
            mdata.AddNumericColumn("numcol", "", new[] { 1.0, 2.0 });
            mdata.AddMultiNumericColumn("multnumcol", "this is multnumcol", new[] { new[] { -2.0, 2.0 }, new double[] { } });
            mdata.AddCategoryColumn("catcol", "", new[] { new[] { "cat1", "cat1.1" }, new[] { "cat2", "cat1" } });

            string mdataStr;
            using (var memstream = new MemoryStream())
            using (var writer = new StreamWriter(memstream))
            {
                PerseusUtils.WriteMatrix(mdata, writer);
                writer.Flush();
                mdataStr = Encoding.UTF8.GetString(memstream.ToArray());
            }

            IMatrixData mdata2 = PerseusFactory.CreateMatrixData();
            PerseusUtils.ReadMatrix(mdata2, new ProcessInfo(new Settings(), status => { }, progress => { }, 1, i => { }), () =>
            {
                var tmpStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(mdataStr)));
                return tmpStream;
            }, "matrix1", '\t');
            
            Assert.AreEqual(2, mdata2.RowCount);
            Assert.AreEqual(3, mdata2.ColumnCount);

            Assert.AreEqual(2, mdata2.StringColumnCount);
            Assert.AreEqual(1, mdata2.NumericColumnCount);
            Assert.AreEqual(1, mdata2.CategoryColumnCount);
            Assert.AreEqual(1, mdata2.MultiNumericColumnCount);

            Assert.AreEqual("hallo", mdata2.StringColumns[mdata2.StringColumnNames.FindIndex(col => col.Equals("strcol2"))][1]);

            Assert.AreEqual(1, mdata2.CategoryRowCount);
            Assert.AreEqual(1, mdata2.NumericRowCount);
        }

        [TestMethod]
        public void WriteDataWithAnnotationColumnsTest()
        {
            // main data
            var mdata = PerseusFactory.CreateDataWithAnnotationColumns();
            // annotation columns
            mdata.AddStringColumn("strcol1", "this is stringcol1", new[] { "1", "2" });
            mdata.AddStringColumn("strcol2", "", new[] { "", "hallo" });
            mdata.AddNumericColumn("numcol", "", new[] { 1.0, 2.0 });
            mdata.AddMultiNumericColumn("multnumcol", "this is multnumcol", new[] { new[] { -2.0, 2.0 }, new double[] { } });
            mdata.AddCategoryColumn("catcol", "", new[] { new[] { "cat1", "cat1.1" }, new[] { "cat2", "cat1" } });

            string mdataStr;
            using (var memstream = new MemoryStream())
            using (var writer = new StreamWriter(memstream))
            {
                PerseusUtils.WriteDataWithAnnotationColumns(mdata, writer);
                writer.Flush();
                mdataStr = Encoding.UTF8.GetString(memstream.ToArray());
            }

            IMatrixData _mdata2 = PerseusFactory.CreateMatrixData();
            PerseusUtils.ReadMatrix(_mdata2, new ProcessInfo(new Settings(), status => { }, progress => { }, 1, i => { }), () =>
            {
                var tmpStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(mdataStr)));
                return tmpStream;
            }, "matrix1", '\t');
            var mdata2 = (IDataWithAnnotationColumns) _mdata2;
            
            Assert.AreEqual(2, mdata2.RowCount);

            Assert.AreEqual(2, mdata2.StringColumnCount);
            Assert.AreEqual(1, mdata2.NumericColumnCount);
            Assert.AreEqual(1, mdata2.CategoryColumnCount);
            Assert.AreEqual(1, mdata2.MultiNumericColumnCount);

            Assert.AreEqual("hallo", mdata2.StringColumns[mdata2.StringColumnNames.FindIndex(col => col.Equals("strcol2"))][1]);
        }
    }
}