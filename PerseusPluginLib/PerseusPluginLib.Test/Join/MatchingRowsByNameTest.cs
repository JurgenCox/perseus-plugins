using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num.Matrix;
using BaseLibS.Param;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusLibS.Data;
using PerseusPluginLib.Join;
using PerseusPluginLib.Manual;
using PerseusPluginLib.Rearrange;

namespace PerseusPluginLib.Test.Join
{
	[TestFixture]
	public class MatchingRowsByNameTest
    {
        private Parameters parameters;
        private IMatrixData expand;
        private IMatrixData proteinMain;
        private IMatrixData peptides;
        private MatchingRowsByName matching;

        [SetUp]
        public void TestInitialize()
        {
            double[,] peptidesValues = {{9.0}};
            peptides = PerseusFactory.CreateMatrixData(peptidesValues, new List<string> {"pep_MS/MS Count"});
            peptides.AddNumericColumn("pep_Intensity", "", new [] {0.0});
            peptides.AddStringColumn("pep_id", "", new []{"35"});
            peptides.AddStringColumn("pep_Protein group IDs", "", new []{"13;21"});
            peptides.Quality.Init(1, 1);
            peptides.Quality.Set(0, 0, 1);
            ExpandMultiNumeric multiNum = new ExpandMultiNumeric();
            string errorString = string.Empty;
            Parameters parameters2 = multiNum.GetParameters(peptides, ref errorString);
            parameters2.GetParam<int[]>("Text columns").Value = new[] {1};
            IMatrixData[] suppl = null;
            IDocumentData[] docs = null;
            multiNum.ProcessData(peptides, parameters2, ref suppl, ref docs, BaseTest.CreateProcessInfo());

	        double[,] proteinMainValues = {
	            {166250000.0},
                {8346000.0}
	        };
	        proteinMain = PerseusFactory.CreateMatrixData(proteinMainValues, new List<string> {"prot_LFQ intensity"});
	        proteinMain.Name = "protein main";
            proteinMain.AddStringColumn("prot_id", "", new [] {"13", "21"});
            proteinMain.AddStringColumn("prot_gene name", "", new [] {"geneA", "geneB"});
	        double[,] expandValues = {
	            {9.0},
                {9.0}
	        };
	        expand = PerseusFactory.CreateMatrixData(expandValues, new List<string> {"pep_MS/MS Count"});
	        expand.Name = "expand";
            expand.AddNumericColumn("pep_Intensity", "", new [] {0.0, 0.0});
            expand.AddStringColumn("pep_id", "", new []{"35", "35"});
            expand.AddStringColumn("pep_Protein group IDs", "", new []{"13", "21"});

	        matching = new MatchingRowsByName();
	        string err = string.Empty;
	        parameters = matching.GetParameters(new[] {expand, proteinMain}, ref err);
        }

        [Test]
        public void TestExpandMultiNumColumn()
        {
            Assert.AreEqual(1, peptides.Quality.ColumnCount);
            Assert.AreEqual(2, peptides.Quality.RowCount);
            Assert.AreEqual(1, peptides.IsImputed.ColumnCount);
            Assert.AreEqual(2, peptides.IsImputed.RowCount);
            Assert.AreEqual(2, peptides.RowCount);
        }

	    [Test]
	    public void TestSmallExample()
	    {
	        SingleChoiceParam matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in table 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs", "pep_Intensity"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        IMatrixData matched = matching.ProcessData(new[] {expand, proteinMain}, parameters, ref supplTables, ref documents, BaseTest.CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(1, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
	    [Test]
	    public void TestSmallExample2()
	    {
	        Parameter<int[]> mainColParam = parameters.GetParam<int[]>("Copy main columns");
	        mainColParam.Value = new[] {0};
	        SingleChoiceParam matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in table 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs", "pep_Intensity"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        IMatrixData matched = matching.ProcessData(new[] {expand, proteinMain}, parameters, ref supplTables, ref documents, BaseTest.CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "prot_LFQ intensity", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(2, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
        [Test]
	    public void TestSmallExample3()
	    {
	        Parameter<int[]> mainColParam = parameters.GetParam<int[]>("Copy main columns");
	        mainColParam.Value = new[] {0};
	        SingleChoiceParam matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in table 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs", "pep_Intensity"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        IMatrixData matched = matching.ProcessData(new[] {peptides, proteinMain}, parameters, ref supplTables, ref documents, BaseTest.CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "prot_LFQ intensity", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(2, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }

        [Test]
        public void TestConvertNumericToMultiNumeric()
        {
            var mBase = PerseusFactory.CreateMatrixData();
            mBase.AddStringColumn("Id", "", new []{"n1;n2", "n3", "n5"});
			Assert.IsTrue(mBase.IsConsistent(out var mBaseConsistent), mBaseConsistent);
            var mdata = PerseusFactory.CreateMatrixData(new[,] {{0.0}, {1.0}, {2.0}, {3.0}});
            mdata.AddStringColumn("Id", "", new []{"n1", "n2", "n3", "n4"});
			Assert.IsTrue(mdata.IsConsistent(out var mdataConsistent), mdataConsistent);
            var match = new MatchingRowsByName();
            var errString = string.Empty;
            var param = match.GetParameters(new []{mBase, mdata}, ref errString);
	        param.GetParam<int[]>("Copy main columns").Value = new[] {0};
            param.GetParam<int>("Combine copied main values").Value = 5;
	        param.GetParam<int>("Join style").Value = 1;
	        param.GetParam<bool>("Add indicator").Value = true;
	        param.GetParam<bool>("Add original row numbers").Value = true;
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var result = match.ProcessData(new[] {mBase, mdata}, param, ref supplTables, ref documents, BaseTest.CreateProcessInfo());
			var indicator = result.GetCategoryColumnAt(0).Select(cats => cats.SingleOrDefault() ?? "");
			CollectionAssert.AreEqual(new [] {"+", "+", "", "+"}, indicator);
			CollectionAssert.AreEqual(new [] {"n1;n2", "n3", "n5", "n4"}, result.GetStringColumn("Id"));
            CollectionAssert.AreEqual(new [] {"Original row numbers", "Column 1"}, result.MultiNumericColumnNames); 
            CollectionAssert.AreEqual(new [] {0.0, 1.0}, result.MultiNumericColumns[0][0]);
            CollectionAssert.AreEqual(new [] {2.0}, result.MultiNumericColumns[0][1]);
            CollectionAssert.AreEqual(new double[0], result.MultiNumericColumns[0][2]);
            CollectionAssert.AreEqual(new [] {3.0}, result.MultiNumericColumns[0][3]);
            CollectionAssert.AreEqual(new [] {0.0, 1.0}, result.MultiNumericColumns[1][0]);
            CollectionAssert.AreEqual(new [] {2.0}, result.MultiNumericColumns[1][1]);
            CollectionAssert.AreEqual(new double[0], result.MultiNumericColumns[1][2]);
            CollectionAssert.AreEqual(new [] {3.0}, result.MultiNumericColumns[1][3]);
        }

	    [Test]
	    public void TestMatchingCaseSensitive()
	    {
		    var mBase = PerseusFactory.CreateMatrixData();
			mBase.AddStringColumn("Name", "", new []{"A", "a", "B", "b", "C", "c"});
			Assert.IsTrue(mBase.IsConsistent(out var mBaseConsistent), mBaseConsistent);

		    var mdata = PerseusFactory.CreateMatrixData();
			mdata.AddStringColumn("Name", "", new []{"a", "B"});
			Assert.IsTrue(mdata.IsConsistent(out var mdataConsistent), mdataConsistent);
            var match = new MatchingRowsByName();
            var errString = string.Empty;
            var param = match.GetParameters(new []{mBase, mdata}, ref errString);
	        param.GetParam<bool>("Add indicator").Value = true;
	        param.GetParam<bool>("Ignore case").Value = false;
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var result = match.ProcessData(new[] {mBase, mdata}, param, ref supplTables, ref documents, BaseTest.CreateProcessInfo());
			var indicator = result.GetCategoryColumnAt(0).Select(cats => cats.SingleOrDefault() ?? "").ToArray();
			CollectionAssert.AreEqual(new [] {"", "+", "+", "", "", ""}, indicator);
	    }

	    [Test]
	    public void TestMatchingCaseInSensitive()
	    {
		    var mBase = PerseusFactory.CreateMatrixData();
			mBase.AddStringColumn("Name", "", new []{"A", "a", "B", "b", "C", "c"});
			Assert.IsTrue(mBase.IsConsistent(out var mBaseConsistent), mBaseConsistent);

		    var mdata = PerseusFactory.CreateMatrixData();
			mdata.AddStringColumn("Name", "", new []{"a", "B"});
			Assert.IsTrue(mdata.IsConsistent(out var mdataConsistent), mdataConsistent);
            var match = new MatchingRowsByName();
            var errString = string.Empty;
            var param = match.GetParameters(new []{mBase, mdata}, ref errString);
	        param.GetParam<bool>("Add indicator").Value = true;
	        param.GetParam<bool>("Ignore case").Value = true;
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var result = match.ProcessData(new[] {mBase, mdata}, param, ref supplTables, ref documents, BaseTest.CreateProcessInfo());
			var indicator = result.GetCategoryColumnAt(0).Select(cats => cats.SingleOrDefault() ?? "").ToArray();
			CollectionAssert.AreEqual(new [] {"+", "+", "+", "+", "", ""}, indicator);
	    }
	}
}