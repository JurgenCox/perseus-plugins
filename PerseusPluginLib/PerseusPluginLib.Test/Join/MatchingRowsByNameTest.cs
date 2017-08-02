using System.Collections.Generic;
using System.Linq;
using BaseLibS.Param;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Join;
using PerseusPluginLib.Mods;
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
            var peptidesValues = new[,] {{9.0f}};
            peptides = PerseusFactory.CreateMatrixData(peptidesValues, new List<string> {"pep_MS/MS Count"});
            peptides.AddNumericColumn("pep_Intensity", "", new [] {0.0});
            peptides.AddStringColumn("pep_id", "", new []{"35"});
            peptides.AddStringColumn("pep_Protein group IDs", "", new []{"13;21"});
            var multiNum = new ExpandMultiNumeric();
            var errorString = string.Empty;
            var parameters2 = multiNum.GetParameters(peptides, ref errorString);
            parameters2.GetParam<int[]>("Text columns").Value = new[] {1};
            IMatrixData[] suppl = null;
            IDocumentData[] docs = null;
            multiNum.ProcessData(peptides, parameters2, ref suppl, ref docs, CreateProcessInfo());

	        var proteinMainValues = new[,]
	        {
	            {166250000.0f},
                {8346000.0f}
	        };
	        proteinMain = PerseusFactory.CreateMatrixData(proteinMainValues, new List<string> {"prot_LFQ intensity"});
	        proteinMain.Name = "protein main";
            proteinMain.AddStringColumn("prot_id", "", new [] {"13", "21"});
            proteinMain.AddStringColumn("prot_gene name", "", new [] {"geneA", "geneB"});
	        var expandValues = new[,]
	        {
	            {9.0f},
                {9.0f}
	        };
	        expand = PerseusFactory.CreateMatrixData(expandValues, new List<string> {"pep_MS/MS Count"});
	        expand.Name = "expand";
            expand.AddNumericColumn("pep_Intensity", "", new [] {0.0, 0.0});
            expand.AddStringColumn("pep_id", "", new []{"35", "35"});
            expand.AddStringColumn("pep_Protein group IDs", "", new []{"13", "21"});

	        matching = new MatchingRowsByName();
	        var err = string.Empty;
	        parameters = matching.GetParameters(new[] {expand, proteinMain}, ref err);
            
        }

        [Test]
        public void TestExpandMultiNumColumn()
        {
            Assert.AreEqual(1, peptides.IsImputed.ColumnCount);
            Assert.AreEqual(2, peptides.IsImputed.RowCount);
            Assert.AreEqual(2, peptides.RowCount);
        }

	    [Test]
	    public void TestSmallExample()
	    {
	        var matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in matrix 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        var matched = matching.ProcessData(new[] {expand, proteinMain}, parameters, ref supplTables, ref documents, CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(1, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
	    [Test]
	    public void TestSmallExample2()
	    {
	        var mainColParam = parameters.GetParam<int[]>("Main columns");
	        mainColParam.Value = new[] {0};
	        var matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in matrix 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        var matched = matching.ProcessData(new[] {expand, proteinMain}, parameters, ref supplTables, ref documents, CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "prot_LFQ intensity", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(2, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
        [Test]
	    public void TestSmallExample3()
	    {
	        var mainColParam = parameters.GetParam<int[]>("Main columns");
	        mainColParam.Value = new[] {0};
	        var matchColParam1 = (SingleChoiceParam) parameters.GetParam<int>("Matching column in matrix 1");
            CollectionAssert.AreEqual(new [] {"pep_id", "pep_Protein group IDs"}, matchColParam1.Values.ToArray());
	        matchColParam1.Value = 1;
            Assert.AreEqual("pep_Protein group IDs", matchColParam1.StringValue);
	        IMatrixData[] supplTables = null;
	        IDocumentData[] documents = null;
	        var matched = matching.ProcessData(new[] {peptides, proteinMain}, parameters, ref supplTables, ref documents, CreateProcessInfo());

            CollectionAssert.AreEqual(new [] {"pep_MS/MS Count", "prot_LFQ intensity", "pep_id", "pep_Protein group IDs", "pep_Intensity"},
                matched.ColumnNames.Concat(matched.StringColumnNames).Concat(matched.NumericColumnNames).ToArray());
            Assert.AreEqual(2, matched.RowCount);
            Assert.AreEqual(2, matched.ColumnCount);
            Assert.AreEqual(1, matched.NumericColumnCount);
	    }
	}
}