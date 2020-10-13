using System;

namespace PerseusApi.Image{
	[Serializable]
	public class SubjectData : ICloneable{
		public object Clone(){
			return new SubjectData();
		}
	}
}