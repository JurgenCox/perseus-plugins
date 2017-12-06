using System.Collections.Generic;
using System.Linq;
using BaseLibS.Param;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Join;
using PerseusPluginLib.Rearrange;
using Assert = NUnit.Framework.Assert;
using CollectionAssert = NUnit.Framework.CollectionAssert;

namespace PerseusPluginLib.Test.Join
{
	[TestFixture]
	public class MatchingRowsByNameTest : BaseTest
    {
        private Parameters parameters;
        private IMatrixData expand;
        private IMatrixData proteinMain;
        private IMatrixData peptides;
        private MatchingRowsByName matching;

        [SetUp]
        public void TestInitialize()
        {
            double[,] peptidesValues = new[,] {{9.0}};
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
            multiNum.ProcessData(peptides, parameters2, ref suppl, ref docs, CreateProcessInfo());

	        double[,] proteinMainValues = new[,]
	        {
	            {166250000.0},
                {8346000.0}
	        };
	        proteinMain = PerseusFactory.CreateMatrixData(proteinMainValues, new List<string> {"prot_LFQ intensity"});
	        proteinMain.Name = "protein main";
            proteinMain.AddStringColumn("prot_id", "", new [] {"13", "21"});
            proteinMain.AddStringColumn("prot_gene name", "", new [] {"geneA", "geneB"});
	        double[,] expandValues = new[,]
	        {
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
	        SingleChoiceParam matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in matrix 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        IMatrixData matched = matching.ProcessData(new[] {expand, proteinMain}, parameters, ref supplTables, ref documents, CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(1, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
	    [Test]
	    public void TestSmallExample2()
	    {
	        Parameter<int[]> mainColParam = parameters.GetParam<int[]>("Main columns");
	        mainColParam.Value = new[] {0};
	        SingleChoiceParam matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in matrix 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        IMatrixData matched = matching.ProcessData(new[] {expand, proteinMain}, parameters, ref supplTables, ref documents, CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "prot_LFQ intensity", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(2, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
        [Test]
	    public void TestSmallExample3()
	    {
	        Parameter<int[]> mainColParam = parameters.GetParam<int[]>("Main columns");
	        mainColParam.Value = new[] {0};
	        SingleChoiceParam matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in matrix 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        IMatrixData matched = matching.ProcessData(new[] {peptides, proteinMain}, parameters, ref supplTables, ref documents, CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "prot_LFQ intensity", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(2, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }

        [Test]
        public void TestStringAnnotationRowsWithDifferentNames()
        {
            var matching = new MatchingRowsByName();
            var mdata1 = PerseusFactory.CreateMatrixData(new[,] { { 0.0, 1 }, { 2, 3 } }, new List<string> { "A", "B" });
            mdata1.AddStringRow("catRow1", "", new []{"A", "B"});
            mdata1.AddStringColumn("Id", "", new []{"1", "2"});
            var mdata2 = PerseusFactory.CreateMatrixData(new[,] { { 0.0, 1 }, { 2, 3 } }, new List<string> { "B", "C" });
            mdata2.AddStringRow("catRow2", "", new []{"A", "B"});
            mdata2.AddStringColumn("Id", "", new []{"2", "1"});
            mdata2.AddStringColumn("Test", "", new []{"2_", "1_"});
            var inputData = new[] {mdata1, mdata2};
            var errString = string.Empty;
            var parameters = matching.GetParameters(inputData, ref errString);
            parameters.GetParam<int[]>("Text columns").Value = new []{1};
            Assert.IsTrue(string.IsNullOrEmpty(errString), errString);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var processInfo = CreateProcessInfo();
            var output = matching.ProcessData(inputData, parameters, ref supplTables, ref documents, processInfo);
            Assert.IsTrue(string.IsNullOrEmpty(processInfo.ErrString));
            Assert.IsTrue(output.IsConsistent(out string con), con);
            Assert.AreEqual(output.ColumnCount, output.Values.ColumnCount);
            Assert.AreEqual(output.ColumnCount, output.StringRows[0].Length);
            Assert.AreEqual(2, output.StringColumnCount);
            CollectionAssert.AreEqual(new [] {"1_", "2_"}, output.StringColumns[1]);
        }
        [Test]
        public void TestCategoryAnnotationRowsWithDifferentNames()
        {
            var matching = new MatchingRowsByName();
            var mdata1 = PerseusFactory.CreateMatrixData(new[,] { { 0.0, 1 }, { 2, 3 } }, new List<string> { "A", "B" });
            mdata1.AddCategoryRow("catRow1", "", new []{new []{"A", "B"}});
            mdata1.AddStringColumn("Id", "", new []{"1", "2"});
            var mdata2 = PerseusFactory.CreateMatrixData(new[,] { { 0.0, 1 }, { 2, 3 } }, new List<string> { "B", "C" });
            mdata2.AddCategoryRow("catRow2", "", new []{new []{"B", "C"}});
            mdata2.AddStringColumn("Id", "", new []{"1", "2"});
            var inputData = new[] {mdata1, mdata2};
            var errString = string.Empty;
            var parameters = matching.GetParameters(inputData, ref errString);
            Assert.IsTrue(string.IsNullOrEmpty(errString), errString);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var processInfo = CreateProcessInfo();
            var output = matching.ProcessData(inputData, parameters, ref supplTables, ref documents, processInfo);
            Assert.IsTrue(string.IsNullOrEmpty(processInfo.ErrString));
            Assert.IsTrue(output.IsConsistent(out string con), con);
            Assert.AreEqual(output.ColumnCount, output.Values.ColumnCount);
            Assert.AreEqual(output.ColumnCount, output.StringRows[0].Length);
            Assert.AreEqual(output.ColumnCount, output.GetCategoryRowAt(0).Length);
        }
        [Test]
        public void TestAnnotationRows()
        {
            var matching = new MatchingRowsByName();
            var mdata1 = PerseusFactory.CreateMatrixData(new[,] { { 0.0, 1 }, { 2, 3 } }, new List<string> { "A", "B" });
            mdata1.AddCategoryRow("catRow", "", new []{new []{"A", "B"}});
            mdata1.AddStringRow("stringRow", "", new []{"A", "B"});
            mdata1.AddStringColumn("Id", "", new []{"1", "2"});
            var mdata2 = PerseusFactory.CreateMatrixData(new[,] { { 0.0, 1 }, { 2, 3 } }, new List<string> { "B", "C" });
            mdata2.AddCategoryRow("catRow", "", new []{new []{"B", "C"}});
            mdata2.AddStringRow("stringRow", "", new []{"A", "B"});
            mdata2.AddStringColumn("Id", "", new []{"1", "2"});
            var inputData = new[] {mdata1, mdata2};
            var errString = string.Empty;
            var parameters = matching.GetParameters(inputData, ref errString);
            Assert.IsTrue(string.IsNullOrEmpty(errString), errString);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
            var processInfo = CreateProcessInfo();
            var output = matching.ProcessData(inputData, parameters, ref supplTables, ref documents, processInfo);
            Assert.IsTrue(string.IsNullOrEmpty(processInfo.ErrString));
            Assert.IsTrue(output.IsConsistent(out string con), con);
            Assert.AreEqual(output.ColumnCount, output.Values.ColumnCount);
            Assert.AreEqual(output.ColumnCount, output.StringRows[0].Length);
            Assert.AreEqual(output.ColumnCount, output.GetCategoryRowAt(0).Length);
        }
    }
}