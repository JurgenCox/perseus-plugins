using System.Collections.Generic;
using BaseLibS.Param;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PerseusApi.Document;
using PerseusApi.Matrix;
using PerseusLibS;
using PerseusPluginLib.Rearrange;

namespace PerseusPluginLib.Test.Rearrange{
	/// <summary>
	/// Testing the ProcessTextColumns class requires, at a minimum, a regular expression 
	/// and MatrixData for it to act on. The private method TestRegex encapsulates nost
	/// of the mechanics, so that the test methods only have to specify the regex, the 
	/// input data, and the expected output.
	/// </summary>
	[TestClass]
	public class ProcessTextColumnsTest{
		/// <summary>
		/// The regex "^([^;]+)" should output everything before the first semicolon.
		/// </summary>
		[TestMethod] public void TestOnlyToFirstSemicolon(){
			const string regexStr = "^([^;]+)";
			string[] stringsInit = {"just one item", "first item; second item"};
			string[] stringsExpect = {"just one item", "first item"};
			TestRegex(regexStr, stringsInit, stringsExpect);
		}

		/// <summary>
		/// The regex "B *= *([^,; ]+)" should output the value given to B.
		/// </summary>
		[TestMethod] public void TestAssignmentWithEqualSign(){
			const string regexStr = "B *= *([^,; ]+)";
			string[] stringsInit = new[]{"A = 123, B = 456", "A=123; B=456"};
			string[] stringsExpect = new[]{"456", "456"};
			TestRegex(regexStr, stringsInit, stringsExpect);
		}

		/// <summary>
		/// The regex "B *= *([^,; ]+)" should output the value given to B.
		/// </summary>
		[TestMethod] public void TestSeparatedBySemicolons(){
			const string regexStr = "B *= *([^,; ]+)";
			string[] stringsInit = new[]{"A = 123, B = 456", "A=123; B=456", "B=123; B=456"};
			string[] stringsExpect = new[]{"456", ";456", "123;456"};
			TestRegex(regexStr, stringsInit, stringsExpect);
		}

		/// <summary>
		/// An auxiliary method for testing the action of regular expressions. 
		/// Limited to a single column, which should be sufficient for this purpose.
		/// Multiple rows are allowed to test the effect of one regex on several strings.
		/// </summary>
		private static void TestRegex(string regexStr, string[] stringsInit, string[] stringsExpect){
			const string name = "Test";
			IMatrixData[] supplTables = null;
			IDocumentData[] documents = null;
			List<string> stringColumnNames = new List<string>{"Column Name"};
			List<string[]> stringColumnsInit = new List<string[]>{stringsInit};
			List<string[]> stringColumnsExpect = new List<string[]>{stringsExpect};
			Parameters param =
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Columns", new[]{0}){Values = stringColumnNames},
					new StringParam("Regular expression", regexStr), new BoolParam("Keep original columns", false),
					new BoolParam("Strings separated by semicolons are independent", false)
				});
			IMatrixData mdata = PerseusFactory.CreateNewMatrixData();
			mdata.Clear();
			mdata.Name = name;
			mdata.SetAnnotationColumns(stringColumnNames, stringColumnsInit, mdata.CategoryColumnNames, new List<string[][]>(),
				mdata.NumericColumnNames, mdata.NumericColumns, mdata.MultiNumericColumnNames, mdata.MultiNumericColumns);
			var ptc = new ProcessTextColumns();
			ptc.ProcessData(mdata, param, ref supplTables, ref documents, null);
			const bool ignoreCase = false;
			for (int rowInd = 0; rowInd < stringColumnsInit[0].Length; rowInd++){
				Assert.AreEqual(mdata.StringColumns[0][rowInd], stringColumnsExpect[0][rowInd], ignoreCase);
			}
		}
	}
}