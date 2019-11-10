using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Filter{
	public class FilterDuplicateRows : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Remove duplicate rows based on the specified columns. Caution! Limited numerical accuracy: Uses textual representation of values.";
		public string HelpOutput => "The filtered matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Filter rows based on duplicate values";
		public string Heading => "Filter rows";
		public bool IsActive => true;
		public float DisplayRank => 10;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Filterrows:FilterRandomRows";

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(
                new MultiChoiceParam("Main", Enumerable.Range(0, mdata.ColumnCount).ToArray())
                {
                    Values = mdata.ColumnNames
                },
                new MultiChoiceParam("Numeric", Enumerable.Range(0, mdata.NumericColumnCount).ToArray())
                {
                    Values = mdata.NumericColumnNames
                },
                new MultiChoiceParam("Text", Enumerable.Range(0, mdata.StringColumnCount).ToArray())
                {
                    Values = mdata.StringColumnNames
                },
                new MultiChoiceParam("Category", Enumerable.Range(0, mdata.CategoryColumnCount).ToArray())
                {
                    Values = mdata.CategoryColumnNames
                },
                new MultiChoiceParam("MultiNumeric", Enumerable.Range(0, mdata.MultiNumericColumnCount).ToArray())
                {
                    Values = mdata.MultiNumericColumnNames
                },
                PerseusPluginUtils.CreateFilterModeParam(true));
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo)
		{
            var mainSubset = param.GetParam<int[]>("Main").Value;
		    var mainColumns = mainSubset.Select(mdata.Values.GetColumn).ToArray();
		    var numericSubset = param.GetParam<int[]>("Numeric").Value;
		    var numericColumns = ArrayUtils.SubList(mdata.NumericColumns, numericSubset);
		    var stringSubset = param.GetParam<int[]>("Text").Value;
		    var stringColumns = ArrayUtils.SubList(mdata.StringColumns, stringSubset);
		    var categorySubset = param.GetParam<int[]>("Category").Value;
		    var categoryColumns = categorySubset.Select(mdata.GetCategoryColumnAt).ToArray();
		    var multiNumericSubset = param.GetParam<int[]>("MultiNumeric").Value;
		    var multiNumericColumns = ArrayUtils.SubList(mdata.MultiNumericColumns, multiNumericSubset);
		    var rows = new Dictionary<string, int>();
            var discardrows = new Dictionary<string, int>();
            for (int j = 0; j < mdata.RowCount; j++)
		    {
		        int i = j;
		        var row = string.Join("\t", mainColumns.Select(col => $"{col[i]}")
		            .Concat(numericColumns.Select(col => $"{col[i]}"))
		            .Concat(stringColumns.Select(col => $"{col[i]}"))
                    .Concat(categoryColumns.Select(col => string.Join(";", col[i])))
                    .Concat(multiNumericColumns.Select(col => string.Join(";", col[i].Select(d => $"{d}")))));
		        if (!rows.ContainsKey(row))
		        {
		            rows[row] = i;
		        } 
            }

            PerseusPluginUtils.FilterRowsNew(mdata, param, rows.Values.ToArray());
            if (param.GetParam<int>("Filter mode").Value == 2)
            {
                for (int j = 0; j < mdata.RowCount; j++)
                {
                    int i = j;
                    var row = string.Join("\t", mainColumns.Select(col => $"{col[i]}")
                        .Concat(numericColumns.Select(col => $"{col[i]}"))
                        .Concat(stringColumns.Select(col => $"{col[i]}"))
                        .Concat(categoryColumns.Select(col => string.Join(";", col[i])))
                        .Concat(multiNumericColumns.Select(col => string.Join(";", col[i].Select(d => $"{d}")))));
                    if (rows.ContainsKey(row))
                    {
                        discardrows[row] = j;
                    }
                }
                    supplTables = new[] { PerseusPluginUtils.CreateSupplTabSplit(mdata, discardrows.Values.ToArray()) };
            }
        }
	}
}