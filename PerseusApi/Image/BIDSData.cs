using Accord;
using System;
using System.IO;
using System.Reflection;
namespace PerseusApi.Image{
	[Serializable]
	public class BIDSData : ICloneable{
		// BIDS information (see https://bids-specification.readthedocs.io/en/stable/99-appendices/04-entity-table.html)
		public string sub { get; set; } = "";
		public string ses { get; set; } = "";
		public string task { get; set; } = "";
		public string acq { get; set; } = "";
		public string ce { get; set; } = "";
		public string rec { get; set; } = "";
		public string dir { get; set; } = "";
		public string run { get; set; } = "";
		public string mod { get; set; } = "";
		public string echo { get; set; } = "";
		public string recording { get; set; } = "";
		public BIDSData(){
		}
		public BIDSData(string BIDS_encoded_path){
			foreach (PropertyInfo prop in typeof(BIDSData).GetProperties()){
				string property = prop.Name + "-";
				if (BIDS_encoded_path.Contains(property)){
					prop.SetValue(this,
						BIDS_encoded_path.Split(new string[]{property}, StringSplitOptions.None)[1].Split('_')[0]
							.Split('\\')[0]);
				}
			}
		}
		public BIDSData(BinaryReader reader){
			sub = reader.ReadString();
			ses = reader.ReadString();
			task = reader.ReadString();
			acq = reader.ReadString();
			ce = reader.ReadString();
			rec = reader.ReadString();
			dir = reader.ReadString();
			run = reader.ReadString();
			mod = reader.ReadString();
			echo = reader.ReadString();
			recording = reader.ReadString();
		}
		public void Write(BinaryWriter writer){
			writer.Write(sub);
			writer.Write(ses);
			writer.Write(task);
			writer.Write(acq);
			writer.Write(ce);
			writer.Write(rec);
			writer.Write(dir);
			writer.Write(run);
			writer.Write(mod);
			writer.Write(echo);
			writer.Write(recording);
		}
		public object Clone(){
			BIDSData clone = new BIDSData(){
				sub = sub,
				ses = ses,
				task = task,
				acq = acq,
				ce = ce,
				rec = rec,
				dir = dir,
				run = run,
				mod = mod,
				echo = echo,
				recording = recording
			};
			return clone;
		}
	}
}