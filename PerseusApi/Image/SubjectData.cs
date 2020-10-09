using System;

namespace PerseusApi.Image{
	public class SubjectData : ICloneable{
		public object Clone(){
			return new SubjectData();
		}
	}
}