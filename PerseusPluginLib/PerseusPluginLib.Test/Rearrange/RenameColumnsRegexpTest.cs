using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BaseLibS.Param;
using Moq;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Rearrange;

namespace PerseusPluginLib.Test.Rearrange{
	[TestFixture]
	public class RenameColumnsRegexpTest{
		/// <summary>
		/// Test the wiki example
		/// </summary>
		[Test]
		public void TestWikiExample(){
			List<string> colnames = new List<string>(){"column 1", "column 2", "column 3"};
			var error = Rename(colnames, "column (.*)", "$1");
			Assert.IsTrue(string.IsNullOrEmpty(error));
			CollectionAssert.AreEqual(new List<string>{"1", "2", "3"}, colnames);
		}

		/// <summary>
		/// Test switching order
		/// </summary>
		[Test]
		public void TestSwitchOrder(){
			List<string> colnames = new List<string>(){"column 1", "column 2", "column 3"};
			var error = Rename(colnames, "(?<first>.*) (?<second>.*)", "${second} ${first}");
			Assert.IsTrue(string.IsNullOrEmpty(error));
			CollectionAssert.AreEqual(new List<string>{"1 column", "2 column", "3 column"}, colnames);
		}

		[Test]
		public void TestReplacement(){
			List<string> colnames = new List<string>(){"column 1", "column 2", "column 3"};
			var error = Rename(colnames, "(column) (.*)", "$1SPACE$2");
			Assert.IsTrue(string.IsNullOrEmpty(error));
			CollectionAssert.AreEqual(new List<string>{"columnSPACE1", "columnSPACE2", "columnSPACE3"}, colnames);
		}

		[Test]
		public void TestDuplicates(){
			List<string> colnames = new List<string>(){"column 1", "column 2", "column 3"};
			var error = Rename(colnames, "(column) (.*)", "$1");
			Assert.IsFalse(string.IsNullOrEmpty(error));
			CollectionAssert.AreEqual(new List<string>{"column", "column", "column"}, colnames);
		}

		/// <summary>
		/// renaming helper method for mocking IMatrixData
		/// </summary>
		/// <param name="colnames"></param>
		/// <param name="pattern"></param>
		/// <param name="replacement"></param>
		private static string Rename(List<string> colnames, string pattern, string replacement){
			RenameColumnsRegexp renamer = new RenameColumnsRegexp();
			Mock<IMatrixData> matrix = new Mock<IMatrixData>();
			matrix.Setup(m => m.ColumnCount).Returns(colnames.Count);
			matrix.Setup(m => m.ColumnNames).Returns(colnames);
			matrix.Setup(m => m.StringColumnNames).Returns(new List<string>());
			matrix.Setup(m => m.StringColumnCount).Returns(0);
			matrix.Setup(m => m.NumericColumnNames).Returns(new List<string>());
			matrix.Setup(m => m.NumericColumnCount).Returns(0);
			matrix.Setup(m => m.CategoryColumnNames).Returns(new List<string>());
			matrix.Setup(m => m.CategoryColumnCount).Returns(0);
			matrix.Setup(m => m.MultiNumericColumnNames).Returns(new List<string>());
			matrix.Setup(m => m.MultiNumericColumnCount).Returns(0);
			string err = "";
			Parameters param = renamer.GetParameters(matrix.Object, ref err);
            param.GetParamWithSubParams<int>("Column type").GetSubParameters()
                .GetParam<Tuple<Regex, string>>("Regex").Value = Tuple.Create(new Regex(pattern), replacement);
			IMatrixData[] supplTables = null;
			IDocumentData[] documents = null;
			var pInfo = new ProcessInfo(new Settings(), s => { }, i => { }, 1);
			renamer.ProcessData(matrix.Object, param, ref supplTables, ref documents, pInfo);
			return pInfo.ErrString;
		}
	}
}