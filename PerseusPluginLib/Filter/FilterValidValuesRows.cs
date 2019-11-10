using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Filter{
	internal enum FilteringMode{
		Valid,
		GreaterThan,
		GreaterEqualThan,
		LessThan,
		LessEqualThan,
		Between,
		Outside
	}

	public class FilterValidValuesRows : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("missingsButton.Image.png");
		public string Name => "Filter rows based on valid values";
		public string Heading => "Filter rows";
		public bool IsActive => true;
		public float DisplayRank => 3;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Filterrows:FilterValidValuesRows";

		public string Description
			=>
				"Rows of the expression matrix are filtered to contain at least the specified numbers of entries that are " +
				"valid in the specified way.";

		public string HelpOutput
			=> "The matrix of expression values is constrained to contain only these rows that fulfil the requirement.";

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
            const bool rows = true;
			int minValids = PerseusPluginUtils.GetMinValids(param, out bool percentage);
			ParameterWithSubParams<int> modeParam = param.GetParamWithSubParams<int>("Mode");
            int modeInd = modeParam.Value;
			if (modeInd != 0 && mdata.CategoryRowNames.Count == 0){
				processInfo.ErrString = "No grouping is defined.";
				return;
			}
			PerseusPluginUtils.ReadValuesShouldBeParams(param, out FilteringMode filterMode, out double threshold, out double threshold2);
			if (modeInd != 0){
				int gind = modeParam.GetSubParameters().GetParam<int>("Grouping").Value;
				string[][] groupCol = mdata.GetCategoryRowAt(gind);
				NonzeroFilterGroup(minValids, percentage, mdata, param, modeInd == 2, threshold, threshold2, filterMode, groupCol);
			} else{
				PerseusPluginUtils.NonzeroFilter1(rows, minValids, percentage, mdata, param, threshold, threshold2, filterMode);
			}
        }

		private static void NonzeroFilterGroup(int minValids, bool percentage, IMatrixData mdata, Parameters param,
			bool oneGroup, double threshold, double threshold2, FilteringMode filterMode, IList<string[]> groupCol){
			List<int> valids = new List<int>();
			string[] groupVals = ArrayUtils.UniqueValuesPreserveOrder(groupCol);
			Array.Sort(groupVals);
			int[][] groupInds = CalcGroupInds(groupVals, groupCol);
			for (int i = 0; i < mdata.RowCount; i++){
				int[] counts = new int[groupVals.Length];
				int[] totals = new int[groupVals.Length];
				for (int j = 0; j < groupInds.Length; j++){
					for (int k = 0; k < groupInds[j].Length; k++){
						if (groupInds[j][k] >= 0){
							totals[groupInds[j][k]]++;
						}
					}
					if (PerseusPluginUtils.IsValid(mdata.Values.Get(i, j), threshold, threshold2, filterMode)){
						for (int k = 0; k < groupInds[j].Length; k++){
							if (groupInds[j][k] >= 0){
								counts[groupInds[j][k]]++;
							}
						}
					}
				}
				bool[] groupValid = new bool[counts.Length];
				for (int j = 0; j < groupValid.Length; j++){
					groupValid[j] = PerseusPluginUtils.Valid(counts[j], minValids, percentage, totals[j]);
				}
				if (oneGroup ? ArrayUtils.Or(groupValid) : ArrayUtils.And(groupValid)){
					valids.Add(i);
				}
			}
			PerseusPluginUtils.FilterRowsNew(mdata, param, valids.ToArray());
		}

		private static int[][] CalcGroupInds(string[] groupVals, IList<string[]> groupCol){
			int[][] result = new int[groupCol.Count][];
			for (int i = 0; i < result.Length; i++){
				result[i] = new int[groupCol[i].Length];
				for (int j = 0; j < result[i].Length; j++){
					result[i][j] = Array.BinarySearch(groupVals, groupCol[i][j]);
				}
			}
			return result;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
            return
				new Parameters(PerseusPluginUtils.GetMinValuesParam(mdata, true), new SingleChoiceWithSubParams("Mode"){
					Values = new[]{"In total", "In each group", "In at least one group"},
					SubParams =new[]{
						new Parameters(new Parameter[0]),
						new Parameters(new Parameter[]{new SingleChoiceParam("Grouping"){Values = mdata.CategoryRowNames}}),
						new Parameters(new Parameter[]{new SingleChoiceParam("Grouping"){Values = mdata.CategoryRowNames}})
					},
					ParamNameWidth = 50,
					TotalWidth = 731
				}, PerseusPluginUtils.GetValuesShouldBeParam(), PerseusPluginUtils.CreateFilterModeParam(false));
		}
	}
}