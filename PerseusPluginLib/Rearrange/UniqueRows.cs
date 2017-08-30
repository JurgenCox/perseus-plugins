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

namespace PerseusPluginLib.Rearrange{
	public class UniqueRows : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

	    public string Description => "Combines rows with identical values in the specified columns";
		public string Name => "Unique rows";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 16;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string HelpOutput => "";
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:UniqueValues";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

	     static string _unique = "Id column";
	     static string _string = "Combine text columns by";
	     static string _numeric = "Combine numeric columns by";
	     static string _multi_numeric = "Combine multi numeric columns by";
	     static string _category = "Combine category/text columns by";

         static string[] _numeric_choices = { "median" };
         static string[] _multi_numeric_choices = { "concatenation" };
         static string[] _category_choices = { "union" };
         static string[] _string_choices = { _str_union_split };

	     const string _str_union_split = "Union, split on ';'";

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            var ids = mdata.StringColumns[param.GetParam<int>(_unique).Value];
			ParseParameters(param, out Func<double[], double> combineNumeric, out Func<string[], string> combineString, out Func<string[][], string[]> combineCategory, out Func<double[][], double[]> combineMultiNumeric);
			mdata.UniqueRows(ids, combineNumeric, combineString, combineCategory, combineMultiNumeric);
        }

	    private void ParseParameters(Parameters param, out Func<double[], double> combineNumeric, out Func<string[], string> combineString,
	        out Func<string[][], string[]> combineCategory, out Func<double[][], double[]> combineMultiNumeric)
	    {
	        var combineNumericParm = _numeric_choices[param.GetParam<int>(_numeric).Value];
	        switch (combineNumericParm)
	        {
	            case "median":
	                combineNumeric = ArrayUtils.Median;
	                break;
	            default:
	                throw new NotImplementedException($"Method {combineNumericParm} is not implemented");
	        }
	        var combineStringParam = _string_choices[param.GetParam<int>(_string).Value];
	        switch (combineStringParam)
	        {
	            case _str_union_split:
	                combineString = Union;
	                break;
	            default:
	                throw new NotImplementedException($"Method {combineStringParam} is not implemented");
	        }
	        var combineCategoryParam = _category_choices[param.GetParam<int>(_category).Value];
	        switch (combineCategoryParam)
	        {
	            case "union":
	                combineCategory = CatUnion;
	                break;
	            default:
	                throw new NotImplementedException($"Method {combineCategoryParam} is not implemented");
	        }
	        var combineMultiNumericParam = _multi_numeric_choices[param.GetParam<int>(_multi_numeric).Value];
	        switch (combineMultiNumericParam)
	        {
	            case "concatenation":
	                combineMultiNumeric = MultiNumUnion;
	                break;
	            default:
	                throw new NotImplementedException($"Method {combineMultiNumericParam} is not implemented");
	        }
	    }

	    public static double[] MultiNumUnion(params double[][] nums)
	    {
	        return nums.SelectMany(x => x).ToArray();
	    }

	    public static string[] CatUnion(params string[][] strs)
	    {
	        return strs.SelectMany(x => x).Distinct().ToArray();
	    } 

	    public static string Union(params string[] strs)
	    {
	        return string.Join(";", strs.SelectMany(x => x.Split(';')).Distinct());
	    } 

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            return new Parameters(
                new SingleChoiceParam(_unique)
                {
                    Values = mdata.StringColumnNames
                },
                new SingleChoiceParam(_string)
                {
                    Values = _string_choices
                },
                new SingleChoiceParam(_numeric)
                {
                    Values = _numeric_choices
                },
                new SingleChoiceParam(_category)
                {
                    Values = _category_choices
                },
                new SingleChoiceParam(_multi_numeric)
                {
                    Values = _multi_numeric_choices
                }
            );
        }

    }
}