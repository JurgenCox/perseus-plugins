using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Group{
	public class CreateNumericalAnnotRow : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"Add or edit numerical annotation rows. This could for instance " +
				"define the times of samples for time series data.";

		public string HelpOutput => "Same matrix with numerical annotation row added or modified.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Numerical annotation rows";
		public string Heading => "Annot. rows";
		public bool IsActive => true;
		public float DisplayRank => 2;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotrows:CreateNumericalAnnotRow";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			ParameterWithSubParams<int> scwsp = param.GetParamWithSubParams<int>("Action");
			Parameters spar = scwsp.GetSubParameters();
			switch (scwsp.Value){
				case 0:
					ProcessDataCreate(mdata, spar);
					break;
				case 1:
					ProcessDataEdit(mdata, spar);
					break;
				case 2:
					ProcessDataRename(mdata, spar);
					break;
				case 3:
					ProcessDataDelete(mdata, spar);
					break;
			}
		}

		private static void ProcessDataRename(IDataWithAnnotationRows mdata, Parameters param){
			int groupColInd = param.GetParam<int>("Numerical row").Value;
			string newName = param.GetParam<string>("New name").Value;
			string newDescription = param.GetParam<string>("New description").Value;
			mdata.NumericRowNames[groupColInd] = newName;
			mdata.NumericRowDescriptions[groupColInd] = newDescription;
		}

		private static void ProcessDataDelete(IDataWithAnnotationRows mdata, Parameters param){
			int groupColInd = param.GetParam<int>("Numerical row").Value;
			mdata.NumericRows.RemoveAt(groupColInd);
			mdata.NumericRowNames.RemoveAt(groupColInd);
			mdata.NumericRowDescriptions.RemoveAt(groupColInd);
		}

		private static void ProcessDataEdit(IDataWithAnnotationRows mdata, Parameters param){
			ParameterWithSubParams<int> s = param.GetParamWithSubParams<int>("Numerical row");
			int groupColInd = s.Value;
			Parameters sp = s.GetSubParameters();
			for (int i = 0; i < mdata.ColumnCount; i++){
				string t = mdata.ColumnNames[i];
				double x = sp.GetParam<double>(t).Value;
				mdata.NumericRows[groupColInd][i] = x;
			}
		}

		public Parameters GetEditParameters(IMatrixData mdata){
			Parameters[] subParams = new Parameters[mdata.NumericRowCount];
			for (int i = 0; i < subParams.Length; i++){
				subParams[i] = GetEditParameters(mdata, i);
			}
			List<Parameter> par = new List<Parameter>{
				new SingleChoiceWithSubParams("Numerical row"){
					Values = mdata.NumericRowNames,
					SubParams = subParams,
					Help = "Select the numerical row that should be edited."
				}
			};
			return new Parameters(par);
		}

		public Parameters GetEditParameters(IMatrixData mdata, int ind){
			List<Parameter> par = new List<Parameter>();
			for (int i = 0; i < mdata.ColumnCount; i++){
				string t = mdata.ColumnNames[i];
				string help = "Specify a numerical value for the column '" + t + "'.";
				par.Add(new DoubleParam(t, mdata.NumericRows[ind][i]){Help = help});
			}
			return new Parameters(par);
		}

		private static void ProcessDataCreate(IDataWithAnnotationRows mdata, Parameters param){
			string name = param.GetParam<string>("Row name").Value;
			double[] groupCol = new double[mdata.ColumnCount];
			for (int i = 0; i < mdata.ColumnCount; i++){
				string ename = mdata.ColumnNames[i];
				double value = param.GetParam<double>(ename).Value;
				groupCol[i] = value;
			}
			mdata.AddNumericRow(name, name, groupCol);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			SingleChoiceWithSubParams scwsp = new SingleChoiceWithSubParams("Action"){
				Values = new[]{"Create", "Edit", "Rename", "Delete"},
				SubParams =
					new[]{GetCreateParameters(mdata), GetEditParameters(mdata), GetRenameParameters(mdata), GetDeleteParameters(mdata)},
				ParamNameWidth = 136,
				TotalWidth = 731
			};
			return new Parameters(new Parameter[]{scwsp});
		}

		public Parameters GetDeleteParameters(IMatrixData mdata){
			List<Parameter> par = new List<Parameter>{
				new SingleChoiceParam("Numerical row"){
					Values = mdata.NumericRowNames,
					Help = "Select the numerical row that should be deleted."
				}
			};
			return new Parameters(par);
		}

		public Parameters GetRenameParameters(IMatrixData mdata){
			List<Parameter> par = new List<Parameter>{
				new SingleChoiceParam("Numerical row"){
					Values = mdata.NumericRowNames,
					Help = "Select the numerical row that should be renamed."
				},
				new StringParam("New name"),
				new StringParam("New description"),
			};
			return new Parameters(par);
		}

		public Parameters GetCreateParameters(IMatrixData mdata){
			List<Parameter> par = new List<Parameter>{
				new StringParam("Row name"){Value = "Quantity1", Help = "Name of the new numerical annotation row."}
			};
			for (int i = 0; i < mdata.ColumnNames.Count; i++){
				string t = mdata.ColumnNames[i];
				string help = "Specify a numerical value for the column '" + t + "'.";
				par.Add(new DoubleParam(t, (i + 1.0)){Help = help});
			}
			return new Parameters(par);
		}
	}
}