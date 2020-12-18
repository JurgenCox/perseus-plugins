using System;
using PerseusApi.Generic;

namespace PerseusApi.Sequence{
	public interface ISequenceInfo : IDataWithAnnotationColumns, ICloneable{
		string[] GetSequences(bool aligned);
	    string GetSequenceAt(int index, bool aligned);
		int AlignmentCount { get; }
		string GetIdAt(int index);
	}
}