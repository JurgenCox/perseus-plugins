using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.AnnotCols.AnnotationProvider;

namespace PerseusPluginLib.AnnotCols{
	public class BackToBaseIdentifiers : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"This activity does the inverse of the 'Add annotation' activity. " +
				"Any of the columns that can be created by the " +
				"'Add annotation' activity can be mapped back to the base identifiers (typically UniProt ids).";

		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "To base identifiers";
		public string Heading => "Annot. columns";
		public bool IsActive => true;
		public float DisplayRank => -19.5f;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotcolumns:BackToBaseIdentifiers";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

        private readonly IAnnotationProvider _annotationProvider = new ConfFolderAnnotationProvider();
		public Parameters GetParameters(IMatrixData mdata, ref string errorString)
		{
		    return CreateParameters(mdata.StringColumnNames, _annotationProvider);
		}

	    public static Parameters CreateParameters(List<string> colChoice, IAnnotationProvider annotationProvider)
	    {
	        int colInd = 0;
	        for (int i = 0; i < colChoice.Count; i++)
	        {
	            if (colChoice[i].ToUpper().Contains("GENE NAME"))
	            {
	                colInd = i;
	                break;
	            }
	        }
	        int selFile = 0;
	        var textSources = annotationProvider.TextSources();
	        for (int i = 0; i < textSources.Length; i++)
	        {
	            if (textSources[i].source.ToLower().Contains("mainannot"))
	            {
	                selFile = i;
	                break;
	            }
	        }
	        Parameters[] subParams = new Parameters[textSources.Length];
	        for (int i = 0; i < subParams.Length; i++)
	        {
	            int selInd = 0;
	            var annot = textSources[i];
	            for (int j = 0; j < annot.names.Length; j++)
	            {
	                if (annot.names[j].ToLower().Contains("gene name"))
	                {
	                    selInd = j;
	                    break;
	                }
	            }
	            subParams[i] =
	                new Parameters(
	                    new SingleChoiceParam("Identifiers")
	                    {
	                        Values = colChoice,
	                        Value = colInd,
	                        Help =
	                            "Specify here the column that contains the identifiers which are going to be matched back to " +
	                            annot.id +
	                            " identifiers."
	                    }, new SingleChoiceParam("Identifier type") {Values = annot.names, Value = selInd});
	        }
	        return
	            new Parameters(new SingleChoiceWithSubParams("Source", selFile)
	            {
	                Values = textSources.Select(source => source.source).ToArray(),
	                SubParams = subParams,
	                ParamNameWidth = 136,
	                TotalWidth = 735
	            });
	    }

	    public void ProcessData(IMatrixData mdata, Parameters para, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo)
	    {
	        var annotationProvider = _annotationProvider;
	        ParameterWithSubParams<int> sourceParam = para.GetParamWithSubParams<int>("Source");
	        int sourceIndex = sourceParam.Value;
	        Parameters param = sourceParam.GetSubParameters();
	        int baseCol = param.GetParam<int>("Identifiers").Value;
	        int selection = param.GetParam<int>("Identifier type").Value;
	        var (_, id, _) = annotationProvider.TextSources()[sourceIndex];
	        var newColumn = annotationProvider.MapToBaseIdentifiers(mdata.StringColumns[baseCol], sourceIndex, selection);
            mdata.AddStringColumn(id, id, newColumn);
	    }

	}
}