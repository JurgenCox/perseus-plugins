using BaseLibS.Param;
using PerseusApi.Generic;

namespace PerseusApi.Sequence{
	public interface ISequenceUpload : ISequenceActivity, IUpload{
		void LoadData(ISequenceData sequenceData, Parameters parameters, ref IData[] supplData, ProcessInfo processInfo);
	}
}