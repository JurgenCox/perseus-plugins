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
                    string err = ProcessDataReadFromFileList(mdata, spar);
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
            Dictionary<string, int> map = ArrayUtils.InverseMap(nameCol);
            for (int i = 0; i < colNames.Length; i++)
            {
                if (i == nameIndex)
                {
                    continue;
                }
                string groupName = colNames[i];
                string[] groupCol = TabSep.GetColumn(groupName, filename, '\t');
                string[][] newCol = new string[mdata.ColumnCount][];
                for (int j = 0; j < newCol.Length; j++)
                {
                    string colName = mdata.ColumnNames[j];
                    if (!map.ContainsKey(colName))
                    {
                        newCol[j] = new string[0];
                        continue;
                    }
                    int ind = map[colName];
                    string group = groupCol[ind] ?? "";
                    group = group.Trim();
                    if (string.IsNullOrEmpty(group))
                    {
                        newCol[j] = new string[0];
                    }
                    else
                    {
                        string[] w = group.Split(';');
                        Array.Sort(w);
                        for (int k = 0; k < w.Length; k++)
                        {
                            w[k] = w[k].Trim();
                        }
                        newCol[j] = w;
                    }
                }
                mdata.AddCategoryRow(groupName, groupName, newCol);
            }
            return null;
        }


        private static string ProcessDataReadFromFileList(IDataWithAnnotationRows mdata, Parameters param)
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


        private static void ProcessDataWriteTemplateFile(IDataWithAnnotationRows mdata, Parameters param)
        {
            Parameter<string> fp = param.GetParam<string>("Output file");
            StreamWriter writer = new StreamWriter(fp.Value);
            writer.WriteLine("ColumnNames");
            for (int i = 0; i < mdata.ColumnCount; i++)
            {
                string colName = mdata.ColumnNames[i];
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
