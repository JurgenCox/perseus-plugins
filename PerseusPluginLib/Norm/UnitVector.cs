using System;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Norm{
	public class UnitVector : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string HelpOutput => "Normalized expression matrix.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Normalization:UnitVector";

		public string Name => "Unit vectors";
		public string Heading => "Normalization";
		public bool IsActive => true;
		public float DisplayRank => -8;

		public string Description
			=>
				"The rows/columns are regarded as high-dimensional vectors. They are divided by their lengts resulting in a matrix of unit vectors."
			;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			Parameter<int> access = param.GetParam<int>("Matrix access");
			bool rows = access.Value == 0;
			UnitVectors(rows, mdata);
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Matrix access"){
						Values = new[]{"Rows", "Columns"},
						Help = "Specifies if the analysis is performed on the rows or the columns of the matrix."
					}
				});
		}

		public void UnitVectors(bool rows, IMatrixData data){
			if (rows){
				for (int i = 0; i < data.RowCount; i++){
					double len = 0;
					for (int j = 0; j < data.ColumnCount; j++){
						double q = data.Values.Get(i, j);
						len += q*q;
					}
					len = Math.Sqrt(len);
					for (int j = 0; j < data.ColumnCount; j++){
						data.Values.Set(i, j, data.Values.Get(i, j)/(float) len);
					}
				}
			} else{
				for (int j = 0; j < data.ColumnCount; j++){
					double len = 0;
					for (int i = 0; i < data.RowCount; i++){
						double q = data.Values.Get(i, j);
						len += q*q;
					}
					len = Math.Sqrt(len);
					for (int i = 0; i < data.RowCount; i++){
						data.Values.Set(i, j, data.Values.Get(i, j) / (float) len);
					}
				}
			}
		}
	}
}