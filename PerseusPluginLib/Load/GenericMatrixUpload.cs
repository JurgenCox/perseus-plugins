using System;
using System.Collections.Generic;
using System.IO;
using BaseLibS.Graph;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using Calc;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;

namespace PerseusPluginLib.Load{
	public class GenericMatrixUpload : IMatrixUpload{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => Bitmap2.GetImage("upload64.png");
		public string Name => "Generic matrix upload";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixUpload:GenericMatrixUpload";

		public string Description
			=>
				"Load data from a tab-separated file. The first row should contain the column names, also separated by tab characters. " +
				"All following rows contain the tab-separated values. Such a file can for instance be generated from an excel sheet by " +
				"using the export as a tab-separated .txt file.";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(ref string errorString){
			return
				new Parameters(new Parameter[]{
					new PerseusLoadMatrixParam("File"){
						Filter =
							"Text (Tab delimited) (*.txt;*.tsv)|*.txt;*.txt.gz;*.tsv;*.tsv.gz|CSV (Comma delimited) (*.csv)|*.csv;*.csv.gz",
						Help = "Please specify here the name of the file to be uploaded including its full path."
					}
				});
		}

		public void LoadData(IMatrixData mdata, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			PerseusLoadMatrixParam par = (PerseusLoadMatrixParam) parameters.GetParam("File");
			string filename = par.Filename;
			if (string.IsNullOrEmpty(filename)){
				processInfo.ErrString = "Please specify a filename";
				return;
			}
		    PerseusUtils.ReadMatrixFromFile(mdata, processInfo, filename, par.MainColumnIndices, par.NumericalColumnIndices, par.CategoryColumnIndices, par.TextColumnIndices, par.MultiNumericalColumnIndices, par.MainFilterParameters, par.NumericalFilterParameters, par.ShortenExpressionColumnNames);
        }
	}
}