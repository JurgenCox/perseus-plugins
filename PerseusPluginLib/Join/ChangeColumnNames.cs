using BaseLibS.Calc;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Vector;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PluginChangeColumnNames
{
    public class Class1 : IMatrixMultiProcessing
    {
        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Name => "Change Column Names";
        public bool IsActive => true;
        public float DisplayRank => -2;
        public string HelpOutput => "Changes the Column Names based on an input table.";
        public string Description
            =>
                "Replace Column names based a table. The first matrix contains the " +
                "column that will be edited while the second matrix is used to define the key-value table.";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;
        public int MinNumInput => 2;
        public int MaxNumInput => 2;
        public string Heading => "Basic";
        public string Url => "";

        public string GetInputName(int index)
        {
            return index == 0 ? "Base matrix" : "Other matrix";
        }

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public Parameters GetParameters(IMatrixData[] inputData, ref string errString)
        {
            IMatrixData matrixData1 = inputData[0];
            IMatrixData matrixData2 = inputData[1];

            return

                new Parameters(new MultiChoiceParam("Columns in matrix 1 to be edited")
                {
                    Values = matrixData1.ColumnNames,
                    Help =
                        "The columns in the first matrix in which strings will be replaced " +
                        "according to the key-value table specified in matrix 2."
                }, new SingleChoiceParam("Keys in matrix 2")
                {
                    Values = matrixData2.StringColumnNames,
                    Value = 0,
                    Help = "The keys for the replacement table."
                }, new SingleChoiceParam("Values in matrix 2")
                {
                    Values = matrixData2.StringColumnNames,
                    Value = 1,
                    Help = "The values for the replacement table."
                });
        }

        public IMatrixData ProcessData(IMatrixData[] inputData, Parameters parameters, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            IMatrixData mdata1 = inputData[0]; //Base matrix
            IMatrixData mdata2 = inputData[1]; // other matrix
            Dictionary<string, string> map = GetMap(inputData[1], parameters);
            IMatrixData result = (IMatrixData)mdata1.Clone();
            //create Parameters
            int[] columns = parameters.GetParam<int[]>("Columns in matrix 1 to be edited").Value;
            string[] columnNames = new string[columns.Length];

            if (mdata1 == mdata2)
                {
                    processInfo.ErrString = "Please select a Matrix to be edited an a key-value table";

                    return result;
                }
                //edit Columns based on key-value table
                for (int i = 0; i < columnNames.Length; i++)
                {
                    columnNames[i] = result.ColumnNames[columns[i]];
                    string edit = Process(columnNames[i], map);
                    result.ColumnNames[i] = edit;
                }  
                    return result;

        }
        private static Dictionary<string, string> GetMap(IMatrixData mdata2, Parameters parameters)
        {
            string[] keys = mdata2.StringColumns[parameters.GetParam<int>("Keys in matrix 2").Value];
            string[] values = mdata2.StringColumns[parameters.GetParam<int>("Values in matrix 2").Value];
            Dictionary<string, string> map = new Dictionary<string, string>();
            for (int i = 0; i < keys.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(keys[i]))
                {
                    continue;
                }
                if (!map.ContainsKey(keys[i]))
                {
                    map.Add(keys[i], values[i]);
                }
            }
            return map;
        }
        
        private static string Process(string s, Dictionary<string, string> map)
        {
            if (!s.Contains(";"))
            {
                return map.ContainsKey(s) ? map[s] : "";
            }
            string[] q = s.Split(';');
            List<string> result = new List<string>();
            foreach (string s1 in q)
            {
                if (map.ContainsKey(s1))
                {
                    result.Add(map[s1]);
                }
            }
            if (result.Count == 0)
            {
                return "";
            }
            return StringUtils.Concat(";", result);
        }
       







    }
}