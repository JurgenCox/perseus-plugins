using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BaseLibS.Param;
using Moq;
using NUnit.Framework;
using PerseusApi.Document;
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
			Rename(colnames, "column (.*)", "$1");
			CollectionAssert.AreEqual(new List<string>{"1", "2", "3"}, colnames);
		}

		/// <summary>
		/// Test switching order
		/// </summary>
		[Test]
		public void TestSwitchOrder(){
			List<string> colnames = new List<string>(){"column 1", "column 2", "column 3"};
			Rename(colnames, "(?<first>.*) (?<second>.*)", "${second} ${first}");
			CollectionAssert.AreEqual(new List<string>{"1 column", "2 column", "3 column"}, colnames);
		}

		[Test]
		public void TestReplacement(){
			List<string> colnames = new List<string>(){"column 1", "column 2", "column 3"};
			Rename(colnames, "(column) (.*)", "$1SPACE$2");
			CollectionAssert.AreEqual(new List<string>{"columnSPACE1", "columnSPACE2", "columnSPACE3"}, colnames);
		}

		/// <summary>
		/// renaming helper method for mocking IMatrixData
		/// </summary>
		/// <param name="colnames"></param>
		/// <param name="pattern"></param>
		/// <param name="replacement"></param>
		private static void Rename(List<string> colnames, string pattern, string replacement){
			RenameColumnsRegexp renamer = new RenameColumnsRegexp();
			Mock<IMatrixData> matrix = new Moq.Mock<IMatrixData>();
			matrix.Setup(m => m.ColumnCount).Returns(colnames.Count);
			matrix.Setup(m => m.ColumnNames).Returns(colnames);
			string err = "";
			Parameters param = renamer.GetParameters(matrix.Object, ref err);
			param.GetParam<Tuple<Regex, string>>("Regex").Value = Tuple.Create(new Regex(pattern), replacement);
			IMatrixData[] supplTables = null;
			IDocumentData[] documents = null;
			renamer.ProcessData(matrix.Object, param, ref supplTables, ref documents, null);
		}
	}
}