using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Parse;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange
{
        public class RenameColumnMetabolomics : IMatrixProcessing
        {
            public bool HasButton => false;
            public Bitmap2 DisplayImage => null;

            public string Description
                => "New names can be specified for each expression column. The new names are typed in explicitly.";

            public string HelpOutput => "Same matrix but with the new expression column names.";
            public string[] HelpSupplTables => new string[0];
            public int NumSupplTables => 0;
            public string Name => "Rename columns";
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


        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
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
