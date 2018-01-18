using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.AnnotCols{
	public class CombineAnnotationColumns : IMatrixProcessing{
	    private static readonly string[] Strategies = {"Union", "Intersection", "Majority"};
	    public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Combine annotations columns using different strategies.";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Combine annotation columns";
		public string Heading => "Annot. columns";
		public bool IsActive => true;
		public float DisplayRank => 4;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotcolumns:CombineAnnotationColumns";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> choice = mdata.CategoryColumnNames;
		    return new Parameters(
                new StringParam("Name")
                {
                    Help = "New column name",
                },
                new MultiChoiceParam("Columns")
                {
                    Values = choice,
                    Help = "Choose which columns are combined"
                },
                new SingleChoiceParam("Strategy")
		        {
		            Values = Strategies,
                    Help = "Choose the strategy by which each of the rows are combined into one new value.\nUnion: Combine all values in the row.\nIntersection: Values that occur in all rows.\nMajority: Choose the value that occurs most often. Ties are broken by sorting."
		        },
                new BoolParam("Keep original columns", false)
                {
                    Help = "Check to keep the original columns."
                });
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
		{
		    var name = param.GetParam<string>("Name").Value;
		    var keep = param.GetParam<bool>("Keep original columns").Value;
		    var choice = param.GetParam<int[]>("Columns").Value;
		    var strategy = Strategies[param.GetParam<int>("Strategy").Value];
		    var combiner = GetCombiner(strategy);
		    var columns = choice.Select(mdata.GetCategoryColumnAt).ToArray();
		    var n = mdata.RowCount;
		    var values = new string[n][];
		    for (int i = 0; i < mdata.RowCount; i++)
		    {
		        var row = columns.Select(col => col[i]).ToArray();
		        values[i] = combiner(row);
		    }
		    if (!keep)
		    {
                // if unsorted removing column will change index of other columns
		        foreach (var col in choice.OrderByDescending(col => col))
		        {
		            mdata.RemoveCategoryColumnAt(col);
		        }
		    }
            mdata.AddCategoryColumn(name, "Combined column", values);
		}

	    private static Func<string[][], string[]> GetCombiner(string strategy)
	    {
	        switch (strategy)
	        {
                case "Union":
                    return Union;
                case "Intersection":
                    return Intersection;
                case "Majority":
                    return Majority;
	        }
            throw new NotImplementedException($"Strategy {strategy} not implemented.");
	    }

	    public static string[] Union(IEnumerable<string[]> values)
	    {
	        return values.SelectMany(value => value).Distinct().OrderBy(value => value).ToArray();
	    }

	    public static string[] Intersection(IEnumerable<string[]> values)
	    {
	        var valueArray = values.ToArray();
	        return valueArray.SelectMany(value => value)
	            .GroupBy(value => value)
	            .Where(grp => grp.Count() == valueArray.Length)
	            .Select(grp => grp.Key)
	            .OrderBy(value => value)
	            .ToArray();
	    }

	    public static string[] Majority(IEnumerable<string[]> values)
	    {
	        return values.SelectMany(value => value.DefaultIfEmpty(string.Empty))
                .GroupBy(value => value)
                .OrderByDescending(grp => grp.Count())
                .ThenBy(grp => grp.Key)
                .Select(grp => grp.Key)
                .Take(1)
                .Where(value => !string.IsNullOrEmpty(value))
                .ToArray();
	    }
    }
}