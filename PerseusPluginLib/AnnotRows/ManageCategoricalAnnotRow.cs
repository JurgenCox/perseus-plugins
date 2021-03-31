using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.AnnotRows{
	public class ManageCategoricalAnnotRow : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("groupButton.Image.png");

		public string Description =>
			"Manage the categorical annotation rows. One important applications is to define a grouping that is " +
			"later used in a t-test or ANOVA.";

		public string HelpOutput => "Same matrix with categorical annotation rows added or modified.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Categorical annotation rows";
		public string Heading => "Annot. rows";
		public bool IsActive => true;
		public float DisplayRank => 1;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url =>
			"http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotrows:ManageCategoricalAnnotRow";

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			ParameterWithSubParams<int> scwsp = param.GetParamWithSubParams<int>("Action");
			Parameters spar = scwsp.GetSubParameters();
			switch (scwsp.Value){
				case 0:
					ProcessDataCreate(mdata, spar);
					break;
				case 1:
					ProcessDataCreateFromGoupNames(mdata, spar, processInfo);
					break;
				case 2:
					ProcessDataEdit(mdata, spar);
					break;
				case 3:
					ProcessDataRename(mdata, spar);
					break;
				case 4:
					ProcessDataDelete(mdata, spar);
					break;
				case 5:
					ProcessDataWriteTemplateFile(mdata, spar);
					break;
				case 6:
					string err = ProcessDataReadFromFile(mdata, spar);
					if (err != null){
						processInfo.ErrString = err;
					}
					break;
			}
		}

		private static string ProcessDataReadFromFile(IDataWithAnnotationRows mdata, Parameters param){
			Parameter<string> fp = param.GetParam<string>("Input file");
			string filename = fp.Value;
			string[] colNames;
			try{
				colNames = TabSep.GetColumnNames(filename, '\t');
			} catch (Exception){
				return "Could not open file " + filename + ". It maybe open in another program.";
			}
			int nameIndex = GetNameIndex(colNames);
			if (nameIndex < 0){
				return "Error: the file has to contain a column called 'Name'.";
			}
			if (colNames.Length < 2){
				return "Error: the file does not contain an annotation column.";
			}
			string[] nameCol = TabSep.GetColumn(colNames[nameIndex], filename, '\t');
			(string[] catColNames, string[] numColNames) = SplitColNames(colNames, nameIndex);
			Dictionary<string, int> map = ArrayUtils.InverseMap(nameCol);
			foreach (string catColName in catColNames){
				AddCategoryRow(catColName, filename, mdata, map);
			}
			foreach (string numColName in numColNames){
				ManageNumericalAnnotRow.AddNumericRow(numColName, filename, mdata, map);
			}
			return null;
		}

		internal static void AddCategoryRow(string catColName, string filename, IDataWithAnnotationRows mdata,
			Dictionary<string, int> map){
			string[] groupCol = TabSep.GetColumn(catColName, filename, '\t');
			string[][] newCol = new string[mdata.ColumnCount][];
			for (int j = 0; j < newCol.Length; j++){
				string colName = mdata.ColumnNames[j];
				if (!map.ContainsKey(colName)){
					newCol[j] = new string[0];
					continue;
				}
				int ind = map[colName];
				string group = groupCol[ind] ?? "";
				group = group.Trim();
				if (string.IsNullOrEmpty(group)){
					newCol[j] = new string[0];
				} else{
					string[] w = group.Split(';');
					Array.Sort(w);
					for (int k = 0; k < w.Length; k++){
						w[k] = w[k].Trim();
					}
					newCol[j] = w;
				}
			}
			if (catColName.ToLower().EndsWith("[c]")){
				catColName = catColName.Substring(0, catColName.Length - 3);
				catColName = catColName.Trim();
			}
			mdata.AddCategoryRow(catColName, catColName, newCol);
		}

		private static (string[], string[]) SplitColNames(string[] colNames, int nameIndex){
			List<string> catColNames = new List<string>();
			List<string> numColNames = new List<string>();
			for (int i = 0; i < colNames.Length; i++){
				if (i == nameIndex){
					continue;
				}
				string colName = colNames[i];
				if (colName.ToLower().EndsWith("[n]")){
					numColNames.Add(colName);
				} else{
					catColNames.Add(colName);
				}
			}
			return (catColNames.ToArray(), numColNames.ToArray());
		}

		private static int GetNameIndex(IList<string> colNames){
			for (int i = 0; i < colNames.Count; i++){
				if (colNames[i].ToLower().Equals("name")){
					return i;
				}
			}
			return -1;
		}

		private static void ProcessDataWriteTemplateFile(IDataWithAnnotationRows mdata, Parameters param){
			Parameter<string> fp = param.GetParam<string>("Output file");
			StreamWriter writer = new StreamWriter(fp.Value);
			writer.WriteLine("Name\tNew grouping");
			for (int i = 0; i < mdata.ColumnCount; i++){
				string colName = mdata.ColumnNames[i];
				writer.WriteLine(colName + "\t" + colName);
			}
			writer.Close();
		}

		private static void ProcessDataRename(IDataWithAnnotationRows mdata, Parameters param){
			int groupColInd = param.GetParam<int>("Category row").Value;
			string newName = param.GetParam<string>("New name").Value;
			string newDescription = param.GetParam<string>("New description").Value;
			mdata.CategoryRowNames[groupColInd] = newName;
			mdata.CategoryRowDescriptions[groupColInd] = newDescription;
		}

		private static void ProcessDataDelete(IDataWithAnnotationRows mdata, Parameters param){
			int groupColInd = param.GetParam<int>("Category row").Value;
			mdata.RemoveCategoryRowAt(groupColInd);
		}

		private static void ProcessDataEdit(IDataWithAnnotationRows mdata, Parameters param){
			ParameterWithSubParams<int> s = param.GetParamWithSubParams<int>("Category row");
			int groupColInd = s.Value;
			Parameters sp = s.GetSubParameters();
			string[][] newRow = new string[mdata.ColumnCount][];
			Dictionary<string, string> map = sp.GetParam<Dictionary<string, string>>("Values").Value;
			for (int i = 0; i < mdata.ColumnCount; i++){
				string t = mdata.ColumnNames[i];
				string x = map[t];
				newRow[i] = x.Length > 0 ? x.Split(';') : new string[0];
			}
			mdata.SetCategoryRowAt(newRow, groupColInd);
		}

		public Parameters GetEditParameters(IMatrixData mdata){
			Parameters[] subParams = new Parameters[mdata.CategoryRowCount];
			for (int i = 0; i < subParams.Length; i++){
				subParams[i] = GetEditParameters(mdata, i);
			}
			List<Parameter> par = new List<Parameter>{
				new SingleChoiceWithSubParams("Category row"){
					Values = mdata.CategoryRowNames,
					SubParams = subParams,
					Help = "Select the category row that should be edited.",
					ParamNameWidth = 50
				}
			};
			return new Parameters(par);
		}

		public Parameters GetEditParameters(IMatrixData mdata, int ind){
			List<Parameter> par = new List<Parameter>();
			Dictionary<string, string> map = new Dictionary<string, string>();
			for (int i = 0; i < mdata.ColumnCount; i++){
				string t = mdata.ColumnNames[i];
				map.Add(t, StringUtils.Concat(";", mdata.GetCategoryRowAt(ind)[i]));
			}
			par.Add(new DictionaryStringValueParam("Values", map));
			return new Parameters(par);
		}

		private static void ProcessDataCreate(IDataWithAnnotationRows mdata, Parameters param){
			string name = param.GetParam<string>("Row name").Value;
			Dictionary<string, string> map = param.GetParam<Dictionary<string, string>>("Values").Value;
			string[][] groupCol = new string[mdata.ColumnCount][];
			for (int i = 0; i < mdata.ColumnCount; i++){
				string ename = mdata.ColumnNames[i];
				string value = map[ename];
				groupCol[i] = value.Length > 0 ? value.Split(';') : new string[0];
			}
			mdata.AddCategoryRow(name, name, groupCol);
		}

		private static void ProcessDataCreateFromGoupNames(IDataWithAnnotationRows mdata, Parameters param,
			ProcessInfo processInfo){
			var name = param.GetParam<string>("Name").Value;
			ParameterWithSubParams<int> scwsp = param.GetParamWithSubParams<int>("Pattern");
			Parameters spar = scwsp.GetSubParameters();
			string regexString = "";
			string replacement = "";
			switch (scwsp.Value){
				case 0:
				case 1:
				case 2:
					regexString = GetSelectableRegexes()[scwsp.Value][1];
					break;
				case 3:
					regexString = spar.GetParam<string>("Regex").Value;
					break;
				case 4:
					regexString = spar.GetParam<string>("Regex").Value;
					replacement = spar.GetParam<string>("Replace with").Value;
					break;
			}
			Regex regex;
			try{
				regex = new Regex(regexString);
			} catch (ArgumentException){
				processInfo.ErrString = "The regular expression you provided has invalid syntax.";
				return;
			}
			List<string[]> groupNames = new List<string[]>();
			foreach (string sampleName in mdata.ColumnNames){
				string groupName = scwsp.Value < 4
					? regex.Match(sampleName).Groups[1].Value
					: regex.Replace(sampleName, replacement);
				if (string.IsNullOrEmpty(groupName)){
					groupName = sampleName;
				}
				groupNames.Add(new[]{groupName});
			}
			mdata.AddCategoryRow(name, "", groupNames.ToArray());
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			SingleChoiceWithSubParams scwsp = new SingleChoiceWithSubParams("Action"){
				Values =
					new[]{
						"Create", "Create from experiment name", "Edit", "Rename", "Delete", "Write template file",
						"Read from file"
					},
				SubParams = new[]{
					GetCreateParameters(mdata), GetCreateFromExperimentNamesParameters(mdata), GetEditParameters(mdata),
					GetRenameParameters(mdata), GetDeleteParameters(mdata), GetWriteTemplateFileParameters(),
					GetReadFromFileParameters()
				},
				ParamNameWidth = 136,
				TotalWidth = 731
			};
			return new Parameters(new Parameter[]{scwsp});
		}

		public Parameters GetDeleteParameters(IMatrixData mdata){
			List<Parameter> par = new List<Parameter>{
				new SingleChoiceParam("Category row"){
					Values = mdata.CategoryRowNames, Help = "Select the category row that should be deleted."
				}
			};
			return new Parameters(par);
		}

		public Parameters GetRenameParameters(IMatrixData mdata){
			List<Parameter> par = new List<Parameter>{
				new SingleChoiceParam("Category row"){
					Values = mdata.CategoryRowNames, Help = "Select the category row that should be renamed."
				},
				new StringParam("New name"),
				new StringParam("New description")
			};
			return new Parameters(par);
		}

		public Parameters GetReadFromFileParameters(){
			List<Parameter> par = new List<Parameter>{
				new FileParam("Input file"){Filter = "Tab separated file (*.txt)|*.txt", Save = false}
			};
			return new Parameters(par);
		}

		public Parameters GetWriteTemplateFileParameters(){
			List<Parameter> par = new List<Parameter>{
				new FileParam("Output file", "Groups.txt"){Filter = "Tab separated file (*.txt)|*.txt", Save = true}
			};
			return new Parameters(par);
		}

		public Parameters GetCreateParameters(IMatrixData mdata){
			List<Parameter> par = new List<Parameter>{
				new StringParam("Row name"){Value = "Group1", Help = "Name of the new category annotation row."}
			};
			Dictionary<string, string> map = new Dictionary<string, string>();
			foreach (string t in mdata.ColumnNames){
				map.Add(t, t);
			}
			par.Add(new DictionaryStringValueParam("Values", map));
			return new Parameters(par);
		}

		/// <remarks>author: Marco Hein</remarks>>
		public Parameters GetCreateFromExperimentNamesParameters(IMatrixData mdata){
			List<string[]> selectableRegexes = GetSelectableRegexes();
			List<string> vals = new List<string>();
			foreach (string[] s in selectableRegexes){
				vals.Add(s[0]);
			}
			vals.Add("match regular expression");
			vals.Add("replace regular expression");
			List<Parameters> subparams = new List<Parameters>();
			for (int i = 0; i < selectableRegexes.Count; i++){
				subparams.Add(new Parameters(new Parameter[]{ }));
			}
			subparams.Add(new Parameters(new Parameter[]{new StringParam("Regex", "")}));
			subparams.Add(new Parameters(new StringParam("Regex", ""), new StringParam("Replace with", "")));
			return new Parameters(new StringParam("Name", "Grouping"),
				new SingleChoiceWithSubParams("Pattern", 0){
					Values = vals, SubParams = subparams, ParamNameWidth = 100, TotalWidth = 400
				});
		}

		private static List<string[]> GetSelectableRegexes(){
			return new List<string[]>{
				new[]{"..._01,02,03", "^(.*)_[0-9]*$"},
				new[]{"(LFQ) intensity ..._01,02,03", "^(?:LFQ )?[Ii]ntensity (.*)_[0-9]*$"},
				new[]{"(Normalized) ratio H/L ..._01,02,03", "^(?:Normalized )?[Rr]atio(?: [HML]/[HML]) (.*)_[0-9]*$"}
			};
		}
	}
}