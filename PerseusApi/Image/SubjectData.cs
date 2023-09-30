using System;
using System.IO;
namespace PerseusApi.Image{
	[Serializable]
	public class SubjectData : ICloneable{

		public SubjectData() {
		}
		public SubjectData(BinaryReader reader) {
		}
		public void Write(BinaryWriter writer){
		}
		public object Clone(){
			return new SubjectData();
		}
	}
}