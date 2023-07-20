using System;
using System.IO;
using BaseLibS.Util;
namespace PerseusApi.Generic{
	[Serializable]
	public class Settings{
		public int Nthreads { get; set; }
		public string TempFolder { get; set; }
		public string[] CommentPrefix { get; set; }

		public Settings(){
			Nthreads = Environment.ProcessorCount;
			CommentPrefix = new[]{"#", "!"};
			TempFolder = "";
		}
		public Settings(BinaryReader reader){
			Nthreads = reader.ReadInt32();
			CommentPrefix = FileUtils.ReadStringArray(reader);
			TempFolder = reader.ReadString();
		}
		public void Write(BinaryWriter writer){
			writer.Write(Nthreads);
			FileUtils.Write(CommentPrefix, writer);
			writer.Write(TempFolder);
		}
	}
}