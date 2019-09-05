using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
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
using System.Windows.Forms;
using System.Linq;

namespace PerseusPluginLib.Rearrange
{
    public class RenameColumnTemplate : IMatrixProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;

        public string Description
            => "New names can be specified for each expression column. The new names are typed in explicitly.";

        public string HelpOutput => "Same matrix but with the new expression column names.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Rename columns Template";
        public string Heading => "Rearrange";
        public bool IsActive => false;
        public float DisplayRank => 0;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;

        public string Url
            => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:RenameColumns";

        public int GetMaxThreads(Parameters parameters)
        {
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
                    ProcessDataCreate(mdata, spar, processInfo);
                    break;
                case 1:
                    ProcessDataWriteTemplateFile(mdata, spar);
                    break;
                case 2:
                    string err = ProcessDataReadFromFileListNew(mdata, spar);
                    if (err != null)
                    {
                        processInfo.ErrString = err;
                    }
                    break; 
            }
        }

        private static string ProcessDataReadFromFile(IDataWithAnnotationRows mdata, Parameters param)
        {
            Parameter<string> fp = param.GetParam<string>("Input file");
            string filename = fp.Value;
            string[] colNames;
            try
            {
                colNames = TabSep.GetColumnNames(filename, '\t');
            }
            catch (Exception)
            {
                return "Could not open file " + filename + ". It maybe open in another program.";
            }
            int nameIndex = GetNameIndex(colNames);
            if (nameIndex < 0)
            {
                return "Error: the file has to contain a column called 'Name'.";
            }
            if (colNames.Length < 2)
            {
                return "Error: the file does not contain a grouping column.";
            }
            string[] nameCol = TabSep.GetColumn(colNames[nameIndex], filename, '\t');
            for (int i = 0; i < nameCol.Length; i++)
            {
                MessageBox.Show(nameCol[i].ToString());
            }
                return null;
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
           // string filename = fp.Value;
            string[] newNames = new string[mdata.ColumnCount];
            List<string> expressionColumnNames = new List<string>();
            try
            {
                using (var reader = new StreamReader(new FileStream(fp, FileMode.Open)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                            var colnames = mdata.ColumnNames.Select(columnName => new { Old = columnName, New = line }).ToList();
                            expressionColumnNames.Add(line);
                    }
                    newNames = GetStringArray(expressionColumnNames);
                    for (int i = 0; i < mdata.ColumnCount; i++)
                    {
                        if (newNames[i] != mdata.ColumnNames[i])
                            mdata.ColumnNames[i] = newNames[i];
                    }
                    for (int i = 0; i < mdata.CategoryColumnCount; i++)
                    {
                        if (newNames[i] != mdata.CategoryColumnNames[i])
                            mdata.CategoryColumnNames[i] = newNames[i];
                    }
                    for (int i = 0; i < mdata.StringColumnCount; i++)
                    {
                        if (newNames[i] != mdata.StringColumnNames[i])
                            mdata.StringColumnNames[i] = newNames[i];
                    }

                }
            }
            catch (Exception)
            {
                return "The File cannot be opened. It might be open in another software.";
            }
            return null;
        }

        private static string ProcessDataReadFromFileList(IMatrixData mdata, Parameters param)
        {
            Parameter<string> fp = param.GetParam<string>("Input file");
            string filename = fp.Value;
            string[] colNames;
            try
            {
                colNames = TabSep.GetColumnNames(filename, '\t');
            }
            catch (Exception)
            {
                return "Could not open file " + filename + ". It maybe open in another program.";
            }
            int nameIndex = GetNameIndex(colNames);
            string[] nameCol = TabSep.GetColumn(colNames[nameIndex], filename, '\t');
            MessageBox.Show(nameCol.Length.ToString());
            MessageBox.Show(mdata.ColumnCount.ToString());
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                MessageBox.Show(nameCol.Length.ToString());
                MessageBox.Show(mdata.ColumnCount.ToString());
            /*    if (nameCol.Length != mdata.ColumnCount)
                    {
                        return "ERROR";
                    }
                    else { mdata.ColumnNames[i] = nameCol[i]; } */
            }
            
            return null;
        }

        private static int GetNameIndex(IList<string> colNames)
        {
            for (int i = 0; i < colNames.Count; i++)
            {
                if (colNames[i].ToLower().Equals("ColumnNames"))
                {
                    return i;
                }
            }
            return -1;
        }

        private static List<string[]> GetSelectableRegexes()
        {
            return new List<string[]> {
                new[] {"..._01,02,03", "^(.*)_[0-9]*$"},
                new[] {"(LFQ) intensity ..._01,02,03", "^(?:LFQ )?[Ii]ntensity (.*)_[0-9]*$"},
                new[] {"(Normalized) ratio H/L ..._01,02,03", "^(?:Normalized )?[Rr]atio(?: [HML]/[HML]) (.*)_[0-9]*$"}
            };
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
                writer.WriteLine(colName);
            }
            for (int i = 0; i < mdata.NumericColumnCount; i++)
            {
                string colName = mdata.NumericColumnNames[i];
                writer.WriteLine(colName);
            }
            for (int i = 0; i < mdata.CategoryColumnCount; i++)
            {
                string colName = mdata.CategoryColumnNames[i];
                writer.WriteLine(colName);
            }
            for (int i = 0; i < mdata.StringColumnCount; i++)
            {
                string colName = mdata.StringColumnNames[i];
                writer.WriteLine(colName);
            }
            for (int i = 0; i < mdata.MultiNumericColumnCount; i++)
            {
                string colName = mdata.MultiNumericColumnNames[i];
                writer.WriteLine(colName);
            }
            writer.Close();
        }
        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            SingleChoiceWithSubParams scwsp = new SingleChoiceWithSubParams("Action")
            {
                Values =
                    new[] {
                        "Edit" , "Write template file", "Read from file"
                     // , "Rename", "Delete",
                   //     "Write template file", "Read from file"
                    },
                SubParams = new[] {
                    GetEditParameters(mdata), GetWriteTemplateFileParameters(mdata),
                    // GetRenameParameters(mdata), GetDeleteParameters(mdata),
                 //   GetWriteTemplateFileParameters(mdata), 
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

        public void ProcessDataCreate(IMatrixData mdata, Parameters param,  ProcessInfo processInfo)
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
