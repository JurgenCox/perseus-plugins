using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public class RenameColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=> "New names can be specified for each expression column. The new names are typed in explicitly.";

		public string HelpOutput => "Same matrix but with the new expression column names.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Rename columns";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:RenameColumns";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
          ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            ParameterWithSubParams<int> scwsp = param.GetParamWithSubParams<int>("Action");
            Parameters spar = scwsp.GetSubParameters();
            switch (scwsp.Value)
            {
                case 0:
                    ProcessDataCreate(mdata, spar);
                    break;
                case 1:
                    ProcessDataCreate(mdata, spar, processInfo);
                    break;
                case 2:
                    ProcessDataWriteTemplateFile(mdata, spar);
                    break;
                case 3:
                    string err = ProcessDataReadFromFileListNew(mdata, spar);
                    if (err != null)
                    {
                        processInfo.ErrString = err;
                    }
                    break;
            }
        }


        private static void ProcessDataCreate(IMatrixData mdata, Parameters param)
        {
            Dictionary<string, string> map = param.GetParam<Dictionary<string, string>>("Values").Value;
            string[] newNames = new string[mdata.ColumnCount];
            List<string> expressionColumnNames = new List<string>();
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                string ename = mdata.ColumnNames[i];
                string value = map[ename];
                expressionColumnNames.Add(value);
            }
            newNames = GetStringArray(expressionColumnNames);
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                if (newNames[i] != mdata.ColumnNames[i])
                    mdata.ColumnNames[i] = newNames[i];
            }
        }
        private static string[] GetStringArray(List<string> getlistpoli)
        {
            List<string> doubles = getlistpoli.Select(i => (string)i).ToList();
            string[] check = doubles.ToArray();

            return check;
        }
        private static string ProcessDataReadFromFileListNew(IMatrixData mdata, Parameters param)
        {
            var fp = param.GetParam<string>("Input file").Value;
            string[] newNames = new string[mdata.ColumnCount];
            string[] newNamesCategory = new string[mdata.ColumnCount];
            string[] newNamesString = new string[mdata.ColumnCount];
            string[] newNamesNumeric = new string[mdata.ColumnCount];
            string[] newNamesMultiNumeric = new string[mdata.ColumnCount];
            List<string> expressionColumnNames = new List<string>();
            List<string> expressionColumnCategory = new List<string>();
            List<string> expressionColumnString = new List<string>();
            List<string> expressionColumnNumeric = new List<string>();
            List<string> expressionColumnMultiNumeric = new List<string>();
            try
            {
                using (var reader = new StreamReader(new FileStream(fp, FileMode.Open)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var colnames = mdata.ColumnNames.Select(columnName => new { Old = columnName, New = line }).ToList();
                        if (line.StartsWith("D:"))
                        {
                            expressionColumnNames.Add(line.Substring(2, line.Length - 2));
                        }
                        else
                       if (line.StartsWith("C:"))
                        {
                            expressionColumnCategory.Add(line.Substring(2, line.Length - 2));
                        }
                        if (line.StartsWith("S:"))
                        {
                            expressionColumnString.Add(line.Substring(2, line.Length - 2));
                        }
                        if (line.StartsWith("N:"))
                        {
                            expressionColumnNumeric.Add(line.Substring(2, line.Length - 2));
                        }
                        if (line.StartsWith("M:"))
                        {
                            expressionColumnMultiNumeric.Add(line.Substring(2, line.Length - 2));
                        }
                    }
                    newNames = GetStringArray(expressionColumnNames);
                    newNamesCategory = GetStringArray(expressionColumnCategory);
                    newNamesString = GetStringArray(expressionColumnString);
                    newNamesNumeric = GetStringArray(expressionColumnNumeric);
                    newNamesMultiNumeric = GetStringArray(expressionColumnMultiNumeric);
                    for (int i = 0; i < mdata.ColumnCount; i++)
                    {
                        if (newNames[i] != mdata.ColumnNames[i])
                            mdata.ColumnNames[i] = newNames[i];
                    }
                    for (int i = 0; i < mdata.CategoryColumnCount; i++)
                    {
                        if (newNamesCategory[i] != mdata.CategoryColumnNames[i])
                            mdata.CategoryColumnNames[i] = newNamesCategory[i];
                    }
                    for (int i = 0; i < mdata.StringColumnCount; i++)
                    {
                        if (newNamesString[i] != mdata.StringColumnNames[i])
                            mdata.StringColumnNames[i] = newNamesString[i];
                    }
                    for (int i = 0; i < mdata.NumericColumnCount; i++)
                    {
                        if (newNamesNumeric[i] != mdata.NumericColumnNames[i])
                            mdata.NumericColumnNames[i] = newNamesNumeric[i];
                    }
                    for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
                    {
                        if (newNamesMultiNumeric[i] != mdata.MultiNumericColumnNames[i])
                            mdata.MultiNumericColumnNames[i] = newNamesMultiNumeric[i];
                    }
                }
            }
            catch (Exception)
            {
                return "The File cannot be opened. It might be open in another software.";
            }
            return null;
        }

        public Parameters GetWriteTemplateFileParameters(IMatrixData mdata)
        {
            List<Parameter> par = new List<Parameter> {
                new FileParam("Output file", "ColumnNames.txt") {Filter = "Tab separated file (*.txt)|*.txt", Save = true}
            };
            return new Parameters(par);
        }


        private static void ProcessDataWriteTemplateFile(IMatrixData mdata, Parameters param)
        {
            Parameter<string> fp = param.GetParam<string>("Output file");
            StreamWriter writer = new StreamWriter(fp.Value);
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                string colName = mdata.ColumnNames[i];
                writer.WriteLine("D:" + colName);
            }
            for (int i = 0; i < mdata.NumericColumnCount; i++)
            {
                string colName = mdata.NumericColumnNames[i];
                writer.WriteLine("N:" + colName);
            }
            for (int i = 0; i < mdata.CategoryColumnCount; i++)
            {
                string colName = mdata.CategoryColumnNames[i];
                writer.WriteLine("C:" + colName);
            }
            for (int i = 0; i < mdata.StringColumnCount; i++)
            {
                string colName = mdata.StringColumnNames[i];
                writer.WriteLine("S:" + colName);
            }
            for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
            {
                string colName = mdata.MultiNumericColumnNames[i];
                writer.WriteLine("M:" + colName);
            }
            writer.Close();
        }
        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            SingleChoiceWithSubParams scwsp = new SingleChoiceWithSubParams("Action")
            {
                Values =
                    new[] {
                       "Create", "Edit" , "Write template file", "Read from file"
                    },
                SubParams = new[] {
                    GetCreateParameters(mdata), GetEditParameters(mdata), GetWriteTemplateFileParameters(mdata),
                    GetReadFromFileParameters(mdata)
                },
                ParamNameWidth = 136,
                TotalWidth = 731
            };
            return new Parameters(new Parameter[] { scwsp });
        }


        public Parameters GetReadFromFileParameters(IMatrixData mdata)
        {
            List<Parameter> par = new List<Parameter> {
                new FileParam("Input file") {Filter = "Tab separated file (*.txt)|*.txt", Save = false}
            };
            return new Parameters(par);
        }


        public Parameters GetCreateParameters(IMatrixData mdata)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (string t in mdata.ColumnNames)
            {
                map.Add(t, t);
            }
             List<Parameter> par = new List<Parameter>{
            new DictionaryStringValueParam("Values", map)
             };
            return new Parameters(par);
        }
        public void ProcessDataCreate(IMatrixData mdata, Parameters param, ProcessInfo processInfo)
        {
            List<string> expressionColumnNames = new List<string>();
            HashSet<string> taken = new HashSet<string>();
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                string newName = param.GetParam<string>(mdata.ColumnNames[i]).Value;
                if (taken.Contains(newName))
                {
                    processInfo.ErrString = "Name " + newName + " is contained multiple times";
                    return;
                }
                taken.Add(newName);
                expressionColumnNames.Add(newName);
            }
            mdata.ColumnNames = expressionColumnNames;
            taken = new HashSet<string>();
            List<string> numericColumnNames = new List<string>();
            for (int i = 0; i < mdata.NumericColumnCount; i++)
            {
                string newName = param.GetParam<string>(mdata.NumericColumnNames[i]).Value;
                if (taken.Contains(newName))
                {
                    processInfo.ErrString = "Name " + newName + " is contained multiple times";
                    return;
                }
                taken.Add(newName);
                numericColumnNames.Add(newName);
            }
            mdata.NumericColumnNames = numericColumnNames;
            taken = new HashSet<string>();
            List<string> categoryColumnNames = new List<string>();
            for (int i = 0; i < mdata.CategoryColumnCount; i++)
            {
                string newName = param.GetParam<string>(mdata.CategoryColumnNames[i]).Value;
                if (taken.Contains(newName))
                {
                    processInfo.ErrString = "Name " + newName + " is contained multiple times";
                    return;
                }
                taken.Add(newName);
                categoryColumnNames.Add(newName);
            }
            mdata.CategoryColumnNames = categoryColumnNames;
            taken = new HashSet<string>();
            List<string> stringColumnNames = new List<string>();
            for (int i = 0; i < mdata.StringColumnCount; i++)
            {
                string newName = param.GetParam<string>(mdata.StringColumnNames[i]).Value;
                if (taken.Contains(newName))
                {
                    processInfo.ErrString = "Name " + newName + " is contained multiple times";
                    return;
                }
                taken.Add(newName);
                stringColumnNames.Add(newName);
            }
            mdata.StringColumnNames = stringColumnNames;
            taken = new HashSet<string>();
            List<string> multiNumericColumnNames = new List<string>();
            for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
            {
                string newName = param.GetParam<string>(mdata.MultiNumericColumnNames[i]).Value;
                if (taken.Contains(newName))
                {
                    processInfo.ErrString = "Name " + newName + " is contained multiple times";
                    return;
                }
                taken.Add(newName);
                multiNumericColumnNames.Add(newName);
            }
            mdata.MultiNumericColumnNames = multiNumericColumnNames;
        }
        public Parameters GetEditParameters(IMatrixData mdata)
        {
            List<Parameter> par = new List<Parameter>();
            foreach (string t in mdata.ColumnNames)
            {
                string help = "Specify the new name for the column '" + t + "'.";
                par.Add(new StringParam(t) { Value = t, Help = help });
            }
            foreach (string t in mdata.NumericColumnNames)
            {
                string help = "Specify the new name for the column '" + t + "'.";
                par.Add(new StringParam(t) { Value = t, Help = help });
            }
            foreach (string t in mdata.CategoryColumnNames)
            {
                string help = "Specify the new name for the column '" + t + "'.";
                par.Add(new StringParam(t) { Value = t, Help = help });
            }
            foreach (string t in mdata.StringColumnNames)
            {
                string help = "Specify the new name for the column '" + t + "'.";
                par.Add(new StringParam(t) { Value = t, Help = help });
            }
            foreach (string t in mdata.MultiNumericColumnNames)
            {
                string help = "Specify the new name for the column '" + t + "'.";
                par.Add(new StringParam(t) { Value = t, Help = help });
            }
            return new Parameters(par);
        }
    }
}