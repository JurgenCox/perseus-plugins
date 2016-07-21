using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Mods{
	public class ShortenMotifs : IMatrixProcessing{
		public string Name => "Shorten motif length";
		public float DisplayRank => 18;
		public string Description => "Sequence windows are shortened based on start and length parameters.";
		public bool IsActive => true;
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Heading => "Modifications";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Modifications:ShortenMotifs";
		public Parameters GetParameters(IMatrixData mdata, ref string errString){
			List<string> colChoice = mdata.StringColumnNames;
			int colSeqInd = 0;
			for (int i = 0; i < colChoice.Count; i++){
				if (colChoice[i].ToUpper().Equals("SEQUENCE WINDOW")){
					colSeqInd = i;
					break;
				}
			}
			return
				new Parameters(new Parameter[]{
					new SingleChoiceParam("Sequence window"){
						Values = colChoice,
						Value = colSeqInd,
						Help = "Specify here the column that contains the sequence windows around the site."
					},
					new IntParam("Start", 6){Help = "The flanks will be measured with respect to this position."},
					new IntParam("Length", 11){Help = "Flanking regions of this length will be kept surrounding the central position."}
				});
		}
		public int GetMaxThreads(Parameters parameters){
			return 1;
		}
		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int stringColumnIndx = param.GetParam<int>("Sequence window").Value;
			string[] win = mdata.StringColumns[stringColumnIndx];
			int start = param.GetParam<int>("Start").Value - 1;
			int length = param.GetParam<int>("Length").Value;
			if (start < 0){
				processInfo.ErrString = "Start position cannot be smaller than 1.";
				return;
			}
			if (start + length > win[0].Length){
				processInfo.ErrString = "Start + length cannot exceed the total length of the sequence.";
				return;
			}
			string[] shortenedMotifs = new string[win.Length];
			for (int i = 0; i < mdata.RowCount; ++i){
				shortenedMotifs[i] = win[i].Substring(start, length);
			}
			mdata.AddStringColumn("Short sequence window", "", shortenedMotifs);
		}
	}
}