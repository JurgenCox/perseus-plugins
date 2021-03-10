using System;

namespace PerseusApi.Image{
	[Serializable]
	public class SessionData : ICloneable{
		public object Clone(){
			return new SubjectData();
		}
	}
}