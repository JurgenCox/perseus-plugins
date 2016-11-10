using System;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Manual{
	[Serializable]
	public class SelectRowsManuallyResult : IAnalysisResult{
		private readonly IMatrixData mdata;

		public SelectRowsManuallyResult(IMatrixData mdata){
			this.mdata = mdata;
		}

		public object CreateUiElement(Action<string> updateStatusLabel, Action<IData> newData){
			return new SelectRowsManuallyControl(mdata, newData);
		}

		public string Heading => "Select rows manually";
	}
}