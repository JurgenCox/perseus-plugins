using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Filter{
	public class FilterValidValuesColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Name => "Filter columns based on valid values";
		public string Heading => "Filter columns";
		public bool IsActive => true;
		public float DisplayRank => 3;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Filtercolumns:FilterValidValuesColumns";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Description
			=>
				"Columns of the expression matrix are filtered to contain at least the specified numbers of entries that are " +
				"valid in the specified way.";

		public string HelpOutput
			=> "The matrix of expression values is constrained to contain only these columns that fulfill the requirement.";

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
            const bool rows = false;
			int minValids = PerseusPluginUtils.GetMinValids(param, out bool percentage);
			ParameterWithSubParams<int> modeParam = param.GetParamWithSubParams<int>("Mode");
			int modeInd = modeParam.Value;
			if (modeInd != 0 && mdata.CategoryRowNames.Count == 0){
				processInfo.ErrString = "No grouping is defined.";
				return;
			}
			if (modeInd != 0){
				processInfo.ErrString = "Group-wise filtering can only be appled to rows.";
				return;
			}
			PerseusPluginUtils.ReadValuesShouldBeParams(param, out FilteringMode filterMode, out double threshold, out double threshold2);
			if (modeInd != 0){
				//TODO
			} else{
                if (param.GetParam<int>("Filter mode").Value == 2)
                {
                    supplTables = new[] { PerseusPluginUtils.NonzeroFilter1Split(rows, minValids, percentage, mdata, param, threshold, threshold2, filterMode) };
                }
                PerseusPluginUtils.NonzeroFilter1(rows, minValids, percentage, mdata, param, threshold, threshold2, filterMode);
			}
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(PerseusPluginUtils.GetMinValuesParam(mdata, false), new SingleChoiceWithSubParams("Mode"){
					Values = new[]{"In total"},
					SubParams ={new Parameters(new Parameter[0])},
					ParamNameWidth = 50,
					TotalWidth = 731
				}, PerseusPluginUtils.GetValuesShouldBeParam(), PerseusPluginUtils.CreateFilterModeParamNew(true));
		}
	}
}