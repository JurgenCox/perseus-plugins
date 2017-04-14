using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public class CombineCategoricalColumns : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Combine the terms in two categorical columns to form a new categorical column.";
		public string HelpOutput => "A new categorical column is generated with combined terms.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Combine categorical columns";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 17.5f;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:CombineCategoricalColumns";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new SingleChoiceParam("First column", 0){Values = mdata.CategoryColumnNames}, new SingleChoiceParam("Second column", 0){Values = mdata.CategoryColumnNames});
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			if (mdata.CategoryColumnCount < 2){
				processInfo.ErrString = "There are less than two categorical columns available.";
				return;
			}
			int colInd1 = param.GetParam<int>("First column").Value;
			int colInd2 = param.GetParam<int>("Second column").Value;
			string[][] col1 = mdata.GetCategoryColumnAt(colInd1);
			string[][] col2 = mdata.GetCategoryColumnAt(colInd2);
			string[][] result = new string[col1.Length][];
			for (int i = 0; i < result.Length; i++){
				result[i] = CombineTerms(col1[i], col2[i]);
			}
			string colName = mdata.CategoryColumnNames[colInd1] + "_" + mdata.CategoryColumnNames[colInd2];
			mdata.AddCategoryColumn(colName, "", result);
		}

		private static string[] CombineTerms(ICollection<string> x, ICollection<string> y){
			string[] result = new string[x.Count*y.Count];
			int count = 0;
			foreach (string t in x){
				foreach (string t1 in y){
					result[count++] = t + "_" + t1;
				}
			}
			Array.Sort(result);
			return result;
		}
	}
}