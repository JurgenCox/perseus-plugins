using System.IO;
namespace PerseusPluginLib.Utils{
	public interface INameInfo{
		string[] GetRowNames();
		string[] GetNameSelection();
		string GetRowName(int ind);
		string GetRowName2(int ind);
		string GetRowDescription(int ind);
		int NameColumnIndex { get; set; }
		void Write(BinaryWriter writer);
	}
}