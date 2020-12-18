using PerseusApi.Generic;

namespace PerseusApi.Sequence{
	public interface ISequenceData : IData, IDataWithAnnotationRows{
		SequenceType SequenceType { get; set; }
		ISequenceInfo this[int i] { get; }
		void AddSequence(string sequence, string name, string description);
	}
}